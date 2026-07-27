# Osobny kreator i tryb sesji treningowej

## Cel

Przycisk „Wpisz trening” ma przenosić na osobną stronę, zamiast rozwijać formularz na pulpicie. Użytkownik najpierw przygotowuje trening ręcznie albo z AI, sprawdza szkic, a następnie rozpoczyna pełnoekranową sesję przeznaczoną do wpisywania serii, ciężaru i powtórzeń.

## Potwierdzona przyczyna starego widoku

Pulpit `Home.razor` przechowuje stan `_quickWorkoutOpen`, a `ToggleQuickWorkout` rozwija lokalny panel `quick-workout-builder`. Nowy widok sesji istnieje pod `/workout/{id}`, lecz jest osiągalny dopiero po utworzeniu sesji. Flow AI dla wykonanego treningu znajduje się osobno w Asystencie. Te trzy elementy nie tworzą jednego procesu.

## Docelowa nawigacja

1. Wszystkie akcje „Wpisz trening” bez aktywnej sesji prowadzą do `/workout/new`.
2. Gdy aktywna sesja już istnieje, akcja prowadzi bezpośrednio do `/workout/{id}`.
3. `/workout/new` jest osobną stroną i nie jest dialogiem ani rozwijanym panelem.
4. Po zatwierdzeniu ręcznego szkicu strona tworzy szybką sesję i przechodzi do `/workout/{id}`.
5. Po zatwierdzeniu szkicu AI strona zapisuje trening dopiero po wyraźnej decyzji użytkownika. Dla treningu, który ma być wykonany teraz, zaakceptowany szkic jest zamieniany na szybką sesję i otwierany w `/workout/{id}`.

## Strona `/workout/new`

### Nagłówek

- przycisk powrotu;
- tytuł „Nowy trening”;
- krótka informacja, że trening zostanie zapisany dopiero po zatwierdzeniu;
- dwa wyraźne wejścia: główne „Dodaj z AI” i drugorzędne „Dodaj ręcznie”.

### Tryb AI

- pole tekstowe z przykładem: „Zrobiłem wyciskanie 80 kg 4×8, wiosłowanie 4×10…”;
- przycisk „Przygotuj trening”;
- AI zwraca edytowalny szkic z nazwą treningu, ćwiczeniami oraz seriami;
- każdą serię można poprawić pod względem ciężaru, powtórzeń i RIR;
- można usunąć ćwiczenie albo serię;
- przed zatwierdzeniem nic nie jest zapisane jako ukończony trening ani aktywna sesja;
- główna akcja dla bieżącego treningu brzmi „Rozpocznij ten trening”.

### Tryb ręczny

- nazwa treningu i planowany czas;
- wyszukiwarka ćwiczeń;
- lista wybranych ćwiczeń w kolejności wykonania;
- dla każdego ćwiczenia: liczba serii, zakres powtórzeń, RIR, przerwa oraz opcjonalny interwał;
- możliwość połączenia co najmniej dwóch ćwiczeń w superserię;
- możliwość usunięcia i zmiany kolejności ćwiczeń;
- podsumowanie liczby ćwiczeń i serii;
- akcja „Rozpocznij trening”.

## Sesja `/workout/{id}`

Istniejąca strona pozostaje miejscem wpisywania faktycznego wykonania. Ma działać jak widok stricte workout:

- duży obraz lub GIF aktywnego ćwiczenia u góry;
- nazwa ćwiczenia i postęp całej sesji;
- przerwa oraz interwał widoczne bez rozwijania dodatkowych paneli;
- tabela serii z polami kg, powtórzenia i RIR;
- zapis serii pojedynczą główną akcją;
- wymiana ćwiczenia bez opuszczania sesji;
- pasek superserii pokazujący kolejność i rundę;
- następne ćwiczenie oraz zakończenie treningu;
- wygląd zgodny z dostarczonymi referencjami, przy zachowaniu kolorystyki FormaAI.

## Ponowne użycie istniejącej logiki

- `TrainingClient.StartQuick` nadal tworzy ręczną szybką sesję.
- Istniejące API sesji obsługuje zapis serii, wymianę, przerwy, interwały i superserie.
- Istniejący endpoint szkicu wykonanego treningu AI pozostaje źródłem rozpoznawania tekstu.
- Logika formularza szkicu zostanie wydzielona z `Assistant.razor` do współdzielonego komponentu, aby `/assistant` i `/workout/new` nie duplikowały walidacji.
- Samo wygenerowanie szkicu AI nie wywołuje endpointu potwierdzającego.

## Usunięcie starego interfejsu

Z `Home.razor` zostaną usunięte:

- `_quickWorkoutOpen` i pozostały stan lokalnego kreatora;
- `ToggleQuickWorkout`, `CloseQuickWorkout`, wyszukiwanie i budowanie szybkiego treningu;
- sekcja `quick-workout-builder`;
- nieużywane style tego panelu.

Przycisk na pulpicie stanie się zwykłą nawigacją do `/workout/new` albo do aktywnej sesji.

## Stany i błędy

- Brak ćwiczeń blokuje rozpoczęcie sesji i pokazuje jasny komunikat.
- Niekompletne serie w szkicu AI są zaznaczone przy konkretnym ćwiczeniu.
- Błąd AI nie usuwa wpisanego opisu.
- Konflikt aktywnej sesji przekierowuje do istniejącej sesji.
- Odświeżenie `/workout/new` nie może przypadkowo zapisać treningu.
- Wielokrotne kliknięcie akcji startowej jest blokowane do zakończenia żądania.

## Responsywność i dostępność

- Na telefonie kreator zajmuje pełną szerokość i prowadzi jedną kolumną.
- Na desktopie zawartość ma ograniczoną szerokość, ale nadal zachowuje kolejność mobilnego flow.
- Wszystkie przyciski ikonowe mają etykiety dostępności.
- Pola serii mają jednoznaczne etykiety, a komunikaty błędów są powiązane z właściwym etapem.
- Sterowanie klawiaturą i widoczny fokus pozostają dostępne.

## Kryteria akceptacji

1. Kliknięcie „Wpisz trening” na pulpicie zmienia URL na `/workout/new`; stary panel nigdy się nie rozwija.
2. Na początku `/workout/new` widoczny jest przycisk „Dodaj z AI”.
3. Ręczny szkic można utworzyć, sprawdzić i rozpocząć jako sesję.
4. Szkic AI jest widoczny i edytowalny przed jakimkolwiek zapisem.
5. Po rozpoczęciu użytkownik trafia do `/workout/{id}` z mediami, seriami, timerami, superserią i wymianą ćwiczenia.
6. Istniejąca aktywna sesja jest wznawiana, zamiast tworzyć drugą.
7. Build rozwiązania i wszystkie testy przechodzą.
