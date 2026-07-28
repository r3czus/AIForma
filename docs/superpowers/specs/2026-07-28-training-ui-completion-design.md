# Domknięcie UI modułu Trening

## Cel

Domknąć cały moduł `Trening` na bazie implementacji scalonej do `main`, bez zmiany globalnego kierunku FormaAI. Zakres obejmuje ekran główny treningu, plany, przygotowanie sesji, aktywną sesję, zapisywanie serii, superserie, zamianę ćwiczenia, szczegóły ćwiczenia oraz wspólną obsługę zdjęć, GIF-ów i filmów.

Projekt korzysta z lokalnych referencji:

- `docs/design/training-references/active-session.png`
- `docs/design/training-references/details.png`
- `docs/design/training-references/plan.png`
- `docs/design/training-references/swap.png`

Referencje definiują hierarchię, proporcje i ergonomię. Kolory, typografia, nawigacja oraz język interfejsu pozostają zgodne z `DESIGN.md`.

## Odbiorca i sytuacja użycia

Główny użytkownik korzysta z aplikacji na telefonie, często jedną ręką i pomiędzy seriami. Najważniejsze są:

- natychmiastowe rozpoznanie aktualnego ćwiczenia i następnej czynności;
- zapis serii bez otwierania dodatkowego formularza;
- bezpieczna korekta wartości;
- szybkie przejście przez superserię;
- czytelne media techniczne bez utraty miejsca na dane treningowe.

Tryb powierzchni to `Operate`. Szybkość, przewidywalność i stan zadania mają pierwszeństwo przed dekoracją.

## Rozważone podejścia

### Ewolucja istniejącego modułu, wybrane

Zachowujemy routing, kontrakty oraz działającą logikę, a przebudowujemy hierarchię komponentów, układ i brakujące interakcje. Ryzyko regresji jest najmniejsze, a rezultat może dokładnie odpowiadać referencjom.

### Adaptacja referencji jeden do jednego

Układ byłby kopiowany możliwie dosłownie. Dałoby to szybkie podobieństwo wizualne, ale osłabiłoby spójność z Forma Signal i nie rozwiązałoby wszystkich stanów domenowych.

### Pełna wymiana warstwy treningowej

Nowe widoki i komponenty powstałyby od zera. Zapewniłoby to czystą strukturę, ale zwiększyłoby zakres, ryzyko utraty istniejących funkcji i czas potrzebny na ponowną walidację.

## Kierunek wizualny

Projekt jest zachowawczym redesignem istniejącego produktu:

- `DESIGN_VARIANCE: 5`
- `MOTION_INTENSITY: 3`
- `VISUAL_DENSITY: 7`

Obowiązuje system Forma Signal i MudBlazor. Nie dodajemy drugiej biblioteki komponentów.

- Tło pozostaje mineralne i jasne, powierzchnie robocze białe.
- Niebieski oznacza akcję i aktywny stan.
- Zieleń oznacza wykonanie oraz gotowość.
- Dane liczbowe używają kroju mono.
- Nagłówki są zwarte i charakterystyczne, lecz nie konkurują z wartościami serii.
- Listy używają rytmu, odstępów i pojedynczych separatorów zamiast zagnieżdżonych kart.
- Promień narożników jest konsekwentny: kontrolki 10 px, główne powierzchnie 14-16 px.
- Wszystkie cele dotykowe mają co najmniej 44 na 44 px.

## Struktura modułu

### Ekran Trening

Moduł zachowuje trzy podstawowe sekcje:

1. `Trening`
2. `Plany`
3. `Ćwiczenia`

Sekcja `Trening` pokazuje najpierw dzisiejszą sesję i jedną wyraźną akcję rozpoczęcia. Wariant pełny, skrócony lub minimum jest wyborem w obrębie tej samej powierzchni, a nie osobną kartą.

Niżej znajduje się krótka historia ostatnich sesji. Dalsza historia pozostaje dostępna w dedykowanym widoku, aby ekran startowy nie zamieniał się w długą tabelę.

### Plany

Na telefonie dni planu są poziomym selektorem, a ćwiczenia wybranego dnia tworzą jedną pionową listę. Na desktopie selektor wykorzystuje pełną szerokość, lecz lista nadal pozostaje pod nim.

Wiersz ćwiczenia zawiera:

- kolejność;
- nazwę i grupy mięśniowe;
- serie, zakres powtórzeń i przerwę;
- miniaturę medium;
- oznaczenie superserii;
- przejście do szczegółów.

### Przygotowanie sesji

Widok ręczny oraz podgląd AI używają tego samego modelu wiersza ćwiczenia. Najważniejsze wartości są widoczne od razu, a RIR, interwał i ustawienia superserii pozostają dostępne bez rozbijania przepływu na wiele ekranów.

Akcja rozpoczęcia lub zapisania wykonanego treningu pozostaje przyklejona na telefonie i zawsze opisuje skutek.

## Aktywna sesja i serie

Aktywna sesja jest zbudowana wokół jednego aktualnego ćwiczenia.

Kolejność informacji:

1. kompaktowy pasek sesji z powrotem, postępem i menu;
2. medium ćwiczenia;
3. nazwa, przerwa, interwał oraz działania kontekstowe;
4. tabela serii;
5. pasek superserii lub nawigacja do kolejnego ćwiczenia;
6. przyklejona akcja główna.

Każdy wiersz serii ma jeden z czterech stanów:

- zaplanowany;
- aktywnie edytowany;
- zapisany;
- błędny.

Wartości `kg`, `powtórzenia` i `RIR` są edytowalne bez dodatkowego modala. Zapisany wiersz można dotknąć, aby przejść do korekty. Typ serii i notatka pozostają w rozwijanych szczegółach, ponieważ nie są potrzebne przy każdym podejściu.

Główna akcja zmienia etykietę zgodnie ze stanem:

- `Zapisz serię`
- `Zapisz poprawioną serię`
- `Przejdź do następnego ćwiczenia`
- `Zakończ trening`

Nie uruchamiamy animacji dla częstych operacji wpisywania. Przycisk daje krótką informację zwrotną przy naciśnięciu, a zapisany stan przechodzi w potwierdzenie w czasie poniżej 220 ms.

## Superserie

Tworzenie i edycja superserii odbywa się w pełnoekranowym panelu na telefonie oraz w szerokim panelu na desktopie.

Użytkownik może:

- wybrać od 2 do 5 ćwiczeń;
- zmienić ich kolejność;
- ustawić liczbę rund;
- ustawić interwał między ćwiczeniami;
- ustawić przerwę po rundzie;
- potwierdzić przeniesienie ćwiczenia z innej superserii.

Podczas sesji pasek superserii pokazuje kolejność, bieżące ćwiczenie oraz numer rundy. Po zapisaniu serii ekran przechodzi do kolejnego członka bez pełnej przerwy. Pełna przerwa uruchamia się po ostatnim ćwiczeniu rundy.

## Zamiana ćwiczenia

Na telefonie zamiana jest pełnym widokiem, a na desktopie panelem zachowującym kontekst sesji.

Widok zawiera:

- aktualne ćwiczenie i stan wykonanych serii;
- wyszukiwanie;
- filtry mięśni, sprzętu i podobieństwa;
- wyniki z miniaturą i metadanymi;
- wyraźnie zaznaczony wybór;
- stałe akcje `Anuluj` i `Potwierdź zamianę`.

Wykonane serie pozostają przypisane do pierwotnego ćwiczenia. Pozostały zakres przejmuje zamiennik.

## Szczegóły ćwiczenia

Szczegóły łączą medium, metadane i trzy sekcje:

- `Historia`
- `Wykres`
- `Technika`

Historia jest czytelna bez poziomego przewijania strony. Na telefonie dane serii są grupowane pod datą, a na większym ekranie mogą używać zwartej tabeli.

## Ujednolicone media ćwiczeń

Powstaje jeden komponent prezentacyjny i jeden wspólny model zachowania mediów używany w planie, aktywnej sesji, szczegółach i zamianie.

Obsługiwane formaty:

- obrazy `JPG`, `JPEG`, `PNG`, `WebP`;
- animacje `GIF`;
- filmy `MP4`, `WebM`.

Wspólne zasady:

- typ medium wynika z zatwierdzonego MIME i rozszerzenia po stronie serwera;
- plik otrzymuje bezpieczną nazwę generowaną przez serwer;
- interfejs pokazuje postęp, błąd formatu i błąd rozmiaru bez utraty formularza;
- własne medium ma pierwszeństwo przed obrazem startowym;
- film ma `playsinline`, kontrolę odtwarzania i wyciszenie jako bezpieczny stan początkowy;
- GIF nie uruchamia się automatycznie przy włączonym ograniczeniu ruchu;
- brak medium daje spójny placeholder o stałych proporcjach;
- układ rezerwuje proporcje przed załadowaniem, aby nie powodować skoku treści;
- miniatura planu i zamiany nie uruchamia filmu ani GIF-u automatycznie;
- pełne medium jest odtwarzane wyłącznie w aktywnej sesji i szczegółach.

## Stany i błędy

Każdy główny widok ma stan:

- ładowania dopasowany do docelowego układu;
- pusty z następną akcją;
- błędu z możliwością ponowienia;
- zapisu z blokadą podwójnego wysłania;
- sukcesu bez utraty kontekstu.

Błąd pojedynczej serii nie blokuje pozostałych danych. Błąd medium nie blokuje zapisania ćwiczenia bez pliku.

## Responsywność i dostępność

- Brak poziomego przewijania całej strony przy szerokości 320 px.
- Przyklejone akcje nie zasłaniają ostatniego wiersza ani dolnej nawigacji.
- Focus jest widoczny na każdym interaktywnym elemencie.
- Etykiety nie opierają znaczenia wyłącznie na kolorze.
- Ciężar i RIR otwierają klawiaturę dziesiętną, a powtórzenia i czasy klawiaturę numeryczną.
- Ruch przestrzenny jest wyłączony dla `prefers-reduced-motion`.
- Media posiadają opis alternatywny lub etykietę techniczną.

## Kryteria ukończenia

- Wszystkie przepływy treningowe zachowują działającą logikę.
- UI odpowiada hierarchii czterech lokalnych referencji, ale pozostaje częścią Forma Signal.
- Serie można szybko dodać, poprawić i odczytać na telefonie.
- Superseria prowadzi poprawnie przez członków i rundy.
- Zamiana ćwiczenia zachowuje wykonane serie.
- Zdjęcie, GIF i film można dodać, wymienić i poprawnie wyświetlić.
- Widoki są zweryfikowane przy szerokościach telefonu i desktopu.
- `dotnet build FormaAI.sln` przechodzi.
- `dotnet test FormaAI.sln --no-build` przechodzi.
