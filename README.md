# FormaAI

Mobilna aplikacja PWA do prowadzenia diety, treningów, pomiarów, spiżarni i list zakupów. Zawiera kontrolowanego asystenta Gemini, który czyta wyłącznie dane zalogowanego użytkownika. Zmiany proponowane przez AI są najpierw wersją roboczą i trafiają do domeny dopiero po jawnym zatwierdzeniu.

## Uruchomienie lokalne

Wymagane są .NET 8 i Docker. Ustaw własne hasło bazy, uruchom SQL Server, a potem aplikację:

```powershell
$env:FORMAAI_DB_PASSWORD = "własne-mocne-hasło"
$env:ConnectionStrings__FormaAI = "Server=localhost,1433;Database=FormaAI;User Id=sa;Password=$($env:FORMAAI_DB_PASSWORD);TrustServerCertificate=True"
docker compose up -d sqlserver
dotnet run --project src/FormaAI.Api
```

Aplikacja sama stosuje oczekujące migracje. Adres zostanie wypisany przez `dotnet run`; API hostuje też frontend Blazor.

Asystent działa po ustawieniu nowego klucza Gemini w zmiennej środowiskowej. Klucza nie zapisuj w repozytorium ani w pliku `appsettings.json`:

```powershell
$env:Gemini__ApiKey = "nowy-klucz"
```

Panel administratora jest dostępny pod `/admin`. Uprawnienie otrzyma konto o adresie wskazanym w konfiguracji:

```powershell
$env:Admin__Email = "twoj-adres@example.com"
```

W panelu można później wybrać Gemini albo API zgodne z OpenAI oraz zmienić adres, model i zaszyfrowany klucz bez restartu aplikacji.

Wdrożenie w EOG musi korzystać z wariantu Gemini dozwolonego przez aktualne [warunki Gemini API](https://ai.google.dev/gemini-api/terms). Dane wrażliwe powinny być wysyłane wyłącznie w konfiguracji o odpowiednich warunkach przetwarzania.

## Docker

Pełny zestaw aplikacja + SQL Server:

```powershell
$env:FORMAAI_DB_PASSWORD = "własne-mocne-hasło"
$env:GEMINI_API_KEY = "nowy-klucz"
$env:ADMIN_EMAIL = "twoj-adres@example.com"
docker compose up --build -d
```

Aplikacja będzie dostępna pod `http://localhost:8080`, a stan procesu pod `http://localhost:8080/health/live`.

## Kopia bazy

Kopia trafia do osobnego wolumenu `formaai-backups`:

```powershell
./scripts/backup-database.ps1
```

W środowisku produkcyjnym wolumen kopii trzeba dodatkowo replikować poza hosta i okresowo testować odtwarzanie.

## Weryfikacja

```powershell
dotnet build FormaAI.sln
dotnet test FormaAI.sln --no-build -m:1
dotnet ef migrations has-pending-model-changes --project src/FormaAI.Infrastructure --startup-project src/FormaAI.Api --no-build
```

Testy domyślne używają fałszywego modelu i nie wykonują płatnych wywołań Gemini.

## Prywatność i dane zewnętrzne

Użytkownik może usunąć konto i dane zależne w ustawieniach. Treści rozmów ani sekrety nie są zapisywane w logach narzędzi. Import kodów kreskowych korzysta z [Open Food Facts](https://world.openfoodfacts.org/data); dane z tej bazy są zawsze przedstawiane do weryfikacji przed zapisem.
