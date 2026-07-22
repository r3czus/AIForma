# Forma Signal — specyfikacja redesignu

## Cel

Przekształcić istniejący interfejs FormaAI w czytelny, motywujący panel codziennego działania, bez zmiany logiki biznesowej i kontraktów API.

## Zakres

- Ujednolicenie tokenów, typografii, odstępów, geometrii i stanów interakcji.
- Przebudowa shellu, górnego paska i nawigacji mobilnej/desktopowej.
- Wzmocnienie hierarchii ekranu Dzisiaj oraz skrócenie drogi do dodania posiłku i treningu.
- Uporządkowanie wizualne Jedzenia, Treningu, Progresu, Profilu i Asystenta.
- Zachowanie jasnego i ciemnego motywu.
- Pełna adaptacja od 320 px do szerokiego desktopu.

## Poza zakresem

- Zmiany modeli domenowych, API, bazy danych i reguł obliczeń.
- Automatyczne zapisywanie propozycji AI bez zatwierdzenia.
- Nowe mechanizmy grywalizacji, powiadomienia lub integracje.

## Główne decyzje

1. Najczęstsze działania są widoczne przed analityką.
2. Powierzchnie są spłaszczone: mniej kart, więcej sekcji i wierszy.
3. AI jest częścią przepływu dodawania, a nie wyłącznie osobnym chatem.
4. Dane mają jedną dominującą wartość i spokojniejsze szczegóły.
5. Nawigacja zmienia się na boczną już na tablecie.

## Kryteria akceptacji

- Użytkownik może przejść do ręcznego dodania jedzenia i tworzenia treningu jednym dotknięciem z właściwego modułu.
- Pomoc AI jest widoczna obok ręcznej ścieżki, ale nie dominuje całego ekranu.
- Interfejs nie zawiera grubych bocznych pasków na zaokrąglonych kartach.
- Tekst pomocniczy ma co najmniej 12 px, a niestandardowe cele dotykowe co najmniej 44 px.
- Widoki działają przy szerokościach 320, 390, 768, 1024 i 1440 px.
- `dotnet build FormaAI.sln` oraz `dotnet test FormaAI.sln --no-build` kończą się powodzeniem.
- Publiczny link Cloudflare odpowiada, a `/health/live` zwraca sukces.
