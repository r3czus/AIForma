param(
    [string]$BaseUrl = "http://127.0.0.1:5082",
    [string]$Email = "demo.admin@formaai.pl",
    [Parameter(Mandatory)] [string]$Password,
    [switch]$CreateAccount,
    [switch]$Preview
)

$ErrorActionPreference = "Stop"
$BaseUrl = $BaseUrl.TrimEnd("/")
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

function Get-Csrf {
    (Invoke-RestMethod -Uri "$BaseUrl/api/account/antiforgery" -WebSession $session).token
}

function Invoke-Api {
    param(
        [string]$Method,
        [string]$Path,
        $Body = $null
    )

    $parameters = @{
        Uri = "$BaseUrl/$Path"
        Method = $Method
        WebSession = $session
    }
    if ($Method -ne "GET") {
        $parameters.Headers = @{ "X-CSRF-TOKEN" = Get-Csrf }
    }
    if ($null -ne $Body) {
        $parameters.ContentType = "application/json; charset=utf-8"
        $json = $Body | ConvertTo-Json -Depth 12
        $parameters.Body = [Text.Encoding]::UTF8.GetBytes($json)
    }
    try {
        Invoke-RestMethod @parameters
    }
    catch {
        $status = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 }
        $details = ""
        if ($_.Exception.Response) {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $details = $reader.ReadToEnd()
        }
        throw "$Method $Path nie powiodło się (HTTP $status). $details"
    }
}

try {
    Invoke-Api "POST" "api/account/login" @{ email = $Email; password = $Password } | Out-Null
}
catch {
    if (-not $CreateAccount -or $_ -notmatch "HTTP 401") {
        throw
    }
    Invoke-Api "POST" "api/account/register" @{
        email = $Email
        password = $Password
        timeZoneId = "Europe/Warsaw"
    } | Out-Null
}

$exerciseNames = @(
    "Wyciskanie sztangi leżąc",
    "Wiosłowanie na maszynie z podparciem",
    "Wyciskanie hantli skos dodatni",
    "Ściąganie drążka do klatki",
    "Unoszenie hantli bokiem",
    "Prostowanie ramion na wyciągu",
    "Uginanie ramion z hantlami",
    "Hack squat",
    "Martwy ciąg rumuński",
    "Suwnica 45 stopni",
    "Uginanie nóg leżąc",
    "Wspięcia na palce stojąc",
    "Crunch na wyciągu",
    "Podciąganie podchwytem",
    "Wyciskanie hantli siedząc",
    "Wyciskanie na maszynie siedząc",
    "Wiosłowanie na wyciągu siedząc",
    "Odwrotne rozpiętki",
    "Prostowanie ramion liną nad głową",
    "Uginanie na modlitewniku",
    "Przysiad bułgarski",
    "Hip thrust ze sztangą",
    "Prostowanie nóg na maszynie",
    "Uginanie nóg siedząc",
    "Wspięcia na palce siedząc",
    "Unoszenie nóg w zwisie"
)

$catalog = @{}
foreach ($name in $exerciseNames) {
    $query = [Uri]::EscapeDataString("*$name*")
    $response = Invoke-Api "GET" "api/v1/exercises?query=$query"
    $match = foreach ($candidate in $response) {
        if ($candidate.name -eq $name) {
            $candidate
            break
        }
    }
    if ($null -eq $match) {
        throw "Brak ćwiczenia w katalogu: $name"
    }
    $catalog[$name] = $match
}

function Planned($name, $sets, $min, $max, $rir, $rest) {
    @{
        exerciseId = $catalog[$name].id
        sets = $sets
        minReps = $min
        maxReps = $max
        targetRir = $rir
        restSeconds = $rest
    }
}

$days = @(
    @{
        name = "Góra A"
        dayOfWeek = 1
        exercises = @(
            (Planned "Wyciskanie sztangi leżąc" 3 6 8 2 150),
            (Planned "Wiosłowanie na maszynie z podparciem" 3 8 12 2 120),
            (Planned "Wyciskanie hantli skos dodatni" 3 8 12 2 120),
            (Planned "Ściąganie drążka do klatki" 3 8 12 2 120),
            (Planned "Unoszenie hantli bokiem" 3 12 20 1 75),
            (Planned "Prostowanie ramion na wyciągu" 2 10 15 1 75),
            (Planned "Uginanie ramion z hantlami" 2 10 15 1 75)
        )
    },
    @{
        name = "Dół A"
        dayOfWeek = 2
        exercises = @(
            (Planned "Hack squat" 3 6 10 2 180),
            (Planned "Martwy ciąg rumuński" 3 8 10 2 150),
            (Planned "Suwnica 45 stopni" 3 10 15 2 120),
            (Planned "Uginanie nóg leżąc" 3 10 15 1 90),
            (Planned "Wspięcia na palce stojąc" 3 10 15 1 75),
            (Planned "Crunch na wyciągu" 3 10 15 2 75)
        )
    },
    @{
        name = "Góra B"
        dayOfWeek = 4
        exercises = @(
            (Planned "Podciąganie podchwytem" 3 6 10 2 150),
            (Planned "Wyciskanie hantli siedząc" 3 8 12 2 120),
            (Planned "Wyciskanie na maszynie siedząc" 3 8 12 2 120),
            (Planned "Wiosłowanie na wyciągu siedząc" 3 8 12 2 120),
            (Planned "Odwrotne rozpiętki" 2 12 20 1 75),
            (Planned "Unoszenie hantli bokiem" 3 12 20 1 75),
            (Planned "Prostowanie ramion liną nad głową" 2 10 15 1 75),
            (Planned "Uginanie na modlitewniku" 2 10 15 1 75)
        )
    },
    @{
        name = "Dół B"
        dayOfWeek = 6
        exercises = @(
            (Planned "Przysiad bułgarski" 3 8 12 2 120),
            (Planned "Hip thrust ze sztangą" 3 8 12 2 150),
            (Planned "Prostowanie nóg na maszynie" 3 10 15 1 90),
            (Planned "Uginanie nóg siedząc" 3 10 15 1 90),
            (Planned "Wspięcia na palce siedząc" 3 12 20 1 75),
            (Planned "Unoszenie nóg w zwisie" 3 10 15 2 75)
        )
    }
)

$planName = "Góra/Dół 4 dni · rekompozycja"
$request = @{
    name = $planName
    goal = "Rekompozycja · siła i masa mięśniowa"
    startsOn = [DateTime]::Today.ToString("yyyy-MM-dd")
    days = $days
}

if ($Preview) {
    $request | ConvertTo-Json -Depth 12
    return
}

$plans = Invoke-Api "GET" "api/v1/training-plans"
$existing = foreach ($candidate in $plans) {
    if ($candidate.name -eq $planName) {
        $candidate
        break
    }
}
$plan = if ($null -eq $existing) {
    Invoke-Api "POST" "api/v1/training-plans" $request
}
else {
    Invoke-Api "PUT" "api/v1/training-plans/$($existing.id)" $request
}

Invoke-Api "POST" "api/v1/training-plans/$($plan.id)/activate" @{} | Out-Null
$active = Invoke-Api "GET" "api/v1/training-plans/active"

[pscustomobject]@{
    Id = $active.id
    Name = $active.name
    Active = $active.isActive
    Days = @($active.days).Count
    Exercises = @($active.days | ForEach-Object { $_.exercises }).Count
}
