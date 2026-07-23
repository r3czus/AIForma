# Jak wystawić FormaAI do testów na telefonie

Ta instrukcja opisuje tymczasowe wystawienie aktualnej wersji aplikacji przez Cloudflare Quick Tunnel. Jest to dobre rozwiązanie do testów na telefonie, ale nie jest wdrożeniem produkcyjnym: adres może zmienić się po ponownym uruchomieniu tunelu i działa tylko wtedy, gdy komputer, aplikacja oraz tunel są uruchomione.

## 1. Przygotuj projekt

Repozytorium znajduje się w:

```text
C:\Users\Jannu\OneDrive\Pulpit\AIFroma
```

Przed publikacją sprawdź aktualny stan i zbuduj rozwiązanie:

```powershell
Set-Location 'C:\Users\Jannu\OneDrive\Pulpit\AIFroma'
git status --short
dotnet build FormaAI.sln
dotnet test FormaAI.sln --no-build
```

Nie publikuj wersji, która nie przechodzi budowania. Nie umieszczaj klucza Gemini w kodzie, repozytorium, logach ani w tym pliku.

## 2. Uruchom bazę LocalDB

```powershell
sqllocaldb start FormaAIWeb
```

Jeżeli instancja jeszcze nie istnieje:

```powershell
sqllocaldb create FormaAIWeb
sqllocaldb start FormaAIWeb
```

## 3. Uruchom aplikację

Najpierw ustaw konfigurację tylko dla bieżącego okna PowerShell:

```powershell
$env:ConnectionStrings__FormaAI = 'Server=(localdb)\FormaAIWeb;Database=FormaAI;Trusted_Connection=True;TrustServerCertificate=True'
$env:Admin__Email = 'admin.formaai@local.pl'
Remove-Item Env:HTTP_PROXY, Env:HTTPS_PROXY, Env:ALL_PROXY -ErrorAction SilentlyContinue
dotnet run --project src/FormaAI.Api/FormaAI.Api.csproj --urls http://0.0.0.0:5082
```

Port `5082` można zastąpić innym wolnym portem. Aplikacja powinna odpowiadać pod adresem:

```text
http://127.0.0.1:5082/health/live
```

## 4. Wystaw aplikację przez Cloudflare Tunnel

W drugim oknie PowerShell uruchom:

```powershell
cloudflared tunnel --url http://127.0.0.1:5082 --no-autoupdate
```

Po chwili w konsoli pojawi się adres podobny do:

```text
https://przykladowa-nazwa.trycloudflare.com
```

Skopiuj cały adres HTTPS i otwórz go w Safari na iPhonie. Telefon nie musi być w tej samej sieci Wi-Fi co komputer.

## 5. Sprawdź wersję przed przekazaniem linku

1. Otwórz publiczny adres w przeglądarce.
2. Zaloguj się kontem testowym.
3. Sprawdź zakładki `Dzisiaj`, `Jedzenie`, `Progres`, `Trening` i `Profil`.
4. Włącz jasny i ciemny motyw.
5. W `Jedzenie` wykonaj próbne zapytanie w trybie `Opis AI`.
6. Sprawdź widok mobilny oraz instalację aplikacji przez `Udostępnij → Do ekranu początkowego` w Safari.

## 6. Zasady bezpieczeństwa

- Klucz API pozostaje po stronie serwera i jest zapisywany przez panel administratora w zaszyfrowanej postaci.
- Publicznie przekazuj wyłącznie adres aplikacji oraz dane konta testowego.
- Quick Tunnel nie ma gwarancji dostępności i nie powinien obsługiwać prawdziwych danych użytkowników.
- Po zakończeniu testów zamknij proces aplikacji i `cloudflared`; publiczny adres przestanie działać.
- Do stałej publikacji użyj hostingu z HTTPS, trwałą bazą SQL Server, kopią zapasową, własną domeną i nazwanym tunelem lub reverse proxy.

## Najczęstsze problemy

### Adres publiczny pokazuje błąd 502

Aplikacja nie działa na porcie przekazanym do `cloudflared`. Sprawdź adres `/health/live` lokalnie i uruchom tunel ponownie z właściwym portem.

### AI długo nie odpowiada

Sprawdź konfigurację modelu i klucza w panelu administratora. Upewnij się też, że proces aplikacji nie odziedziczył błędnych zmiennych `HTTP_PROXY`, `HTTPS_PROXY` lub `ALL_PROXY`.

### Link przestał działać

Quick Tunnel jest tymczasowy. Uruchom `cloudflared` ponownie i przekaż nowy wygenerowany adres.
