param(
    [string]$BaseUrl = "http://127.0.0.1:5080",
    [string]$Email = "demo.admin@formaai.pl",
    [Parameter(Mandatory)] [string]$Password
)

$ErrorActionPreference = "Stop"
$BaseUrl = $BaseUrl.TrimEnd('/')
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

function Get-Csrf {
    (Invoke-RestMethod -Uri "$BaseUrl/api/account/antiforgery" -WebSession $session).token
}

function Invoke-Api {
    param(
        [string]$Method,
        [string]$Path,
        $Body,
        [switch]$AllowNotFound
    )

    $params = @{
        Uri = "$BaseUrl/$Path"
        Method = $Method
        WebSession = $session
    }
    if ($Method -ne "GET") {
        $params.Headers = @{ "X-CSRF-TOKEN" = Get-Csrf }
    }
    if ($null -ne $Body) {
        $params.ContentType = "application/json; charset=utf-8"
        $json = $Body | ConvertTo-Json -Depth 12
        $params.Body = [Text.Encoding]::UTF8.GetBytes($json)
    }
    try {
        $response = Invoke-RestMethod @params
        if ($response -is [Array]) { $response | ForEach-Object { $_ } }
        else { $response }
    }
    catch {
        $status = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 }
        if ($status -eq 404 -and $AllowNotFound) { return $null }

        $details = ""
        if ($_.Exception.Response) {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $details = $reader.ReadToEnd()
        }
        throw "$Method $Path nie powiodło się (HTTP $status). $details"
    }
}

$login = @{ email = $Email; password = $Password }
Invoke-Api "POST" "api/account/login" $login | Out-Null

$created = [ordered]@{ Products = 0; Meals = 0; Exercises = 0; Measurements = 0; Recipes = 0; ShoppingItems = 0; Plans = 0 }
$today = [DateTimeOffset]::Now.Date
$todayText = $today.ToString("yyyy-MM-dd")

$productSeeds = @(
    @{ name = "Skyr naturalny"; brand = "Demo"; caloriesPer100 = 64; proteinPer100 = 12; fatPer100 = 0.2; carbohydratesPer100 = 4; defaultServingAmount = 150; defaultServingUnit = 0; gramsPerPiece = $null; barcode = $null },
    @{ name = "Płatki owsiane"; brand = "Demo"; caloriesPer100 = 370; proteinPer100 = 13; fatPer100 = 7; carbohydratesPer100 = 60; defaultServingAmount = 60; defaultServingUnit = 0; gramsPerPiece = $null; barcode = $null },
    @{ name = "Banan"; brand = $null; caloriesPer100 = 89; proteinPer100 = 1.1; fatPer100 = 0.3; carbohydratesPer100 = 23; defaultServingAmount = 1; defaultServingUnit = 2; gramsPerPiece = 120; barcode = $null },
    @{ name = "Jajka"; brand = $null; caloriesPer100 = 143; proteinPer100 = 13; fatPer100 = 9.5; carbohydratesPer100 = 0.7; defaultServingAmount = 1; defaultServingUnit = 2; gramsPerPiece = 56; barcode = $null },
    @{ name = "Pierś z kurczaka"; brand = $null; caloriesPer100 = 110; proteinPer100 = 23; fatPer100 = 1.5; carbohydratesPer100 = 0; defaultServingAmount = 180; defaultServingUnit = 0; gramsPerPiece = $null; barcode = $null },
    @{ name = "Ryż basmati ugotowany"; brand = "Demo"; caloriesPer100 = 130; proteinPer100 = 2.7; fatPer100 = 0.3; carbohydratesPer100 = 28; defaultServingAmount = 200; defaultServingUnit = 0; gramsPerPiece = $null; barcode = $null },
    @{ name = "Oliwa z oliwek"; brand = "Demo"; caloriesPer100 = 884; proteinPer100 = 0; fatPer100 = 100; carbohydratesPer100 = 0; defaultServingAmount = 10; defaultServingUnit = 0; gramsPerPiece = $null; barcode = $null },
    @{ name = "Brokuł"; brand = $null; caloriesPer100 = 34; proteinPer100 = 2.8; fatPer100 = 0.4; carbohydratesPer100 = 7; defaultServingAmount = 200; defaultServingUnit = 0; gramsPerPiece = $null; barcode = $null },
    @{ name = "Twaróg półtłusty"; brand = "Demo"; caloriesPer100 = 133; proteinPer100 = 19; fatPer100 = 5; carbohydratesPer100 = 3.5; defaultServingAmount = 200; defaultServingUnit = 0; gramsPerPiece = $null; barcode = $null },
    @{ name = "Pieczywo żytnie"; brand = "Demo"; caloriesPer100 = 250; proteinPer100 = 8.5; fatPer100 = 3.3; carbohydratesPer100 = 48; defaultServingAmount = 2; defaultServingUnit = 2; gramsPerPiece = 35; barcode = $null },
    @{ name = "Masło orzechowe"; brand = "Demo"; caloriesPer100 = 588; proteinPer100 = 25; fatPer100 = 50; carbohydratesPer100 = 20; defaultServingAmount = 20; defaultServingUnit = 0; gramsPerPiece = $null; barcode = $null },
    @{ name = "Jogurt grecki"; brand = "Demo"; caloriesPer100 = 97; proteinPer100 = 9; fatPer100 = 5; carbohydratesPer100 = 4; defaultServingAmount = 200; defaultServingUnit = 0; gramsPerPiece = $null; barcode = $null }
)

$products = @{}
foreach ($seed in $productSeeds) {
    $query = [Uri]::EscapeDataString($seed.name)
    $found = @(Invoke-Api "GET" "api/v1/products?query=$query" $null) | Where-Object { $_.name -eq $seed.name } | Select-Object -First 1
    if ($null -eq $found) {
        $found = Invoke-Api "POST" "api/v1/products" $seed
        $created.Products++
    }
    $products[$seed.name] = $found
}

$nutritionTarget = Invoke-Api "GET" "api/v1/nutrition-targets/current" $null -AllowNotFound
if ($null -eq $nutritionTarget) {
    Invoke-Api "POST" "api/v1/nutrition-targets" @{ effectiveFrom = $todayText; caloriesKcal = 2450; proteinG = 175; fatG = 75; carbohydratesG = 270 } | Out-Null
}

$pantry = @(
    @{ name = "Skyr naturalny"; quantity = 750; unit = 0; expiresOn = $today.AddDays(7).ToString("yyyy-MM-dd") },
    @{ name = "Płatki owsiane"; quantity = 900; unit = 0; expiresOn = $null },
    @{ name = "Banan"; quantity = 6; unit = 2; expiresOn = $today.AddDays(4).ToString("yyyy-MM-dd") },
    @{ name = "Jajka"; quantity = 10; unit = 2; expiresOn = $today.AddDays(14).ToString("yyyy-MM-dd") },
    @{ name = "Pierś z kurczaka"; quantity = 600; unit = 0; expiresOn = $today.AddDays(2).ToString("yyyy-MM-dd") },
    @{ name = "Ryż basmati ugotowany"; quantity = 800; unit = 0; expiresOn = $today.AddDays(3).ToString("yyyy-MM-dd") },
    @{ name = "Brokuł"; quantity = 500; unit = 0; expiresOn = $today.AddDays(5).ToString("yyyy-MM-dd") },
    @{ name = "Twaróg półtłusty"; quantity = 400; unit = 0; expiresOn = $today.AddDays(6).ToString("yyyy-MM-dd") }
)
foreach ($item in $pantry) {
    Invoke-Api "PUT" "api/v1/pantry/items" @{ productId = $products[$item.name].id; quantity = $item.quantity; unit = $item.unit; expiresOn = $item.expiresOn } | Out-Null
}

$day = Invoke-Api "GET" "api/v1/nutrition/days/$todayText" $null
if (@($day.meals).Count -eq 0) {
    $offset = [DateTimeOffset]::Now.Offset
    $meals = @(
        @{ name = "Owsianka ze skyrem"; hour = 8; items = @(@{ name = "Płatki owsiane"; grams = 70 }, @{ name = "Skyr naturalny"; grams = 150 }, @{ name = "Banan"; grams = 120 }, @{ name = "Masło orzechowe"; grams = 20 }) },
        @{ name = "Kurczak z ryżem i brokułem"; hour = 13; items = @(@{ name = "Pierś z kurczaka"; grams = 200 }, @{ name = "Ryż basmati ugotowany"; grams = 250 }, @{ name = "Brokuł"; grams = 200 }, @{ name = "Oliwa z oliwek"; grams = 10 }) },
        @{ name = "Jogurt i banan"; hour = 17; items = @(@{ name = "Jogurt grecki"; grams = 200 }, @{ name = "Banan"; grams = 100 }) },
        @{ name = "Kolacja białkowa"; hour = 20; items = @(@{ name = "Jajka"; grams = 168 }, @{ name = "Pieczywo żytnie"; grams = 70 }, @{ name = "Twaróg półtłusty"; grams = 150 }) }
    )
    foreach ($meal in $meals) {
        $when = [DateTimeOffset]::new($today.Date.AddHours($meal.hour), $offset).ToString("o")
        $items = @($meal.items | ForEach-Object { @{ productId = $products[$_.name].id; amountGrams = $_.grams; isEstimated = $false } })
        Invoke-Api "POST" "api/v1/meals" @{ name = $meal.name; occurredAt = $when; items = $items; deductFromPantry = $false } | Out-Null
        $created.Meals++
    }
}

$recipes = @(Invoke-Api "GET" "api/v1/recipes" $null)
if (-not ($recipes | Where-Object { $_.name -eq "Miska kurczaka po treningu" })) {
    $ingredients = @(
        @{ productId = $products["Pierś z kurczaka"].id; quantity = 200; unit = 0 },
        @{ productId = $products["Ryż basmati ugotowany"].id; quantity = 250; unit = 0 },
        @{ productId = $products["Brokuł"].id; quantity = 200; unit = 0 },
        @{ productId = $products["Oliwa z oliwek"].id; quantity = 10; unit = 0 }
    )
    Invoke-Api "POST" "api/v1/recipes" @{ name = "Miska kurczaka po treningu"; description = "Prosty posiłek do przygotowania na dwa dni."; servings = 1; preparationMinutes = 25; ingredients = $ingredients } | Out-Null
    $created.Recipes++
}

$shopping = Invoke-Api "GET" "api/v1/shopping-lists/active" $null
foreach ($item in @(
    @{ name = "Borówki"; quantity = 300; unit = 0; category = 3 },
    @{ name = "Woda mineralna"; quantity = 6; unit = 2; category = 4 }
)) {
    if (-not ($shopping.items | Where-Object { $_.name -eq $item.name })) {
        $shopping = Invoke-Api "POST" "api/v1/shopping-lists/active/items" @{ productId = $null; name = $item.name; quantity = $item.quantity; unit = $item.unit; category = $item.category }
        $created.ShoppingItems++
    }
}

$exerciseSeeds = @(
    @{ name = "Ściąganie drążka wyciągu"; muscleGroup = 1; equipment = 3; isUnilateral = $false; description = "Prowadź łokcie w dół, bez odchylania tułowia." },
    @{ name = "Wyciskanie na maszynie"; muscleGroup = 0; equipment = 2; isUnilateral = $false; description = "Ustaw siedzisko tak, aby uchwyty były na wysokości środka klatki." },
    @{ name = "Prostowanie nóg na maszynie"; muscleGroup = 5; equipment = 2; isUnilateral = $false; description = "Zatrzymaj ruch na moment w pełnym wyproście." },
    @{ name = "Uginanie nóg na maszynie"; muscleGroup = 6; equipment = 2; isUnilateral = $false; description = "Biodra trzymaj dociśnięte do ławki." },
    @{ name = "Unoszenie bokiem hantli"; muscleGroup = 2; equipment = 1; isUnilateral = $false; description = "Lekko ugnij łokcie i nie unoś dłoni wyżej niż barki." },
    @{ name = "Uginanie ramion hantlami"; muscleGroup = 3; equipment = 1; isUnilateral = $true; description = "Nie cofaj łokci podczas ruchu." },
    @{ name = "Prostowanie ramion na wyciągu"; muscleGroup = 4; equipment = 3; isUnilateral = $false; description = "Łokcie pozostają nieruchomo przy tułowiu." },
    @{ name = "Hip thrust na maszynie"; muscleGroup = 7; equipment = 2; isUnilateral = $false; description = "Dopnij pośladki w górze bez przeprostu lędźwi." },
    @{ name = "Plank"; muscleGroup = 9; equipment = 4; isUnilateral = $false; description = "Napnij brzuch i utrzymaj neutralną pozycję miednicy." }
)

$exercises = @{}
foreach ($exercise in @(Invoke-Api "GET" "api/v1/exercises" $null)) { $exercises[$exercise.name] = $exercise }
foreach ($seed in $exerciseSeeds) {
    if (-not $exercises.ContainsKey($seed.name)) {
        $exercises[$seed.name] = Invoke-Api "POST" "api/v1/exercises" $seed
        $created.Exercises++
    }
}

$plans = @(Invoke-Api "GET" "api/v1/training-plans" $null)
$plan = $plans | Where-Object { $_.name -eq "Plan demonstracyjny 3 dni" } | Select-Object -First 1
if ($null -eq $plan) {
    $allExercises = @{}
    foreach ($exercise in @(Invoke-Api "GET" "api/v1/exercises" $null)) { $allExercises[$exercise.name] = $exercise }
    function Planned($name, $sets, $min, $max, $rir, $rest) {
        @{ exerciseId = $allExercises[$name].id; sets = $sets; minReps = $min; maxReps = $max; targetRir = $rir; restSeconds = $rest }
    }
    $days = @(
        @{ name = "Góra A"; dayOfWeek = 1; exercises = @((Planned "Wyciskanie sztangi leżąc" 4 6 8 2 150), (Planned "Wiosłowanie sztangą" 4 8 10 2 120), (Planned "Unoszenie bokiem hantli" 3 12 15 2 75), (Planned "Prostowanie ramion na wyciągu" 3 10 14 2 75)) },
        @{ name = "Dół A"; dayOfWeek = 3; exercises = @((Planned "Przysiad ze sztangą" 4 5 8 2 180), (Planned "Uginanie nóg na maszynie" 3 10 14 2 90), (Planned "Hip thrust na maszynie" 3 8 12 2 120), (Planned "Plank" 3 30 45 1 60)) },
        @{ name = "Góra B"; dayOfWeek = 6; exercises = @((Planned "Ściąganie drążka wyciągu" 4 8 12 2 120), (Planned "Wyciskanie na maszynie" 3 8 12 2 90), (Planned "Wyciskanie nad głowę" 3 6 10 2 120), (Planned "Uginanie ramion hantlami" 3 10 14 2 75)) }
    )
    $plan = Invoke-Api "POST" "api/v1/training-plans" @{ name = "Plan demonstracyjny 3 dni"; goal = "Budowa siły i masy mięśniowej"; startsOn = $todayText; days = $days }
    $created.Plans++
}
Invoke-Api "POST" "api/v1/training-plans/$($plan.id)/activate" @{} | Out-Null

$from = $today.AddDays(-42).ToString("yyyy-MM-dd")
$progress = Invoke-Api "GET" "api/v1/progress/weight?from=$from&to=$todayText" $null
$existingDates = @($progress.points | ForEach-Object { $_.date })
$measurements = @(
    @{ days = -42; weight = 84.2; waist = 91.5 }, @{ days = -35; weight = 83.9; waist = 91.0 },
    @{ days = -28; weight = 83.7; waist = 90.6 }, @{ days = -21; weight = 83.3; waist = 90.0 },
    @{ days = -14; weight = 83.0; waist = 89.6 }, @{ days = -7; weight = 82.7; waist = 89.2 },
    @{ days = 0; weight = 82.4; waist = 88.8 }
)
foreach ($point in $measurements) {
    $date = $today.AddDays($point.days).ToString("yyyy-MM-dd")
    if ($existingDates -notcontains $date) {
        Invoke-Api "POST" "api/v1/body-measurements" @{ localDate = $date; weightKg = $point.weight; waistCm = $point.waist; notes = "Pomiar demonstracyjny" } | Out-Null
        $created.Measurements++
    }
}

Write-Host "Dane demonstracyjne gotowe."
$created.GetEnumerator() | ForEach-Object { Write-Host ("{0}: +{1}" -f $_.Key, $_.Value) }
