# Forma Signal Craft Pass - specyfikacja

## Cel

Podnieść istniejący redesign Forma Signal z poziomu spójnej warstwy wizualnej do poziomu dopracowanego produktu codziennego użytku. Zmiana nie dotyka logiki biznesowej, tras, kontraktów API ani kolejności pól formularzy.

## Odczyt projektu

FormaAI jest mobilną aplikacją operacyjną używaną wiele razy dziennie. Interfejs ma przypominać precyzyjny dziennik treningowy połączony z czytelnym pulpitem dnia, a nie zestaw niezależnych kart ani futurystyczny panel AI.

- `DESIGN_VARIANCE: 6` - charakterystyczny układ, ale bez eksperymentów utrudniających skanowanie.
- `MOTION_INTENSITY: 4` - szybkie sprzężenie zwrotne i krótkie przejścia stanu.
- `VISUAL_DENSITY: 7` - dużo danych, porządkowanych typografią, liniami i rytmem zamiast kolejnymi kontenerami.

## Rozważone podejścia

### 1. Nowa identyfikacja od zera

Największa zmiana wizualna, ale niepotrzebnie odrzuca działający język Forma Signal, zwiększa ryzyko regresji i łamie rozpoznawalność produktu.

### 2. Kolejna warstwa samych nadpisań CSS

Najszybsza, ale utrwala konflikt pomiędzy `app.css` i `forma-signal.css`, nie rozwiązuje niespójnych stanów ładowania ani niedopracowanych interakcji.

### 3. Systemowy craft pass - wybrane

Zachowuje markę i układ informacji, a poprawia wspólne fundamenty, shell, stany ładowania, ruch, minimalne rozmiary tekstu, responsywność i wybrane powierzchnie o największej częstotliwości użycia.

## Kontrakt kierunku

**THESIS:** FormaAI jest pulpitem kolejnego działania. Odrzuca układ, w którym każda informacja potrzebuje własnej karty.

**OWN-WORLD:** Mineralne tło, białe i grafitowe powierzchnie, kobaltowa akcja, pomarańczowe paliwo i zielone wykonanie. Kontrolki mają 10 px promienia, powierzchnie 15 px, a głębokość pojawia się tylko przy elementach unoszących się.

**STORY:** Użytkownik rozpoznaje stan dnia, wybiera następny krok i zapisuje jedzenie albo trening bez szukania.

**FIRST VIEWPORT:** Kompaktowy shell, jednoznaczny nagłówek dnia, pas szybkich działań i Sygnał dnia. Szczegóły pojawiają się później.

**FORM:** Ukierunkowana ewolucja istniejącego świata Forma Signal. Priorytet: czytelność operacyjna, potem ekspresja.

## Zakres

### Fundamenty

- Wspólne krzywe ruchu, czasy, minimalny rozmiar tekstu 12 px i dotyk minimum 44 px.
- Tylko jawnie wskazane właściwości w `transition`; bez `transition: all`.
- Przycisk i każdy element naciskalny reagują subtelną skalą `0.98` przez 120-160 ms.
- `prefers-reduced-motion` usuwa ruch przestrzenny i pozostawia natychmiastowe zmiany koloru oraz widoczności.

### Shell

- Telefon zachowuje dolną nawigację w strefie kciuka.
- Tablet używa zwartej szyny.
- Szeroki desktop otrzymuje czytelniejszą szynę z etykietami i lepszym wykorzystaniem szerokości.
- Górny pasek i nawigacja mają wspólną geometrię, focus i aktywny stan.

### Ładowanie

- Główne ekrany używają jednego komponentu szkieletowego zgodnego z docelowym układem.
- Małe, chwilowe operacje mogą zachować spinner.
- Szkielet ma komunikat dla technologii asystujących i nie porusza się przy ograniczeniu ruchu.

### Główne powierzchnie

- `Dzisiaj`: wyraźna kolejność nagłówek, szybkie działanie, Sygnał dnia, trening, żywienie.
- `Jedzenie` i `Trening`: gęste wiersze, lepsza czytelność metadanych, wyraźne akcje podstawowe.
- `Progres`: dane liczbowe ponad dekoracją, czytelne filtry i kalendarz od 320 px.
- `Profil` i `Asystent`: spokojniejsze powierzchnie, wyraźne stany wyboru i zatwierdzania.

## Kryteria akceptacji

- Żaden cel dotykowy nie jest mniejszy niż 44 x 44 px.
- Żaden celowo mały tekst interfejsu nie jest mniejszy niż 12 px.
- Brak poziomego przewijania całej strony przy 320 px.
- Ruch ma funkcję: wejście, stan lub sprzężenie zwrotne.
- Jasny i ciemny motyw używają tej samej hierarchii.
- Główne widoki nie pokazują generycznego spinnera podczas pierwszego ładowania.
- `dotnet build FormaAI.sln` kończy się kodem 0.
- `dotnet test FormaAI.sln --no-build` kończy się kodem 0.
- Detektor Impeccable nie zgłasza nierozwiązanych usterek dla zmienionych plików.

## Poza zakresem

- Zmiana brandu, logo, tras i etykiet głównej nawigacji.
- Nowe funkcje, modele, API lub zależności frontendowe.
- Zmiana treści prawnych, mechaniki AI lub automatyczne zatwierdzanie propozycji.
- Dekoracyjne fotografie i marketingowe sekcje. To produkt operacyjny, nie landing page.
