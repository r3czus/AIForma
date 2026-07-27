# Analiza referencji modułu Trening

## Wybrany kierunek

Trzy wygenerowane ekrany są częściami jednego systemu, a nie alternatywnymi motywami. Zachowujemy świat Forma Signal: jasne mineralne tło, białe powierzchnie robocze, ciemny tekst, kobaltową akcję i zielone wykonanie. Referencje użytkownika oraz wygenerowane widoki wskazują na płaską, sportową hierarchię opartą na dużym obrazie ćwiczenia, mocnym skondensowanym nagłówku, tabelach i cienkich separatorach.

## Plan treningowy

- Maksymalna szerokość robocza na desktopie: około `1180–1280 px`.
- Zewnętrzne marginesy: `24–32 px`; odstęp między głównymi blokami: `18–24 px`.
- Wewnętrzne zakładki mają równą szerokość i wysokość minimum `56 px`.
- Plan jest jedną powierzchnią z liniami podziału, a nie stosem kart.
- Selektor dni ma cztery szerokie pozycje; na telefonie przewija się poziomo.
- Lista ćwiczeń jest pionowa i znajduje się pod selektorem.
- Wiersz desktopowy ma około `72–82 px`; miniatura ma proporcję `16:9` i szerokość `150–170 px`.
- Superseria korzysta z jednego znacznika i linii kolejności. Nie otacza ćwiczeń dodatkową kartą.
- Najważniejsza akcja `Dodaj nowy plan` jest pełna i kobaltowa; edycja pozostaje obrysowana.

## Aktywna sesja

- Górne media wykorzystują proporcję `16:9`, prawie pełną szerokość i promień `12–14 px`.
- Nazwa ćwiczenia jest głównym punktem typograficznym: duża, skondensowana, maksymalnie dwa wiersze.
- Przerwa, interwał, zamiana i superseria tworzą płaski pasek działań bez osobnych kart.
- Tabela serii ma stałą kolejność `Seria / kg / Powt. / RIR`, z liniami pomiędzy wierszami.
- Stan wykonany łączy zielony znak z treścią; aktywna seria łączy kobaltową liczbę, tekst i focus.
- Pasek superserii pokazuje `A1/A2`, miniatury, parametry oraz `runda / liczba rund`.
- Główna akcja jest przyklejona do dolnej strefy kciuka, ale nie zasłania tabeli.

## Podgląd AI

- Czytelny porządek: wynik analizy → nazwa/data → cardio → ćwiczenia → superseria → zatwierdzenie.
- Cardio i ćwiczenia są sekcjami z separatorami; obramowanie obejmuje tylko faktycznie edytowalną grupę.
- Nierozpoznany element ma komunikat treningowy bez terminologii żywieniowej.
- Serie używają tych samych kolumn co aktywna sesja.
- Edytor superserii pokazuje kolejność `A1/A2`, rundy i odpoczynek po rundzie.
- Dwie końcowe akcje mają równą wagę przestrzenną, ale `Rozpocznij trening` pozostaje wypełnioną akcją podstawową.

## Typografia i kolor

- Nagłówki: Barlow Semi Condensed, `700–800`, zwarte, bez śledzenia poniżej `-0.03em`.
- Treść i kontrolki: Onest, minimum `15 px`.
- Czas i wartości: IBM Plex Mono, minimum `12 px`.
- Tło: `#f4f6f3`; powierzchnia: `#ffffff`; tekst: `#17211c`; opis: `#596760`.
- Akcja: `#3451d1`; wykonanie: `#287454`; błąd: `#b93b35`.
- Linie: neutralne, bez kolorowych grubych boków.

## Responsywność i zachowanie

- Telefon: jedna kolumna, akcje w strefie kciuka, poziome selektory wewnętrzne.
- Desktop: plan wykorzystuje pełną szerokość; aktywna sesja pozostaje skupiona w węższej kolumnie.
- Minimalny cel dotykowy: `44 × 44 px`.
- Brak tekstu poniżej `12 px`.
- `prefers-reduced-motion` zatrzymuje automatyczne media i usuwa ruch przestrzenny.
- UI pozostaje semantycznym HTML/Blazor; wygenerowane grafiki są wyłącznie referencjami i mediami ćwiczeń, nigdy rasteryzowanymi kontrolkami.

## Inwentarz implementacyjny

| Element | Medium |
|---|---|
| Zakładki, listy, tabele, formularze | Blazor + semantyczny HTML + CSS |
| Ikony | MudBlazor Material Icons |
| Zdjęcia ćwiczeń | wygenerowane rastry WebP w `wwwroot` |
| Timery i postęp | istniejąca logika C# + CSS |
| Superserie | kontrakt API + logika domenowa + komponenty Blazor |
| Wykres historii | istniejący MudChart |
| Ruch | krótkie przejścia CSS; brak dekoracyjnych animacji |
