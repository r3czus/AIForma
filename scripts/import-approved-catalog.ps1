param(
    [Parameter(Mandatory)]
    [string]$WorkbookPath,

    [string]$ConnectionString = 'Server=(localdb)\FormaAIWeb;Database=FormaAI;Trusted_Connection=True;TrustServerCertificate=True',

    [string]$RecipeOwnerEmail = 'demo.admin@formaai.pl',

    [switch]$ValidateOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.IO.Compression.FileSystem

function Get-ColumnIndex {
    param([Parameter(Mandatory)][string]$CellReference)

    $letters = [regex]::Match($CellReference, '^[A-Z]+').Value
    $index = 0
    foreach ($letter in $letters.ToCharArray()) {
        $index = ($index * 26) + ([int]$letter - [int][char]'A' + 1)
    }

    return $index - 1
}

function Get-NodeText {
    param(
        [Parameter(Mandatory)]$Cell,
        [Parameter(Mandatory)][AllowEmptyCollection()][object[]]$SharedStrings
    )

    $type = $Cell.GetAttribute('t')
    if ($type -eq 'inlineStr') {
        $textNodes = $Cell.SelectNodes(".//*[local-name()='t']")
        return [string]::Concat(@($textNodes | ForEach-Object { $_.InnerText }))
    }

    $valueNode = $Cell.SelectSingleNode("./*[local-name()='v']")
    if ($null -eq $valueNode) {
        return ''
    }

    if ($type -eq 's') {
        return [string]$SharedStrings[[int]$valueNode.InnerText]
    }

    return [string]$valueNode.InnerText
}

function Read-CatalogWorkbook {
    param([Parameter(Mandatory)][string]$Path)

    $resolvedPath = (Resolve-Path -LiteralPath $Path).Path
    $archive = [System.IO.Compression.ZipFile]::OpenRead($resolvedPath)

    try {
        $sharedStrings = @()
        $sharedEntry = $archive.GetEntry('xl/sharedStrings.xml')
        if ($null -ne $sharedEntry) {
            $reader = [System.IO.StreamReader]::new($sharedEntry.Open())
            try {
                $sharedXml = [xml]$reader.ReadToEnd()
            }
            finally {
                $reader.Dispose()
            }

            $sharedStrings = @(
                $sharedXml.SelectNodes("//*[local-name()='sst']/*[local-name()='si']") |
                    ForEach-Object {
                        [string]::Concat(@($_.SelectNodes(".//*[local-name()='t']") | ForEach-Object { $_.InnerText }))
                    }
            )
        }

        $workbookEntry = $archive.GetEntry('xl/workbook.xml')
        $reader = [System.IO.StreamReader]::new($workbookEntry.Open())
        try {
            $workbookXml = [xml]$reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }

        $relationshipsEntry = $archive.GetEntry('xl/_rels/workbook.xml.rels')
        $reader = [System.IO.StreamReader]::new($relationshipsEntry.Open())
        try {
            $relationshipsXml = [xml]$reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }

        $relationships = @{}
        foreach ($relationship in $relationshipsXml.SelectNodes("//*[local-name()='Relationship']")) {
            $relationships[$relationship.Id] = $relationship.Target
        }

        $result = @{}
        foreach ($sheet in $workbookXml.SelectNodes("//*[local-name()='sheets']/*[local-name()='sheet']")) {
            $relationshipId = $sheet.GetAttribute('id', 'http://schemas.openxmlformats.org/officeDocument/2006/relationships')
            $target = [string]$relationships[$relationshipId]
            $entryPath = if ($target.StartsWith('/')) {
                $target.TrimStart('/')
            }
            else {
                'xl/' + $target.TrimStart('.', '/')
            }

            $sheetEntry = $archive.GetEntry($entryPath)
            $reader = [System.IO.StreamReader]::new($sheetEntry.Open())
            try {
                $sheetXml = [xml]$reader.ReadToEnd()
            }
            finally {
                $reader.Dispose()
            }

            $rows = [System.Collections.Generic.List[object]]::new()
            foreach ($row in $sheetXml.SelectNodes("//*[local-name()='sheetData']/*[local-name()='row']")) {
                $cells = @($row.SelectNodes("./*[local-name()='c']"))
                if ($cells.Count -eq 0) {
                    continue
                }

                $lastColumn = ($cells | ForEach-Object { Get-ColumnIndex $_.GetAttribute('r') } | Measure-Object -Maximum).Maximum
                $values = [object[]]::new($lastColumn + 1)
                foreach ($cell in $cells) {
                    $values[(Get-ColumnIndex $cell.GetAttribute('r'))] = Get-NodeText $cell $sharedStrings
                }

                $rows.Add($values)
            }

            $result[[string]$sheet.name] = $rows
        }

        return $result
    }
    finally {
        $archive.Dispose()
    }
}

function Convert-ToDecimal {
    param(
        [AllowEmptyString()][string]$Value,
        [string]$Field,
        [switch]$Nullable
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        if ($Nullable) {
            return $null
        }

        throw "Pole '$Field' nie może być puste."
    }

    $parsed = [decimal]0
    if (-not [decimal]::TryParse(
        $Value,
        [Globalization.NumberStyles]::Float,
        [Globalization.CultureInfo]::InvariantCulture,
        [ref]$parsed)) {
        throw "Pole '$Field' ma nieprawidłową wartość liczbową: '$Value'."
    }

    return $parsed
}

function Convert-ToInt {
    param(
        [AllowEmptyString()][string]$Value,
        [string]$Field
    )

    $parsed = 0
    if (-not [int]::TryParse($Value, [ref]$parsed)) {
        throw "Pole '$Field' ma nieprawidłową wartość całkowitą: '$Value'."
    }

    return $parsed
}

function Convert-ToApproval {
    param([AllowEmptyString()][string]$Value)
    return $Value.Trim().Equals('Tak', [StringComparison]::OrdinalIgnoreCase)
}

function Convert-ToServingUnit {
    param([Parameter(Mandatory)][string]$Value)

    switch ($Value.Trim().ToLowerInvariant()) {
        'gram' { return 0 }
        'milliliter' { return 1 }
        'piece' { return 2 }
        'sztuka' { return 2 }
        default { throw "Nieobsługiwana jednostka porcji: '$Value'." }
    }
}

$muscleGroups = @{
    Chest = 0
    Back = 1
    Shoulders = 2
    Biceps = 3
    Triceps = 4
    Quadriceps = 5
    Hamstrings = 6
    Glutes = 7
    Calves = 8
    Core = 9
    FullBody = 10
    Forearms = 11
}

$equipmentValues = @{
    Barbell = 0
    Dumbbell = 1
    Machine = 2
    Cable = 3
    Bodyweight = 4
    Kettlebell = 5
    Other = 6
}

function Get-RequiredEnumValue {
    param(
        [Parameter(Mandatory)][hashtable]$Values,
        [Parameter(Mandatory)][string]$Value,
        [Parameter(Mandatory)][string]$Field
    )

    if (-not $Values.ContainsKey($Value)) {
        throw "Pole '$Field' zawiera nieobsługiwaną wartość '$Value'."
    }

    return [int]$Values[$Value]
}

$tables = Read-CatalogWorkbook $WorkbookPath
$requiredSheets = @('Produkty', 'Ćwiczenia', 'Partie mięśniowe', 'Dania', 'Składniki dań')
foreach ($sheetName in $requiredSheets) {
    if (-not $tables.ContainsKey($sheetName)) {
        throw "Brakuje wymaganego arkusza '$sheetName'."
    }
}

$productRows = @($tables['Produkty'])
$exerciseRows = @($tables['Ćwiczenia'])
$muscleRows = @($tables['Partie mięśniowe'])
$recipeRows = @($tables['Dania'])
$ingredientRows = @($tables['Składniki dań'])

$products = [System.Collections.Generic.List[object]]::new()
for ($rowIndex = 1; $rowIndex -lt $productRows.Count; $rowIndex++) {
    $row = $productRows[$rowIndex]
    if (-not (Convert-ToApproval ([string]$row[14]))) {
        continue
    }

    $product = [pscustomobject]@{
        SourceIndex = Convert-ToInt ([string]$row[0]) "Produkty!A$($rowIndex + 1)"
        Name = ([string]$row[1]).Trim()
        Brand = ([string]$row[2]).Trim()
        Category = ([string]$row[3]).Trim()
        Origin = ([string]$row[4]).Trim()
        Calories = Convert-ToDecimal ([string]$row[5]) "Produkty!F$($rowIndex + 1)"
        Protein = Convert-ToDecimal ([string]$row[6]) "Produkty!G$($rowIndex + 1)"
        Fat = Convert-ToDecimal ([string]$row[7]) "Produkty!H$($rowIndex + 1)"
        Carbohydrates = Convert-ToDecimal ([string]$row[8]) "Produkty!I$($rowIndex + 1)"
        ServingAmount = Convert-ToDecimal ([string]$row[9]) "Produkty!J$($rowIndex + 1)" -Nullable
        ServingUnit = Convert-ToServingUnit ([string]$row[10])
        GramsPerPiece = Convert-ToDecimal ([string]$row[11]) "Produkty!L$($rowIndex + 1)" -Nullable
        DataStatus = ([string]$row[12]).Trim()
        SourceNote = ([string]$row[13]).Trim()
    }

    if ([string]::IsNullOrWhiteSpace($product.Name)) {
        throw "Produkt w wierszu $($rowIndex + 1) nie ma nazwy."
    }
    if (@($product.Calories, $product.Protein, $product.Fat, $product.Carbohydrates) | Where-Object { $_ -lt 0 }) {
        throw "Produkt '$($product.Name)' zawiera ujemną wartość odżywczą."
    }
    if ($null -ne $product.ServingAmount -and $product.ServingAmount -le 0) {
        throw "Produkt '$($product.Name)' ma nieprawidłową porcję."
    }
    if ($null -ne $product.GramsPerPiece -and $product.GramsPerPiece -le 0) {
        throw "Produkt '$($product.Name)' ma nieprawidłową masę sztuki."
    }

    $products.Add($product)
}

$productNames = @{}
foreach ($product in $products) {
    $key = $product.Name.ToLowerInvariant()
    if (-not $productNames.ContainsKey($key)) {
        $productNames[$key] = $product
    }
}

$detailedMuscleMap = @{}
for ($rowIndex = 1; $rowIndex -lt $muscleRows.Count; $rowIndex++) {
    $row = $muscleRows[$rowIndex]
    $detailedName = ([string]$row[1]).Trim()
    $enumName = ([string]$row[2]).Trim()
    $detailedMuscleMap[$detailedName] = Get-RequiredEnumValue $muscleGroups $enumName "Partie mięśniowe!C$($rowIndex + 1)"
}

$exercises = [System.Collections.Generic.List[object]]::new()
for ($rowIndex = 1; $rowIndex -lt $exerciseRows.Count; $rowIndex++) {
    $row = $exerciseRows[$rowIndex]
    if (-not (Convert-ToApproval ([string]$row[24]))) {
        continue
    }

    $engagements = @{}
    for ($columnIndex = 7; $columnIndex -le 22; $columnIndex++) {
        $percentage = Convert-ToInt ([string]$row[$columnIndex]) "Ćwiczenia!$columnIndex/$($rowIndex + 1)"
        if ($percentage -eq 0) {
            continue
        }
        if ($percentage -lt 0 -or $percentage -gt 100) {
            throw "Ćwiczenie '$([string]$row[1])' ma nieprawidłowy udział mięśni."
        }

        $detailedName = ([string]$exerciseRows[0][$columnIndex]).Trim()
        if (-not $detailedMuscleMap.ContainsKey($detailedName)) {
            throw "Brakuje mapowania partii '$detailedName'."
        }

        $group = [int]$detailedMuscleMap[$detailedName]
        if (-not $engagements.ContainsKey($group)) {
            $engagements[$group] = 0
        }
        $engagements[$group] += $percentage
    }

    $declaredTotal = Convert-ToInt ([string]$row[23]) "Ćwiczenia!X$($rowIndex + 1)"
    $aggregatedTotal = ($engagements.Values | Measure-Object -Sum).Sum
    if ($declaredTotal -ne 100 -or $aggregatedTotal -ne 100) {
        throw "Ćwiczenie '$([string]$row[1])' nie ma łącznie 100% zaangażowania."
    }
    if ($engagements.Count -gt 5) {
        throw "Ćwiczenie '$([string]$row[1])' po agregacji angażuje więcej niż 5 partii."
    }

    $description = ([string]$row[4]).Trim()
    if ($description.Length -gt 1000) {
        throw "Opis ćwiczenia '$([string]$row[1])' przekracza 1000 znaków."
    }

    $exercise = [pscustomobject]@{
        SourceIndex = Convert-ToInt ([string]$row[0]) "Ćwiczenia!A$($rowIndex + 1)"
        Name = ([string]$row[1]).Trim()
        Equipment = Get-RequiredEnumValue $equipmentValues ([string]$row[2]).Trim() "Ćwiczenia!C$($rowIndex + 1)"
        IsUnilateral = ([string]$row[3]).Trim().Equals('Tak', [StringComparison]::OrdinalIgnoreCase)
        Description = $description
        PrimaryMuscleGroup = Get-RequiredEnumValue $muscleGroups ([string]$row[6]).Trim() "Ćwiczenia!G$($rowIndex + 1)"
        Engagements = @($engagements.GetEnumerator() | Sort-Object Name | ForEach-Object {
            [pscustomobject]@{ MuscleGroup = [int]$_.Key; Percentage = [int]$_.Value }
        })
    }

    if ([string]::IsNullOrWhiteSpace($exercise.Name)) {
        throw "Ćwiczenie w wierszu $($rowIndex + 1) nie ma nazwy."
    }

    $exercises.Add($exercise)
}

$exerciseNames = @{}
foreach ($exercise in $exercises) {
    $key = $exercise.Name.ToLowerInvariant()
    if ($exerciseNames.ContainsKey($key)) {
        throw "Powtórzona nazwa ćwiczenia w arkuszu: '$($exercise.Name)'."
    }
    $exerciseNames[$key] = $exercise
}

$recipes = [System.Collections.Generic.List[object]]::new()
$recipeBySourceId = @{}
for ($rowIndex = 1; $rowIndex -lt $recipeRows.Count; $rowIndex++) {
    $row = $recipeRows[$rowIndex]
    if (-not (Convert-ToApproval ([string]$row[8]))) {
        continue
    }

    $sourceId = Convert-ToInt ([string]$row[0]) "Dania!A$($rowIndex + 1)"
    $recipe = [pscustomobject]@{
        SourceId = $sourceId
        Name = ([string]$row[1]).Trim()
        Servings = Convert-ToInt ([string]$row[2]) "Dania!C$($rowIndex + 1)"
        DeclaredIngredientCount = Convert-ToInt ([string]$row[3]) "Dania!D$($rowIndex + 1)"
        Calories = Convert-ToDecimal ([string]$row[4]) "Dania!E$($rowIndex + 1)"
        Protein = Convert-ToDecimal ([string]$row[5]) "Dania!F$($rowIndex + 1)"
        Fat = Convert-ToDecimal ([string]$row[6]) "Dania!G$($rowIndex + 1)"
        Carbohydrates = Convert-ToDecimal ([string]$row[7]) "Dania!H$($rowIndex + 1)"
        Ingredients = [System.Collections.Generic.List[object]]::new()
    }

    if ([string]::IsNullOrWhiteSpace($recipe.Name) -or $recipe.Servings -le 0) {
        throw "Danie w wierszu $($rowIndex + 1) ma nieprawidłowe dane podstawowe."
    }
    if ($recipeBySourceId.ContainsKey($sourceId)) {
        throw "Powtórzony identyfikator dania: $sourceId."
    }

    $recipeBySourceId[$sourceId] = $recipe
    $recipes.Add($recipe)
}

for ($rowIndex = 1; $rowIndex -lt $ingredientRows.Count; $rowIndex++) {
    $row = $ingredientRows[$rowIndex]
    $recipeId = Convert-ToInt ([string]$row[0]) "Składniki dań!A$($rowIndex + 1)"
    if (-not $recipeBySourceId.ContainsKey($recipeId)) {
        throw "Składnik w wierszu $($rowIndex + 1) odwołuje się do niezatwierdzonego dania $recipeId."
    }

    $productName = ([string]$row[2]).Trim()
    if (-not $productNames.ContainsKey($productName.ToLowerInvariant())) {
        throw "Składnik dania odwołuje się do brakującego produktu '$productName'."
    }

    $quantity = Convert-ToDecimal ([string]$row[3]) "Składniki dań!D$($rowIndex + 1)"
    if ($quantity -le 0) {
        throw "Składnik '$productName' ma nieprawidłową ilość."
    }

    $recipeBySourceId[$recipeId].Ingredients.Add([pscustomobject]@{
        ProductName = $productName
        Quantity = $quantity
    })
}

foreach ($recipe in $recipes) {
    if ($recipe.Ingredients.Count -ne $recipe.DeclaredIngredientCount) {
        throw "Danie '$($recipe.Name)' ma inną liczbę składników niż zadeklarowana."
    }

    $calculated = [ordered]@{
        Calories = [decimal]0
        Protein = [decimal]0
        Fat = [decimal]0
        Carbohydrates = [decimal]0
    }
    foreach ($ingredient in $recipe.Ingredients) {
        $product = $productNames[$ingredient.ProductName.ToLowerInvariant()]
        $factor = $ingredient.Quantity / [decimal]100
        $calculated.Calories += $product.Calories * $factor
        $calculated.Protein += $product.Protein * $factor
        $calculated.Fat += $product.Fat * $factor
        $calculated.Carbohydrates += $product.Carbohydrates * $factor
    }

    foreach ($field in $calculated.Keys) {
        if ([math]::Abs([decimal]$calculated[$field] - [decimal]$recipe.$field) -gt [decimal]0.05) {
            throw "Makro dania '$($recipe.Name)' nie zgadza się dla pola '$field'."
        }
    }
}

if ($products.Count -ne 1000 -or $exercises.Count -ne 100 -or $recipes.Count -ne 30 -or ($recipes.Ingredients.Count | Measure-Object -Sum).Sum -ne 92) {
    throw "Nieoczekiwany zakres zatwierdzonych danych: produkty=$($products.Count), ćwiczenia=$($exercises.Count), dania=$($recipes.Count), składniki=$(($recipes.Ingredients.Count | Measure-Object -Sum).Sum)."
}

Write-Output "Walidacja arkusza zakończona: produkty=$($products.Count), ćwiczenia=$($exercises.Count), dania=$($recipes.Count), składniki=$(($recipes.Ingredients.Count | Measure-Object -Sum).Sum)."

if ($ValidateOnly) {
    return
}

function New-Command {
    param(
        [Parameter(Mandatory)][System.Data.SqlClient.SqlConnection]$Connection,
        [Parameter(Mandatory)][System.Data.SqlClient.SqlTransaction]$Transaction,
        [Parameter(Mandatory)][string]$Sql,
        [Parameter(Mandatory)][hashtable]$Parameters
    )

    $command = $Connection.CreateCommand()
    $command.Transaction = $Transaction
    $command.CommandText = $Sql
    $command.CommandTimeout = 120
    foreach ($entry in $Parameters.GetEnumerator()) {
        $value = if ($null -eq $entry.Value -or ($entry.Value -is [string] -and [string]::IsNullOrWhiteSpace($entry.Value))) {
            [DBNull]::Value
        }
        else {
            $entry.Value
        }
        [void]$command.Parameters.AddWithValue("@$($entry.Key)", $value)
    }

    return $command
}

$connection = [System.Data.SqlClient.SqlConnection]::new($ConnectionString)
$connection.Open()
$transaction = $connection.BeginTransaction([System.Data.IsolationLevel]::Serializable)

try {
    $ownerCommand = New-Command $connection $transaction @'
SELECT TOP (1) [Id]
FROM [AspNetUsers]
WHERE [NormalizedEmail] = @NormalizedEmail;
'@ @{ NormalizedEmail = $RecipeOwnerEmail.Trim().ToUpperInvariant() }
    $recipeOwnerId = $ownerCommand.ExecuteScalar()
    $ownerCommand.Dispose()
    if ($null -eq $recipeOwnerId -or $recipeOwnerId -is [DBNull]) {
        throw "Nie znaleziono konta '$RecipeOwnerEmail' dla importu dań."
    }
    $recipeOwnerId = [string]$recipeOwnerId

    $productIds = @{}
    $createdProducts = 0
    $updatedProducts = 0
    foreach ($product in $products) {
        $externalId = "formaai-catalog-product-$($product.SourceIndex)"
        $findCommand = New-Command $connection $transaction @'
SELECT TOP (1) [Id]
FROM [Products]
WHERE [OwnerUserId] IS NULL AND [ExternalId] = @ExternalId;
'@ @{ ExternalId = $externalId }
        $productId = $findCommand.ExecuteScalar()
        $findCommand.Dispose()

        if ($null -eq $productId -or $productId -is [DBNull]) {
            $findCommand = New-Command $connection $transaction @'
SELECT TOP (1) [Id]
FROM [Products]
WHERE [OwnerUserId] IS NULL
  AND [Name] = @Name
  AND (([Brand] IS NULL AND @Brand IS NULL) OR [Brand] = @Brand)
  AND ([ExternalId] IS NULL OR [ExternalId] NOT LIKE N'formaai-catalog-product-%')
ORDER BY [CreatedAtUtc];
'@ @{ Name = $product.Name; Brand = $product.Brand }
            $productId = $findCommand.ExecuteScalar()
            $findCommand.Dispose()
        }

        if ($null -eq $productId -or $productId -is [DBNull]) {
            $productId = [Guid]::NewGuid()
            $command = New-Command $connection $transaction @'
INSERT INTO [Products]
    ([Id], [OwnerUserId], [Name], [Brand], [Barcode], [DefaultServingAmount], [DefaultServingUnit],
     [GramsPerPiece], [CaloriesPer100], [ProteinPer100], [FatPer100], [CarbohydratesPer100],
     [Source], [ExternalId], [IsVerifiedByUser], [CreatedAtUtc], [UpdatedAtUtc])
VALUES
    (@Id, NULL, @Name, @Brand, NULL, @ServingAmount, @ServingUnit,
     @GramsPerPiece, @Calories, @Protein, @Fat, @Carbohydrates,
     2, @ExternalId, 1, @Now, @Now);
'@ @{
                Id = $productId
                Name = $product.Name
                Brand = $product.Brand
                ServingAmount = $product.ServingAmount
                ServingUnit = $product.ServingUnit
                GramsPerPiece = $product.GramsPerPiece
                Calories = $product.Calories
                Protein = $product.Protein
                Fat = $product.Fat
                Carbohydrates = $product.Carbohydrates
                ExternalId = $externalId
                Now = [DateTime]::UtcNow
            }
            [void]$command.ExecuteNonQuery()
            $command.Dispose()
            $createdProducts++
        }
        else {
            $productId = [Guid]$productId
            $command = New-Command $connection $transaction @'
UPDATE [Products]
SET [Brand] = @Brand,
    [DefaultServingAmount] = @ServingAmount,
    [DefaultServingUnit] = @ServingUnit,
    [GramsPerPiece] = @GramsPerPiece,
    [CaloriesPer100] = @Calories,
    [ProteinPer100] = @Protein,
    [FatPer100] = @Fat,
    [CarbohydratesPer100] = @Carbohydrates,
    [Source] = 2,
    [ExternalId] = @ExternalId,
    [IsVerifiedByUser] = 1,
    [UpdatedAtUtc] = @Now
WHERE [Id] = @Id;
'@ @{
                Id = $productId
                Brand = $product.Brand
                ServingAmount = $product.ServingAmount
                ServingUnit = $product.ServingUnit
                GramsPerPiece = $product.GramsPerPiece
                Calories = $product.Calories
                Protein = $product.Protein
                Fat = $product.Fat
                Carbohydrates = $product.Carbohydrates
                ExternalId = $externalId
                Now = [DateTime]::UtcNow
            }
            [void]$command.ExecuteNonQuery()
            $command.Dispose()
            $updatedProducts++
        }

        $productKey = $product.Name.ToLowerInvariant()
        if (-not $productIds.ContainsKey($productKey)) {
            $productIds[$productKey] = $productId
        }
    }

    $exerciseIds = @{}
    $createdExercises = 0
    $updatedExercises = 0
    foreach ($exercise in $exercises) {
        $findCommand = New-Command $connection $transaction @'
SELECT TOP (1) [Id]
FROM [Exercises]
WHERE [OwnerUserId] IS NULL AND [Name] = @Name
ORDER BY [CreatedAtUtc];
'@ @{ Name = $exercise.Name }
        $exerciseId = $findCommand.ExecuteScalar()
        $findCommand.Dispose()

        if ($null -eq $exerciseId -or $exerciseId -is [DBNull]) {
            $exerciseId = [Guid]::NewGuid()
            $command = New-Command $connection $transaction @'
INSERT INTO [Exercises]
    ([Id], [OwnerUserId], [Name], [Description], [PrimaryMuscleGroup], [Equipment],
     [IsUnilateral], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
VALUES
    (@Id, NULL, @Name, @Description, @PrimaryMuscleGroup, @Equipment,
     @IsUnilateral, 1, @Now, @Now);
'@ @{
                Id = $exerciseId
                Name = $exercise.Name
                Description = $exercise.Description
                PrimaryMuscleGroup = $exercise.PrimaryMuscleGroup
                Equipment = $exercise.Equipment
                IsUnilateral = $exercise.IsUnilateral
                Now = [DateTime]::UtcNow
            }
            [void]$command.ExecuteNonQuery()
            $command.Dispose()
            $createdExercises++
        }
        else {
            $exerciseId = [Guid]$exerciseId
            $command = New-Command $connection $transaction @'
UPDATE [Exercises]
SET [Description] = @Description,
    [PrimaryMuscleGroup] = @PrimaryMuscleGroup,
    [Equipment] = @Equipment,
    [IsUnilateral] = @IsUnilateral,
    [IsActive] = 1,
    [UpdatedAtUtc] = @Now
WHERE [Id] = @Id;
'@ @{
                Id = $exerciseId
                Description = $exercise.Description
                PrimaryMuscleGroup = $exercise.PrimaryMuscleGroup
                Equipment = $exercise.Equipment
                IsUnilateral = $exercise.IsUnilateral
                Now = [DateTime]::UtcNow
            }
            [void]$command.ExecuteNonQuery()
            $command.Dispose()
            $updatedExercises++
        }

        $deleteCommand = New-Command $connection $transaction 'DELETE FROM [ExerciseMuscleEngagements] WHERE [ExerciseId] = @ExerciseId;' @{
            ExerciseId = $exerciseId
        }
        [void]$deleteCommand.ExecuteNonQuery()
        $deleteCommand.Dispose()

        foreach ($engagement in $exercise.Engagements) {
            $command = New-Command $connection $transaction @'
INSERT INTO [ExerciseMuscleEngagements] ([Id], [ExerciseId], [MuscleGroup], [Percentage])
VALUES (@Id, @ExerciseId, @MuscleGroup, @Percentage);
'@ @{
                Id = [Guid]::NewGuid()
                ExerciseId = $exerciseId
                MuscleGroup = $engagement.MuscleGroup
                Percentage = $engagement.Percentage
            }
            [void]$command.ExecuteNonQuery()
            $command.Dispose()
        }

        $exerciseIds[$exercise.Name.ToLowerInvariant()] = $exerciseId
    }

    $createdRecipes = 0
    $updatedRecipes = 0
    $importedIngredientCount = 0
    foreach ($recipe in $recipes) {
        $findCommand = New-Command $connection $transaction @'
SELECT TOP (1) [Id]
FROM [Recipes]
WHERE [UserId] = @UserId AND [Name] = @Name
ORDER BY [CreatedAtUtc];
'@ @{ UserId = $recipeOwnerId; Name = $recipe.Name }
        $recipeId = $findCommand.ExecuteScalar()
        $findCommand.Dispose()
        $description = "Danie z katalogu FormaAI #$($recipe.SourceId)."

        if ($null -eq $recipeId -or $recipeId -is [DBNull]) {
            $recipeId = [Guid]::NewGuid()
            $command = New-Command $connection $transaction @'
INSERT INTO [Recipes]
    ([Id], [UserId], [Name], [Description], [Servings], [PreparationMinutes], [CreatedAtUtc])
VALUES
    (@Id, @UserId, @Name, @Description, @Servings, 0, @Now);
'@ @{
                Id = $recipeId
                UserId = $recipeOwnerId
                Name = $recipe.Name
                Description = $description
                Servings = $recipe.Servings
                Now = [DateTime]::UtcNow
            }
            [void]$command.ExecuteNonQuery()
            $command.Dispose()
            $createdRecipes++
        }
        else {
            $recipeId = [Guid]$recipeId
            $command = New-Command $connection $transaction @'
UPDATE [Recipes]
SET [Description] = @Description,
    [Servings] = @Servings
WHERE [Id] = @Id;
'@ @{ Id = $recipeId; Description = $description; Servings = $recipe.Servings }
            [void]$command.ExecuteNonQuery()
            $command.Dispose()
            $updatedRecipes++
        }

        $deleteCommand = New-Command $connection $transaction 'DELETE FROM [RecipeIngredients] WHERE [RecipeId] = @RecipeId;' @{
            RecipeId = $recipeId
        }
        [void]$deleteCommand.ExecuteNonQuery()
        $deleteCommand.Dispose()

        $order = 1
        foreach ($ingredient in $recipe.Ingredients) {
            $productId = $productIds[$ingredient.ProductName.ToLowerInvariant()]
            $command = New-Command $connection $transaction @'
INSERT INTO [RecipeIngredients] ([Id], [RecipeId], [ProductId], [Quantity], [Unit], [Order])
VALUES (@Id, @RecipeId, @ProductId, @Quantity, 0, @Order);
'@ @{
                Id = [Guid]::NewGuid()
                RecipeId = $recipeId
                ProductId = $productId
                Quantity = $ingredient.Quantity
                Order = $order
            }
            [void]$command.ExecuteNonQuery()
            $command.Dispose()
            $order++
            $importedIngredientCount++
        }
    }

    $verifyProducts = New-Command $connection $transaction @'
SELECT COUNT(*)
FROM [Products]
WHERE [OwnerUserId] IS NULL AND [ExternalId] LIKE N'formaai-catalog-product-%';
'@ @{}
    $verifiedProductCount = [int]$verifyProducts.ExecuteScalar()
    $verifyProducts.Dispose()

    $verifiedExerciseCount = 0
    foreach ($exerciseId in $exerciseIds.Values) {
        $verifyExercise = New-Command $connection $transaction 'SELECT COUNT(*) FROM [Exercises] WHERE [Id] = @Id AND [OwnerUserId] IS NULL AND [IsActive] = 1;' @{
            Id = $exerciseId
        }
        $verifiedExerciseCount += [int]$verifyExercise.ExecuteScalar()
        $verifyExercise.Dispose()
    }

    $verifiedRecipeCount = 0
    foreach ($recipe in $recipes) {
        $verifyRecipe = New-Command $connection $transaction 'SELECT COUNT(*) FROM [Recipes] WHERE [UserId] = @UserId AND [Name] = @Name;' @{
            UserId = $recipeOwnerId
            Name = $recipe.Name
        }
        $verifiedRecipeCount += [int]$verifyRecipe.ExecuteScalar()
        $verifyRecipe.Dispose()
    }

    if ($verifiedProductCount -lt 1000 -or $verifiedExerciseCount -ne 100 -or $verifiedRecipeCount -ne 30 -or $importedIngredientCount -ne 92) {
        throw "Weryfikacja transakcji nie powiodła się: produkty=$verifiedProductCount, ćwiczenia=$verifiedExerciseCount, dania=$verifiedRecipeCount, składniki=$importedIngredientCount."
    }

    $transaction.Commit()
    Write-Output "Migracja zakończona."
    Write-Output "Produkty: utworzono=$createdProducts, zaktualizowano=$updatedProducts."
    Write-Output "Ćwiczenia: utworzono=$createdExercises, zaktualizowano=$updatedExercises."
    Write-Output "Dania: utworzono=$createdRecipes, zaktualizowano=$updatedRecipes, składniki=$importedIngredientCount."
}
catch {
    try {
        $transaction.Rollback()
    }
    catch {
    }
    throw
}
finally {
    $transaction.Dispose()
    $connection.Dispose()
}





