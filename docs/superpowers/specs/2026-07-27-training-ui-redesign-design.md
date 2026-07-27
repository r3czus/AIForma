# Przebudowa modułu Trening — projekt

## Cel

Przebudować wyłącznie moduł `Trening` tak, aby plan, przygotowanie treningu, aktywna sesja, szczegóły ćwiczenia i zamiana ćwiczenia miały hierarchię oraz ergonomię z dostarczonych referencji. Globalna nawigacja FormaAI pozostaje bez zmian. Kolory, typografia i charakter marki pozostają spójne z obecną aplikacją.

Dodatkowo:

- opis treningu przekazany AI ma tworzyć edytowalny podgląd treningu, a nie komunikat dotyczący jedzenia;
- użytkownik ma móc rozpocząć pokazany trening albo zapisać go jako już wykonany;
- klikalna nazwa i opis posiłku w `Jedzeniu` mają być wyrównane do lewej.

## Rozważone podejścia

### 1. Jedno wspólne potwierdzenie AI — wybrane

AI przygotowuje jeden edytowalny szkic. Na końcu użytkownik wybiera:

- `Zapisz jako wykonany`;
- `Rozpocznij trening`.

To podejście nie zgaduje intencji użytkownika na podstawie czasu gramatycznego i nie dubluje formularzy.

### 2. Osobne tryby „wykonany” i „do wykonania”

Tryb byłby wybierany przed wpisaniem opisu. Jest jednoznaczny, ale dodaje decyzję przed najważniejszą czynnością i powiela dużą część interfejsu.

### 3. Automatyczne rozpoznawanie intencji

AI samo wybiera zapis historyczny albo aktywną sesję. Rozwiązanie jest krótkie, ale ryzykowne: błędne rozpoznanie mogłoby zapisać trening bez właściwego zatwierdzenia.

## Zakres nawigacji

Globalne zakładki aplikacji i ich położenie pozostają bez zmian.

W module `Trening` obecne zakładki zostaną uproszczone do trzech:

1. `Trening` — dzisiejsza sesja, szybki start oraz ostatnie wykonane treningi.
2. `Plany` — aktywny plan, pozostałe plany i tworzenie planu.
3. `Ćwiczenia` — katalog, wyszukiwanie oraz tworzenie własnego ćwiczenia.

Formularze tworzenia planu i ćwiczenia nie będą osobnymi stałymi zakładkami. Otworzą się jako dedykowane widoki wywoływane przyciskiem kontekstowym. Dzięki temu podstawowa nawigacja pozostaje krótka.

## Ekran „Trening”

Górna część pokazuje dzisiejszą sesję:

- nazwę dnia i aktywnego planu;
- liczbę ćwiczeń;
- wybór wariantu: `Pełny`, `Skrócony`, `Minimum`;
- główną akcję `Rozpocznij trening`;
- akcje drugorzędne `Wpisz wykonany trening` i `Dodaj z AI`.

Wariant skrócony wykorzystuje istniejącą logikę skracania treningu. Wybrany wariant jest widoczny jako wyraźny stan, nie jako dodatkowa zagnieżdżona karta.

Niżej znajduje się prosta lista ostatnich treningów. Kliknięcie otwiera ich podsumowanie bez mieszania historii z edycją planu.

## Ekran „Plany”

Nagłówek aktywnego planu zawiera:

- nazwę;
- status;
- liczbę dni;
- cel i poziom;
- menu działań;
- akcje `Nowy plan` i `Ułóż z AI`.

Dni planu są pokazane jako poziomy selektor. Na telefonie przewija się w poziomie, a na większym ekranie wykorzystuje pełną szerokość.

Wybrany dzień rozwija pionową listę ćwiczeń. Każdy wiersz zawiera:

- kolejność;
- nazwę oraz grupę mięśniową;
- liczbę serii i zakres powtórzeń;
- przerwę;
- miniaturę zdjęcia lub GIF-u;
- oznaczenie superserii;
- przejście do szczegółów ćwiczenia.

Nie stosujemy układu wielu pionowych kolumn obok siebie. Lista ćwiczeń zawsze znajduje się pod wybranym dniem.

## Przygotowanie treningu

`/workout/new` staje się zwartym ekranem przygotowania sesji, wizualnie należącym do modułu `Trening`.

Na początku użytkownik wybiera:

- `Dodaj z AI`;
- `Dodaj ręcznie`;
- opcjonalnie wariant dzisiejszego planu.

Tryb ręczny zachowuje wyszukiwanie, kolejność, serie, zakres powtórzeń, RIR, przerwę, interwał i łączenie w superserie. Układ zostanie uproszczony: mniej dużych obramowanych bloków, więcej czytelnych wierszy i jedna przyklejona akcja końcowa.

## Trening opisany AI

Prompt oraz obsługa odpowiedzi są jednoznacznie treningowe. Tekst o produktach lub porcjach nie może pojawić się w tym procesie.

AI przekształca opis na szkic zawierający:

- nazwę treningu;
- datę i opcjonalny czas trwania;
- bloki cardio z czasem, dystansem lub tempem, jeśli występują w opisie;
- ćwiczenia siłowe;
- każdą serię z ciężarem, powtórzeniami i opcjonalnym RIR;
- notatkę do ćwiczenia, jeśli część opisu nie daje się bezpiecznie zamienić na liczby.

Po analizie aplikacja pokazuje podgląd. Wszystkie wartości można poprawić, ćwiczenie można podmienić, a nierozpoznane elementy są wskazane przy właściwym wierszu.

Końcowe akcje:

- `Zapisz jako wykonany` tworzy zakończoną sesję z podaną datą i seriami, bez uruchamiania widoku live;
- `Rozpocznij trening` tworzy aktywną sesję i przechodzi do `/workout/{id}`;
- żadna akcja nie wykonuje się bez jawnego zatwierdzenia.

Jeżeli w międzyczasie istnieje aktywna sesja, rozpoczęcie nowej przekierowuje do niej. Zapis historyczny pozostaje możliwy, o ile nie narusza walidacji daty.

## Aktywna sesja

Widok `/workout/{id}` zostanie przebudowany wokół jednego aktualnego ćwiczenia:

- kompaktowy górny pasek z powrotem, postępem i menu;
- szerokie zdjęcie, GIF lub wideo;
- nazwa ćwiczenia i akcja przejścia do szczegółów;
- przerwa oraz interwał;
- przycisk `Zamień ćwiczenie`;
- przycisk `Połącz w superserię`;
- tabela serii: numer, kg, powtórzenia, RIR i stan ukończenia;
- czytelny aktywny wiersz;
- pasek superserii z kolejnością i postępem;
- przyklejona główna akcja zależna od etapu sesji.

Zachowane zostają istniejące funkcje: timery, podpowiedzi z historii, presety AI, superserie, dodawanie ćwiczenia, notatki, zakończenie i porzucenie sesji.

## Tworzenie superserii podczas sesji

Przy aktualnym ćwiczeniu, w tym samym obszarze działań co zamiana, dostępna jest akcja `Połącz w superserię`.

Po jej wybraniu użytkownik:

1. wskazuje co najmniej jedno inne ćwiczenie z bieżącej sesji lub katalogu;
2. ustala kolejność ćwiczeń;
3. wybiera liczbę rund;
4. ustawia czas odpoczynku po pełnej rundzie;
5. zatwierdza nową superserię.

Ćwiczenie należące już do innej superserii wymaga jawnego potwierdzenia przeniesienia. Nie można utworzyć superserii z jednym ćwiczeniem ani dodać tego samego ćwiczenia dwa razy.

Podczas wykonywania superserii:

- ekran prowadzi kolejno przez wszystkie ćwiczenia danej rundy;
- dla każdego podejścia użytkownik wpisuje własny ciężar, liczbę powtórzeń i opcjonalny RIR;
- zapis serii przechodzi do kolejnego ćwiczenia bez uruchamiania pełnej przerwy;
- po zapisaniu ostatniego ćwiczenia rundy uruchamia się ustawiony czas odpoczynku;
- po odpoczynku ekran wraca do pierwszego ćwiczenia następnej rundy;
- pasek superserii stale pokazuje bieżące ćwiczenie, numer rundy i stan zapisanych serii;
- po ukończeniu ostatniej rundy sesja przechodzi do kolejnego ćwiczenia spoza superserii.

Wprowadzone ciężary i powtórzenia są zapisywane niezależnie dla każdego ćwiczenia oraz każdej rundy. Cofnięcie do wcześniejszego elementu nie usuwa już zapisanych wartości.

## Zamiana ćwiczenia

Zamiana jest osobnym pełnym widokiem lub pełnoekranowym panelem na telefonie, a nie małą sekcją w środku tabeli.

Widok zawiera:

- kontekst aktualnego ćwiczenia i wykonanych serii;
- wyszukiwarkę;
- filtry grupy mięśniowej, sprzętu i podobieństwa;
- listę wyników z miniaturą, nazwą oraz metadanymi;
- jawnie zaznaczoną pozycję;
- stałe akcje `Anuluj` i `Potwierdź zamianę`.

Wykonane serie pozostają przy pierwotnym ćwiczeniu, a pozostałe przejmuje zamiennik zgodnie z istniejącą logiką domenową.

## Szczegóły ćwiczenia

Widok zawiera:

- media ćwiczenia;
- nazwę, główne mięśnie i sprzęt;
- zakładki `Historia`, `Wykres`, `Technika`;
- sesje historyczne w tabelach seria / kg × powtórzenia / 1RM;
- przejście do instrukcji technicznej.

Media respektują ustawienie ograniczenia ruchu. Na telefonie tabela nie może wymagać poziomego przewijania.

## Jedzenie

Zmiana jest celowo mała. Wiersz zapisanego posiłku zachowuje obecne funkcje i akcje, ale:

- klikalny blok nazwy i opisu jest wyrównany do lewej;
- tekst nie jest centrowany przez dziedziczone style przycisku;
- pełny obszar tekstowy pozostaje klikalny i dostępny z klawiatury.

## Kierunek wizualny

- Zachowujemy obecny jasny motyw FormaAI, ciemny tekst i niebieski kolor działania.
- Używamy istniejących krojów pisma; nagłówki pozostają zwarte i charakterystyczne.
- Referencje wyznaczają hierarchię, proporcje, kolejność informacji i ergonomię, nie branding.
- Ograniczamy zagnieżdżone karty, zbędne cienie i nadmiar pigułek.
- Minimalny cel dotykowy to 44 × 44 px.
- Układ jest projektowany najpierw dla telefonu, a następnie rozszerzany na desktop.

## Dane i API

Istniejące kontrakty sesji i planów zostaną wykorzystane, gdzie wystarczają. Nowy zapis treningu wykonanego przez AI wymaga serwerowej operacji, która:

1. przyjmuje zatwierdzony szkic;
2. waliduje datę, ćwiczenia i serie;
3. tworzy zakończoną sesję;
4. zapisuje serie oraz notatki;
5. zwraca podsumowanie zapisanej sesji.

Operacja będzie idempotentna względem identyfikatora szkicu AI, aby ponowne kliknięcie nie tworzyło duplikatu.

## Obsługa błędów

- Błędy AI są opisane językiem treningowym.
- Nierozpoznane ćwiczenie nie blokuje całego podglądu; użytkownik może je podmienić.
- Nieprawidłowa seria jest oznaczona przy właściwym wierszu.
- Niepowodzenie zapisu nie usuwa przygotowanego szkicu.
- Konflikt aktywnej sesji przekierowuje do istniejącego treningu tylko przy akcji `Rozpocznij trening`.

## Testy i kryteria akceptacji

Testy aplikacyjne i integracyjne obejmą:

- mapowanie opisu AI na edytowalny szkic treningu;
- zapis szkicu jako zakończonej sesji;
- idempotencję zapisu;
- brak automatycznego zapisu przed potwierdzeniem;
- rozpoczęcie szkicu jako aktywnej sesji;
- obsługę cardio i serii siłowych;
- zachowanie skracania treningu;
- tworzenie i edycję superserii w aktywnej sesji;
- prowadzenie po kolejnych ćwiczeniach superserii;
- uruchamianie przerwy dopiero po ostatnim ćwiczeniu rundy;
- niezależny zapis ciężaru, powtórzeń i RIR dla każdego elementu rundy;
- routing między listą planów, ćwiczeniem, sesją i zamianą.

Kryteria wizualne:

- globalna nawigacja aplikacji pozostaje niezmieniona;
- wewnętrzny moduł ma trzy podstawowe zakładki;
- plany są czytelną listą pod wyborem dnia;
- aktywna sesja odpowiada hierarchii z referencji;
- użytkownik może zbudować superserię w trakcie sesji i jest prowadzony przez jej rundy;
- AI zawsze pokazuje podgląd przed zapisem;
- opis posiłku jest wyrównany do lewej;
- widoki działają przy szerokości telefonu i desktopu;
- pełny `dotnet build FormaAI.sln` i `dotnet test FormaAI.sln --no-build` przechodzą.
