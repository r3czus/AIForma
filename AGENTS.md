# Kontekst projektu FormaAI

## Lokalizacja i cel

- Repozytorium: `C:\Users\Jannu\OneDrive\Pulpit\AIFroma`
- FormaAI to responsywna aplikacja PWA do prowadzenia diety, treningów i postępów sylwetkowych.
- Główny scenariusz: użytkownik zapisuje jedzenie i trening, a aplikacja łączy te dane w czytelne podsumowania dzienne, tygodniowe i długoterminowe.
- Interfejs jest projektowany przede wszystkim pod telefon, ale ma działać również na komputerze.

## Najważniejsze moduły

- `Dzisiaj` — skrót dnia, bilans energii i szybkie rozpoczęcie treningu.
- `Jedzenie` — sekcje posiłków definiowane w profilu, produkty, porcje, ulubione oraz dodawanie ręczne i przez AI.
- `Trening` — plany, dni treningowe, ćwiczenia, serie, przerwy, aktywna sesja i podsumowanie.
- `Progres` — kalendarz diety i treningów, masa ciała, obwody, regularność, objętość i siła.
- `Profil` — cele żywieniowe, ustawienia treningowe, sekcje posiłków, tolerancja kalorii i konto.
- `Asystent` — Gemini lub API zgodne z OpenAI, wybierane w panelu administratora. Zmiany planów wymagają zatwierdzenia użytkownika.

## Technologia i struktura

- .NET 8, ASP.NET Core API, Blazor WebAssembly, MudBlazor i Entity Framework Core.
- Baza danych: SQL Server; lokalnie można użyć instancji LocalDB `FormaAIWeb`.
- `src/FormaAI.Api` — API, uwierzytelnianie i hostowanie aplikacji.
- `src/FormaAI.Web` — interfejs użytkownika.
- `src/FormaAI.Domain` — modele domenowe.
- `src/FormaAI.Application` — logika aplikacyjna.
- `src/FormaAI.Contracts` — kontrakty API.
- `src/FormaAI.Infrastructure` — baza danych, Identity i integracje zewnętrzne.
- `tests` — testy domenowe, aplikacyjne i integracyjne.

## Uruchomienie lokalne

```powershell
sqllocaldb start FormaAIWeb
$env:ConnectionStrings__FormaAI = 'Server=(localdb)\FormaAIWeb;Database=FormaAI;Trusted_Connection=True;TrustServerCertificate=True'
$env:Admin__Email = 'demo.admin@formaai.pl'
dotnet run --project src/FormaAI.Api/FormaAI.Api.csproj --urls http://0.0.0.0:5080
```

Konto demonstracyjne:

- login: `demo.admin@formaai.pl`
- hasło: `FormaAI-Demo-2026!`

Do ponownego dodania danych demonstracyjnych służy `scripts/seed-demo-data.ps1`.

## Zasady dalszej pracy

- Nie zmieniaj całego kierunku wizualnego; rozwijaj istniejący styl FormaAI.
- Projektuj procesy jako krótkie, czytelne kroki odpowiednie dla telefonu.
- Nie zapisuj kluczy API w repozytorium, dokumentacji ani kodzie frontendu. Klucz ma pozostać po stronie serwera lub w zaszyfrowanej konfiguracji administratora.
- Każdy większy, zamknięty moduł zapisuj jako osobny commit z polskim opisem tego, co zostało wykonane.
- Po zmianach uruchom `dotnet build FormaAI.sln` i `dotnet test FormaAI.sln --no-build`.
