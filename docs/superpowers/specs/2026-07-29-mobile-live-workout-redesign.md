# Mobilny ekran aktywnej sesji — projekt

**Data:** 2026-07-29  
**Status:** zaakceptowany kierunek wizualny, oczekuje na przegląd specyfikacji  
**Zakres:** `/workout/{id}` oraz pełnoekranowe panele historii, zamiany ćwiczenia i menu sesji

## Cel

Przebudować mobilny ekran aktywnego treningu tak, aby wiernie odpowiadał przekazanym referencjom: duże medium ćwiczenia u góry, płaska i czytelna tabela serii, akcje umieszczone przy obrazie oraz pełnoekranowe panele zadań pobocznych. Zachować kolorystykę i typografię Forma AI oraz całą istniejącą funkcjonalność sesji.

Telefon jest podstawowym środowiskiem. Ekran ma być wygodny jedną ręką, czytelny między seriami i odporny na przypadkowe kliknięcia.

## Źródła wizualne i zasady ujednolicenia

Referencje są traktowane jako jedna rodzina interfejsu:

- ekran aktywnego ćwiczenia wyznacza główną hierarchię i proporcje;
- ekran „Zamień ćwiczenie” wyznacza wygląd wyszukiwarki, filtrów i listy zamienników;
- ekran historii wyznacza pełnoekranową nawigację zakładkową i tabele wyników;
- dodatkowe widoki planów określają stosowanie cienkich linii, płaskich powierzchni, prostych ikon i oszczędnych obramowań.

W całym przepływie obowiązują:

- jasne mineralne tło Forma AI i białe powierzchnie;
- granatowo-niebieski `--action` dla aktywnego stanu i głównych akcji;
- ciemny `--ink`, stonowany `--muted` i delikatne zielonkawe separatory;
- Barlow Semi Condensed dla dużych nazw i Onest dla interfejsu;
- IBM Plex Mono z cyframi tabularnymi dla czasu, ciężaru, powtórzeń i 1RM;
- ograniczenie cieni, kapsułek i zagnieżdżonych kart;
- cele dotykowe minimum 44 × 44 px oraz widoczny focus.

## Główny ekran aktywnego ćwiczenia

### Rama

Na trasie aktywnego treningu standardowy górny pasek i dolna nawigacja aplikacji pozostają ukryte. Widok zajmuje całe okno telefonu z obsługą bezpiecznych obszarów iOS. Na szerszych ekranach ten sam interfejs jest wyśrodkowany i ograniczony do szerokości odpowiadającej telefonowi; nie rozciąga się w szeroki dashboard.

### Obszar medium

Górną część zajmuje zdjęcie, GIF lub film aktywnego ćwiczenia o szerokości ekranu. Obraz ma dominować wizualnie i zachować kadr z referencji.

Na medium znajdują się:

- przycisk powrotu po lewej;
- przycisk historii/wykresu;
- przycisk zamiany ćwiczenia;
- przycisk menu „…” po prawej;
- kontrolka odtwarzania lub zatrzymania dla materiałów ruchomych.

Pod dolną krawędzią obrazu znajduje się segmentowy wskaźnik pozycji w treningu. Aktywny segment używa koloru `--action`.

Przeciągnięcie medium w lewo wybiera następne ćwiczenie, a w prawo poprzednie. Gest nie uruchamia się podczas pionowego przewijania i nie działa poza skrajnymi ćwiczeniami. Przyciski oraz wskaźnik pozostają pełnoprawną alternatywą dostępną klawiaturą i czytnikiem ekranu.

### Nagłówek ćwiczenia

Bezpośrednio pod medium znajdują się:

- duża nazwa aktywnego ćwiczenia;
- po prawej szacowane 1RM, jeśli istnieją dane;
- krótka linia celu: liczba serii, zakres powtórzeń i opcjonalny RIR;
- oznaczenie presetów AI, jeśli zostały przygotowane.

### Timery

Przerwa oraz interwał są dwoma zwartymi kontrolkami pod nazwą. Aktywny timer wyświetla pozostały czas w sposób dominujący, umożliwia pauzę, wznowienie i reset oraz zachowuje istniejący sygnał końca przerwy. Nieaktywny interwał pokazuje ustawioną wartość lub skrót „stoper”.

### Serie

Serie są płaską tabelą bez karty:

- kolumny: seria, kg, powtórzenia, RIR i stan;
- zapisane serie mają czytelny znacznik wykonania i można je nacisnąć, aby edytować;
- aktualna seria ma niebieski numer, aktywne pola i niebieskie obramowanie;
- przyszłe serie pokazują presety lub planowany zakres w stonowanym kolorze;
- błędy walidacji pojawiają się bezpośrednio pod aktualnym wierszem;
- dodanie kolejnej serii pozostaje możliwe zgodnie z obecną logiką.

Główna akcja „Zapisz serię” lub „Zapisz poprawioną serię” jest przyklejona przy dolnej krawędzi ekranu z uwzględnieniem bezpiecznego obszaru. Nie zasłania tabeli i zachowuje stan ładowania.

### Superseria

Jeśli aktywne ćwiczenie należy do superserii, pod tabelą pojawia się płaski poziomy tor z:

- nazwą i numerem rundy;
- ćwiczeniami A1, A2 itd.;
- miniaturami, nazwami i liczbą serii;
- wyróżnieniem aktualnego ćwiczenia;
- możliwością przejścia do innego członka superserii.

Dodawanie lub edycja superserii nie jest osobnym przyciskiem w głównym układzie. Jest dostępne z menu „…”.

## Pełnoekranowa historia i wykres

Kliknięcie ikony wykresu otwiera panel na całe okno, nad aktywną sesją. Zamknięcie panelu wraca do tego samego ćwiczenia, pozycji przewijania, danych formularza i stanu timerów.

Nagłówek panelu zawiera powrót, nazwę Forma AI i menu. Poniżej widoczne są medium ćwiczenia, nazwa, główne mięśnie, sprzęt oraz zakładki:

- **Historia** — sesje pogrupowane datami; tabela serii, ciężaru × powtórzeń i szacowanego 1RM;
- **Wykres** — trend najlepszego szacowanego 1RM z kolejnych sesji;
- **Technika** — istniejący opis ćwiczenia i możliwość przejścia do pełnych szczegółów, jeżeli dane są dostępne.

Historia i wykres pobierają pełną historię aktywnego ćwiczenia przez istniejącego klienta API. Brak danych pokazuje prosty stan pusty, bez atrap.

## Pełnoekranowa zamiana ćwiczenia

Kliknięcie ikony zamiany otwiera pełnoekranowy panel:

- zwarty podgląd obecnego ćwiczenia z miniaturą, planem i stanem serii;
- tytuł „Zamień ćwiczenie”;
- wyszukiwarka;
- filtry partii, sprzętu oraz podobieństwa;
- pionowa lista zamienników ze zdjęciami;
- wyróżniony wiersz aktualnego ćwiczenia;
- osobny przycisk zamiany w każdym możliwym zamienniku;
- dolny pasek „Anuluj” i potwierdzenie wybranego zamiennika.

Wykonane serie zachowują się zgodnie z obecną logiką. Panel obsługuje Escape, focus i blokadę podwójnego zapisu.

## Menu „…”

Menu rozwija zwarty arkusz akcji. Zawiera:

- utwórz lub edytuj superserię;
- notatkę i typ serii;
- dodaj inne ćwiczenie;
- notatkę do całej sesji;
- zakończ trening i przejdź do podsumowania;
- porzuć trening jako akcję destrukcyjną.

Kreator superserii zachowuje wybór 2–5 ćwiczeń, kolejność, liczbę rund, interwał między ćwiczeniami i przerwę po rundzie.

## Zachowana funkcjonalność

Redesign nie zmienia kontraktów API ani modelu danych. Muszą pozostać:

- zegar całej sesji i postęp;
- sesje siłowe i cardio;
- nawigacja między ćwiczeniami;
- zapis, walidacja i edycja serii;
- presety AI;
- przerwa, interwał i sygnał końca timera;
- ostatni wynik oraz sugestia progresji;
- zamiana ćwiczenia;
- tworzenie i obsługa superserii;
- dodawanie ćwiczeń i notatek;
- zakończenie, porzucenie oraz podsumowanie sesji;
- rekomendacje progresji po zakończeniu.

## Stany i obsługa błędów

- Ładowanie sesji używa szkieletu odpowiadającego nowemu układowi.
- Brak medium pokazuje obecny markowy placeholder w tym samym rozmiarze co obraz.
- Niedostępna historia i pusta lista zamienników mają dedykowane stany puste.
- Błąd zapisu pozostawia wpisane dane i pokazuje komunikat przy właściwej akcji.
- Przyciski zapisujące blokują wielokrotne wysłanie.
- Gest zmiany ćwiczenia nie usuwa ani nie resetuje niezapisanych danych formularza.

## Responsywność

- 320–600 px: pełnoekranowy układ referencyjny.
- 601–900 px: układ telefonu wyśrodkowany, z nieco większym medium.
- Powyżej 900 px: wyśrodkowany talerz roboczy o ograniczonej szerokości; panele historii i zamiany mogą użyć większej wysokości i szerokości, ale zachowują tę samą hierarchię.
- Brak poziomego przewijania całej strony.
- Przyklejona akcja uwzględnia `env(safe-area-inset-bottom)`.

## Weryfikacja

Implementacja zostanie sprawdzona przez:

- testy źródłowe zabezpieczające obecność kluczowych kontrolek i paneli;
- testy logiki treningowej już obecne w rozwiązaniu;
- kompilację całego rozwiązania;
- render mobilny przy szerokościach 320, 390 i 430 px;
- sprawdzenie desktopu;
- ręczne sprawdzenie gestu, edycji serii, timerów, zamiany, superserii, historii i menu;
- kontrolę focusu, etykiet ARIA, kontrastu i `prefers-reduced-motion`.

## Kryterium akceptacji

Na telefonie ekran ma być rozpoznawalny jako wierne odwzorowanie przekazanych screenów: obraz ćwiczenia dominuje u góry, treść jest płaska i zwarta, aktualna seria jest oczywista, a historia, zamiana i superseria znajdują się dokładnie pod wskazanymi ikonami. Jednocześnie wszystkie obecne przepływy treningowe nadal działają.
