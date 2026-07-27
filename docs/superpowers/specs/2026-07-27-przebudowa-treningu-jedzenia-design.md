# Przebudowa treningu i rozszerzenie dziennika jedzenia

## Cel

Przebudować moduł treningowy FormaAI wokół krótkiego, mobilnego przebiegu sesji oraz rozszerzyć dziennik jedzenia o prostą edycję i kopiowanie posiłków między dniami. Zmiany zachowują obecną kolorystykę, istniejące dane użytkowników i działające funkcje.

## Zakres

Projekt obejmuje:

- nowy układ planów treningowych z trzema widocznymi zakładkami dni;
- pełnoekranowy przebieg aktywnej sesji z mediami ćwiczeń;
- wymianę ćwiczeń podczas sesji;
- ustawienia przerw, interwałów i superserii;
- rejestrowanie wykonanego treningu na podstawie opisu dla AI;
- uproszczony dziennik jedzenia;
- edycję zapisanego posiłku przez kliknięcie jego wiersza;
- kopiowanie posiłków między datami i sekcjami;
- zachowanie bezpośredniego usuwania posiłku;
- dalszą obsługę edycji kaloryczności i wielu zdjęć.

Poza zakresem pozostają zmiana identyfikacji wizualnej FormaAI, automatyczny zapis propozycji AI bez potwierdzenia oraz usuwanie źródłowego posiłku podczas kopiowania.

## Moduł treningowy

### Kierunek wizualny i referencje

Przesłane przez użytkownika ekrany są wiążącą referencją struktury treningu, a nie wyłącznie inspiracją. Implementacja ma zachować ich:

- obrazowy, pełnoszeroki nagłówek aktualnego ćwiczenia;
- mocną hierarchię nazwy ćwiczenia, 1RM i timerów;
- prostą tabelę serii bez zagnieżdżonych kart;
- dolną, mobilną akcję rozpoczęcia lub przejścia dalej;
- pełnoekranową listę zamienników z miniaturami;
- szczegóły ćwiczenia podzielone na historię, wykres i instrukcję;
- duże cele dotykowe i oszczędną liczbę kontrolek.

Kolory, typografia i stany interakcji zostaną przełożone na istniejący język FormaAI. Nie będą kopiowane ciemna kolorystyka ani identyfikacja wizualna aplikacji referencyjnej.

Przed implementacją interfejsu zostanie użyty workflow `image-to-code`: osobne, czytelne obrazy referencyjne dla listy planu, aktywnej sesji, wymiany ćwiczenia i szczegółów ćwiczenia zostaną wygenerowane, przeanalizowane i zapisane jako źródło decyzji projektowych. Następnie `frontend-design` posłuży do zbudowania zwartego zestawu tokenów, typografii, odstępów i zachowań responsywnych zgodnych z FormaAI. Implementacja będzie porównywana ze screenami, aby ograniczyć odejście od referencji.

Warstwa jakości wizualnej korzysta również z następujących zasad:

- `impeccable`: tryb Operate, audit istniejącego interfejsu, zachowanie product truth i systemu Forma Signal, kontrola responsywności, dostępności, stanów oraz końcowy polish;
- `emil-design-eng`: animacje wyłącznie jako informacja zwrotna lub wyjaśnienie zmiany stanu, czas 100-250 ms dla codziennych interakcji, mocne ease-out, reakcja na naciśnięcie i pełne wsparcie `prefers-reduced-motion`;
- `design-taste-frontend`: audyt antygenerycznego wyglądu, spójność jednego akcentu, promieni, typografii i stanów. Skill nie steruje architekturą dashboardu ani workflow, ponieważ sam wyłącza z zakresu gęste aplikacje i wieloetapowe formularze;
- `gpt-taste`: wyłącznie kontrola przed wizualnym dryfem i powtarzalnymi wzorcami. Wymagania AIDA, landing page i GSAP nie mają zastosowania do narzędzia treningowego używanego podczas ćwiczeń.

Design Read: responsywna aplikacja operacyjna dla osoby używającej telefonu między seriami. Język ma być bezpośredni, obrazowy i skupiony na następnym działaniu. Forma Signal pozostaje systemem nadrzędnym, a przesłane ekrany definiują kompozycję treningu.

Parametry robocze dla kontroli wyglądu:

- `DESIGN_VARIANCE: 5` - rozpoznawalny charakter bez utraty przewidywalności obsługi;
- `MOTION_INTENSITY: 3` - szybka informacja zwrotna bez dekoracyjnego ruchu;
- `VISUAL_DENSITY: 6` - wystarczająco zwarto między seriami, ale z celami dotykowymi minimum 44 px.

### Plany treningowe

Karta planu zachowuje nagłówek, status, nazwę, cel i akcje planu. Dni planu są prezentowane jako poziomy pasek zakładek:

- na typowej szerokości ekranu widoczne są trzy zakładki;
- kolejne dni są dostępne przez poziome przesunięcie;
- wybrana zakładka ma wyraźny aktywny stan;
- ćwiczenia wybranego dnia tworzą jedną pełnoszeroką, pionową listę pod zakładkami;
- zmiana zakładki podmienia listę bez przeładowania strony;
- kliknięcie ćwiczenia otwiera jego szczegóły;
- edycja dnia pozostaje dostępna z poziomu wybranego dnia.

Układ odpowiada zaakceptowanemu wariantowi B: zakładki u góry, lista poniżej.

### Szczegóły ćwiczenia

Ekran ćwiczenia zawiera:

- GIF, animację albo obraz ćwiczenia u góry;
- nazwę, grupę mięśniową i sprzęt;
- zakładki Historia, Wykres i Instrukcja;
- historię serii i szacowanego 1RM;
- bezpieczny stan zastępczy, gdy ćwiczenie nie ma przypisanego medium.

Istniejące media ćwiczeń są wykorzystywane ponownie. Brak medium nie blokuje planu ani sesji.

### Aktywna sesja

Sesja skupia się na jednym aktualnym ćwiczeniu:

1. U góry znajduje się GIF lub obraz oraz wskaźnik pozycji ćwiczenia w sesji.
2. Pod medium widoczne są nazwa, estymowany 1RM, przerwa i interwał.
3. Tabela serii pozwala wpisać ciężar, powtórzenia i oznaczyć serię jako wykonaną.
4. Użytkownik może dodać serię, przejść dalej, wymienić ćwiczenie albo zakończyć sesję.
5. Dolna akcja jest przyklejona na telefonie, aby pozostała dostępna podczas przewijania.

Wymiana ćwiczenia zachowuje już wykonane serie i pokazuje ćwiczenia o zbliżonej grupie mięśniowej lub wzorcu ruchu. Użytkownik zawsze potwierdza wybór zamiennika.

### Przerwy, interwały i superserie

Planowane ćwiczenie oraz jego kopia w sesji otrzymują:

- opcjonalny identyfikator grupy superserii;
- kolejność wewnątrz grupy;
- opcjonalny interwał między ćwiczeniami grupy;
- przerwę po ukończeniu całej rundy.

Superseria może łączyć co najmniej dwa ćwiczenia. Po ukończeniu serii jednego ćwiczenia sesja przechodzi do następnego ćwiczenia grupy. Między nimi uruchamia się opcjonalny krótki interwał. Właściwy licznik przerwy uruchamia się dopiero po ostatnim ćwiczeniu rundy. Po przerwie użytkownik wraca do pierwszego nieukończonego ćwiczenia kolejnej rundy.

Ćwiczenia bez grupy działają jak dotychczas: przerwa uruchamia się po każdej ukończonej serii.

### Kreator planu

Kreator pozostaje workflow, ale zostaje dopasowany do nowego modelu:

1. nazwa i cel;
2. dzień treningowy;
3. ćwiczenia i kolejność;
4. serie, zakres powtórzeń, RIR, interwały, przerwy i grupowanie w superserie;
5. podsumowanie przed zapisem.

Łączenie ćwiczeń w superserię odbywa się na liście dnia. Użytkownik zaznacza kilka ćwiczeń, wybiera „Połącz w superserię”, ustawia interwał i przerwę po rundzie, a podsumowanie pokazuje grupę jako jeden blok.

### Trening opisany AI

Na ekranie rozpoczęcia lub dodawania treningu użytkownik może wkleić swobodny opis, na przykład wykonane ćwiczenia, serie, ciężary i powtórzenia.

Przepływ:

1. interfejs wysyła opis do serwera;
2. serwer przekazuje AI katalog ćwiczeń użytkownika oraz jednoznaczny format wyniku;
3. AI zwraca szkic bez zapisu w bazie;
4. ekran podglądu pokazuje rozpoznane ćwiczenia i serie;
5. niepewne dopasowania są oznaczone i wymagają wskazania ćwiczenia;
6. użytkownik może poprawić wszystkie wartości;
7. dopiero przycisk „Zapisz trening na dziś” tworzy ukończoną sesję.

Ponowne wysłanie albo błąd sieci nie może utworzyć duplikatu. Zapis wykorzystuje identyfikator szkicu lub klucz idempotencji.

## Moduł jedzenia

### Dziennik dnia

Dziennik zachowuje sekcje posiłków skonfigurowane w profilu. Każda sekcja pokazuje nazwę, liczbę wpisów, kalorie i makro. Po rozwinięciu wyświetla prostą listę zapisanych posiłków.

Wiersz posiłku jest głównym celem kliknięcia:

- kliknięcie wiersza otwiera edycję;
- widoczna akcja usuwania pozostaje przy wpisie;
- kopiowanie znajduje się w menu z trzema kropkami;
- akcje nie dublują się w kilku miejscach.

### Edycja posiłku

Edycja pozwala zmienić:

- nazwę;
- sekcję dnia;
- składniki i porcje;
- kalorie oraz makro produktów;
- docelową kaloryczność całego posiłku;
- zdjęcia z galerii telefonu, również więcej niż jedno.

Zmiana docelowej kaloryczności proporcjonalnie przelicza ilości składników i wynikowe makro. Użytkownik widzi podgląd wartości przed zapisem.

### Kopiowanie posiłków

Menu sekcji lub posiłku udostępnia:

- „Kopiuj do” dla wskazanego wpisu;
- „Kopiuj z” dla wybrania wpisu z innego dnia.

Obie akcje prowadzą do tego samego dwustopniowego workflow:

1. wybór daty i posiłku źródłowego;
2. wybór daty oraz sekcji docelowej.

Podsumowanie przed wykonaniem operacji pokazuje źródło, cel, nazwę, kalorie i liczbę składników. Kopiowanie tworzy nowy posiłek i nie modyfikuje oryginału.

Serwer kopiuje w jednej transakcji:

- nazwę i składniki;
- ilości, jednostki i makro;
- notatki oraz rozpoznane dane;
- odwołania do powiązanych, niezmiennych zdjęć.

Jeżeli sekcja docelowa została usunięta lub zmieniona, interfejs prosi o wybór aktualnej sekcji. Ponowienie tego samego zatwierdzenia jest idempotentne.

### Dodawanie przez AI i zdjęcia

Ręczne dodawanie, rozpoznawanie tekstu przez AI i przesyłanie wielu zdjęć korzystają ze wspólnego ekranu podglądu. AI nigdy nie zapisuje wpisu automatycznie. Użytkownik może poprawić nazwę, składniki, ilości, kalorie i makro przed dodaniem do dziennika.

## Architektura i dane

### Domena i baza

Model planowanego ćwiczenia i ćwiczenia sesji zostanie rozszerzony o dane superserii oraz interwału. Migracja przypisze istniejącym rekordom brak grupy i zachowa dotychczasowe wartości przerw.

Szkic treningu AI jest obiektem tymczasowym. Nie jest liczony jako trening ani progres, dopóki użytkownik go nie zatwierdzi.

Kopiowanie posiłku tworzy nowe encje posiłku i pozycji. Pliki zdjęć pozostają niezmienne, dlatego skopiowany wpis może bezpiecznie wskazywać te same zasoby zamiast duplikować ich zawartość.

### API

Kontrakty treningowe zostaną rozszerzone o pola superserii i interwałów. API otrzyma operacje:

- utworzenia szkicu wykonanego treningu z opisu;
- zatwierdzenia poprawionego szkicu jako ukończonej sesji;
- aktualizacji ustawień czasowych i grupowania ćwiczeń;
- kopiowania posiłku do wskazanej daty i sekcji.

Operacje mutujące sprawdzają właściciela danych, zakres wartości i klucz idempotencji. Szkic AI nie wywołuje operacji zapisu treningu.

### Frontend

Widoki pozostają w Blazor WebAssembly i MudBlazor. Nowe fragmenty zostaną wydzielone w małe komponenty odpowiedzialne za:

- zakładki dni i listę ćwiczeń;
- hero ćwiczenia;
- edytor serii;
- licznik przerwy i interwału;
- grupę superserii;
- podgląd szkicu AI;
- selektor źródła i celu kopiowania posiłku.

Komponenty korzystają z istniejących klientów API i wzorców FormaAI. Stan formularza jest zachowywany podczas odwracalnych błędów sieci.

Interfejs treningowy nie może sprowadzić się do domyślnych kart i formularzy MudBlazor. Komponenty biblioteki pozostają podstawą dostępności i zachowania, ale warstwa układu, typografii, odstępów, tabel serii, timerów i dolnych akcji ma odwzorowywać zaakceptowane referencje.

## Obsługa błędów

- Brak medium ćwiczenia pokazuje neutralny stan zastępczy.
- Nieznane ćwiczenie w odpowiedzi AI wymaga ręcznego dopasowania.
- Nieprawidłowe serie, ciężary albo powtórzenia blokują zatwierdzenie i wskazują konkretne pole.
- Nieudane kopiowanie nie usuwa ani nie zmienia źródła.
- Przerwanie połączenia podczas zatwierdzania pozwala bezpiecznie ponowić operację.
- Konflikt aktywnej sesji jest przedstawiony jako możliwość wznowienia istniejącego treningu.

## Testy

Testy domenowe i aplikacyjne obejmą:

- tworzenie oraz walidację grup superserii;
- kolejność ćwiczeń i przechodzenie między rundami;
- interwał wewnątrz grupy i przerwę po rundzie;
- zachowanie już wykonanych serii po wymianie ćwiczenia;
- migrację istniejących planów bez grup superserii;
- utworzenie szkicu AI bez zapisu sesji;
- walidację i zatwierdzenie szkicu;
- idempotencję zatwierdzenia;
- kopiowanie posiłku między datami i sekcjami;
- brak zmian w posiłku źródłowym;
- zachowanie składników, makro i zdjęć;
- przeskalowanie posiłku do docelowej kaloryczności.

Testy integracyjne sprawdzą autoryzację i transakcje API. Testy komponentów lub możliwe do wydzielenia testy logiki widoku obejmą przełączanie zakładek, edycję wiersza posiłku, menu kopiowania i stany timerów.

Przed publikacją zostaną uruchomione:

```powershell
dotnet build FormaAI.sln
dotnet test FormaAI.sln --no-build
```

## Podział wdrożenia i publikacja

Praca zostanie wykonana na osobnym branchu i podzielona na polskie commity:

1. model superserii i kontrakty treningowe;
2. nowy interfejs planów i aktywnej sesji;
3. szkic wykonanego treningu z AI;
4. kopiowanie posiłków i rozszerzenie API jedzenia;
5. przebudowany interfejs dziennika i edycji posiłku;
6. testy, migracja i poprawki publikacyjne.

Przed commitem interfejsu treningu powstaną i zostaną przeanalizowane osobne referencje wizualne. Po implementacji widoki zostaną sprawdzone na szerokości telefonu oraz komputera, a różnice w hierarchii, odstępach i położeniu głównych akcji poprawione przed scaleniem.

Końcowa bramka jakości UI obejmuje:

1. porównanie renderów z zatwierdzonymi referencjami;
2. przegląd ruchu zgodny z `emil-design-eng`, zapisany w tabeli Before/After/Why;
3. mechaniczny detector `impeccable` uruchomiony raz po zakończeniu zmian UI;
4. osobny finish review `impeccable`;
5. kontrolę stanów loading, empty, error, focus, touch i reduced motion;
6. ponowny render telefonu i desktopu po poprawkach.

Po przejściu pełnego builda i testów branch zostanie scalony do `main`, a nowa wersja aplikacji opublikowana. Użytkownik otrzyma adres działającej strony.

## Kryteria akceptacji

- Plan pokazuje trzy zakładki dni i pełnoszeroką listę wybranego dnia.
- Układ aktywnej sesji, wymiany i szczegółów ćwiczenia pozostaje rozpoznawalnie zgodny z przesłanymi screenami, przy zachowaniu kolorystyki FormaAI.
- Każde ćwiczenie prowadzi do szczegółów z medium i historią.
- Sesja umożliwia wpisywanie serii, wymianę ćwiczenia oraz ustawienie przerwy i interwału.
- Superseria wykonuje ćwiczenia kolejno i uruchamia przerwę po całej rundzie.
- Opis treningu dla AI tworzy wyłącznie edytowalny szkic.
- Trening trafia do historii dopiero po zatwierdzeniu przez użytkownika.
- Kliknięcie posiłku otwiera edycję.
- Usuwanie pozostaje bezpośrednio przy wpisie, a kopiowanie znajduje się w menu „…”.
- Posiłek można skopiować z wybranego dnia i sekcji do wybranego dnia i sekcji bez zmiany oryginału.
- Posiłek można edytować kalorycznie i dodać do niego wiele zdjęć.
- Istniejące dane użytkowników pozostają dostępne po migracji.
