# FormaAI — opis produktu i brief do redesignu frontendu

## 1. Przeznaczenie dokumentu

Ten dokument opisuje aktualny frontend FormaAI i może być użyty jako:

- kontekst dla projektanta UI/UX;
- materiał wejściowy dla generatora interfejsów;
- brief dla modelu AI tworzącego makiety, design system lub kod;
- lista funkcji, których nie wolno zgubić podczas redesignu.

Redesign ma poprawić wygląd, hierarchię, spójność i wygodę obsługi. Nie powinien zmieniać logiki biznesowej, kontraktów API ani modelu danych.

## 2. Czym jest FormaAI

FormaAI to responsywna, instalowalna aplikacja PWA do prowadzenia diety, treningów i postępów sylwetkowych. Łączy dane z całego dnia w jeden obraz i wskazuje użytkownikowi kolejny sensowny krok.

Głównym użytkownikiem jest osoba korzystająca z aplikacji wielokrotnie w ciągu dnia, przede wszystkim na telefonie:

- podczas dodawania posiłku;
- przed treningiem i między seriami;
- podczas sprawdzania realizacji kalorii i makroskładników;
- przy zapisywaniu masy, obwodów i zdjęć;
- podczas tygodniowego podsumowania.

Najważniejsze założenia produktu:

- telefon jest platformą główną, ale interfejs musi dobrze działać także na tablecie i komputerze;
- najczęstsze czynności mają być krótkie i możliwe do wykonania jedną ręką;
- jedzenie i trening można zapisywać ręcznie albo z pomocą AI;
- AI przygotowuje edytowalny szkic, ale niczego nie zapisuje bez zatwierdzenia użytkownika;
- dane liczbowe muszą być czytelne bez analizowania skomplikowanych wykresów;
- aplikacja ma motywować, ale nie może przypominać infantylnej gry ani kasyna.

## 3. Technologia frontendu

- .NET 8;
- Blazor WebAssembly;
- MudBlazor;
- Razor Components;
- globalne arkusze `app.css` i `forma-signal.css`;
- Material Icons dostarczane przez MudBlazor;
- PWA z service workerem, manifestem, trybem offline i ikonami aplikacji;
- lokalny wybór jasnego, ciemnego lub systemowego motywu;
- komunikacja z ASP.NET Core API przez klientów HTTP.

Frontend znajduje się w `src/FormaAI.Web`.

Najważniejsze katalogi:

```text
src/FormaAI.Web/
├── Components/             współdzielone komponenty
├── Layout/MainLayout.razor główna rama i nawigacja
├── Pages/                  wszystkie ekrany aplikacji
├── Services/               klienci API
└── wwwroot/
    ├── css/                style globalne
    ├── js/                 ustawienia przeglądarki
    └── index.html          fonty, PWA i uruchomienie Blazora
```

## 4. Obecna architektura interfejsu

### Główna rama

Każdy główny ekran działa we wspólnym layoucie:

- górny pasek z logotypem tekstowym `FORMA/AI`;
- tagline `Ruch · paliwo · rytm`;
- skrót do Asystenta AI;
- centralna zawartość strony;
- dolna nawigacja z pięcioma pozycjami.

### Główna nawigacja

| Pozycja | Trasa | Rola |
|---|---|---|
| Dzisiaj | `/` | podsumowanie dnia i szybkie działania |
| Jedzenie | `/food` | dziennik jedzenia i realizacja celu |
| Progres | `/progress` | trendy, kalendarz i pomiary |
| Trening | `/training` | plany, ćwiczenia i start sesji |
| Profil | `/profile` | cele, ustawienia i konto |

Asystent jest dostępny z górnego paska pod `/assistant`.

Na telefonie dolna nawigacja jest najważniejszym stałym elementem sterującym. Projekt nie powinien wymagać menu hamburgerowego dla pięciu głównych modułów.

## 5. Mapa ekranów

| Ekran | Trasa | Główne zadanie |
|---|---|---|
| Dzisiaj | `/` | ocena dnia, szybkie dodanie posiłku, rozpoczęcie treningu |
| Jedzenie | `/food` | dziennik posiłków, makro, status kompletności |
| Dodaj posiłek | `/food/add` | ręczne dodawanie, zdjęcie AI lub opis AI |
| Spiżarnia | `/pantry` | zapasy, przepisy i lista zakupów |
| Trening | `/training` | plan dnia, plany treningowe i biblioteka ćwiczeń |
| Nowy trening | `/workout/new` | szybka sesja ręczna albo wygenerowana przez AI |
| Aktywny trening | `/workout/{id}` | wykonywanie ćwiczeń, serii, przerw i superserii |
| Szczegóły ćwiczenia | `/training/exercises/{id}` | historia, wykres, technika i zaangażowanie mięśni |
| Progres | `/progress` | wspólny obraz diety, treningów i ciała |
| Pomiary | `/progress/measurements` | masa i obwody |
| Zdjęcia progresu | `/progress/photos` | prywatne zdjęcia porównawcze |
| Zamknięcie tygodnia | `/progress/weekly` | czterostopniowy check-in |
| Osiągnięcia | `/progress/achievements` | kamienie milowe oparte na danych |
| Profil | `/profile` | dane użytkownika, cele i ustawienia |
| Ustawienia profilu | `/profile/settings/{section}` | ustawienia konkretnej kategorii |
| Asystent | `/assistant` | rozmowa i zatwierdzanie szkiców AI |
| Administrator | `/admin` | konfiguracja dostawcy i modelu AI |
| Prywatność | `/privacy` | opis zasad danych i integracji |

## 6. Jak działa każdy moduł

### 6.1. Dzisiaj

Pulpit odpowiada na trzy pytania:

1. Co mam zrobić teraz?
2. Jak wygląda mój dzisiejszy bilans?
3. Czy mam dzisiaj trening?

Zawartość:

- komunikat i rekomendacja dnia;
- tygodniowe momentum oraz liczba wykonanych treningów;
- skróty do dodania posiłku i treningu;
- dzisiejszy plan treningowy;
- przycisk rozpoczęcia albo wznowienia sesji;
- dzisiejsze sekcje posiłków;
- kalorie i makroskładniki;
- kopiowanie posiłku z innego dnia;
- skrót do Asystenta AI.

Dla niezalogowanej osoby ekran zmienia się w prosty onboarding z logowaniem i rejestracją.

### 6.2. Jedzenie

Moduł prezentuje dziennik wybranego dnia.

Funkcje:

- zmiana dnia w obrębie tygodnia;
- sekcje posiłków zdefiniowane przez użytkownika;
- rozwijanie i zwijanie zawartości sekcji;
- edycja istniejącego posiłku;
- dodawanie produktu do konkretnej sekcji;
- kopiowanie posiłku lub dnia;
- kalorie, białko, tłuszcze i węglowodany;
- porównanie spożycia z celem;
- ręczne oznaczenie dnia jako kompletnego, częściowego lub bez danych;
- lista ostatnich i powtarzanych posiłków;
- przejście do spiżarni.

Dodawanie posiłku ma trzy tryby:

- wyszukanie produktów i ustawienie porcji;
- analiza jednego lub kilku zdjęć;
- opis tekstowy analizowany przez AI.

Szkic AI można poprawić, skalować do wybranej liczby kalorii, uzupełnić o składniki i dopiero potem zatwierdzić.

### 6.3. Spiżarnia

Moduł łączy cztery obszary:

- produkty posiadane w domu;
- własne przepisy;
- brakujące składniki;
- aktywną listę zakupów.

Użytkownik może ustawiać ilość produktu, usuwać go z zapasów, budować przepis, sprawdzać brakujące składniki, przenosić je na listę zakupów i oznaczać zakupy jako wykonane.

### 6.4. Trening

Główny ekran treningu obsługuje:

- dzisiejszą jednostkę;
- pełny trening albo skrócone warianty 30 i 15 minut;
- wznowienie aktywnej sesji;
- przełożenie albo świadome pominięcie treningu;
- listę planów;
- aktywowanie, edycję i duplikowanie planu;
- tworzenie planu w czterech krokach;
- dni treningowe, ćwiczenia, serie, zakresy powtórzeń, RIR i przerwy;
- superserie;
- bibliotekę ćwiczeń;
- tworzenie własnych ćwiczeń;
- przypisywanie partii mięśniowych i procentowego zaangażowania.

Nowy szybki trening można przygotować:

- opisem tekstowym dla AI;
- ręcznie, wybierając ćwiczenia i parametry.

### 6.5. Aktywny trening

To najbardziej operacyjny ekran aplikacji. Powinien być bardzo czytelny, odporny na przypadkowe kliknięcia i wygodny przy zmęczeniu.

Funkcje:

- zegar całej sesji;
- postęp sesji;
- nawigacja pomiędzy ćwiczeniami;
- podgląd planowanych serii;
- zapis ciężaru, powtórzeń i RIR;
- edycja zapisanych serii;
- timer przerwy i stoper interwału;
- dźwięk końca przerwy;
- ostatni wynik danego ćwiczenia;
- zamiana ćwiczenia;
- połączenie ćwiczeń w superserię;
- dodanie ćwiczenia w trakcie sesji;
- notatki;
- zakończenie lub porzucenie treningu;
- podsumowanie po zakończeniu;
- sugestie progresji ciężaru, które można zaakceptować, odrzucić lub pozostawić bez zmiany.

### 6.6. Szczegóły ćwiczenia

Ekran ma trzy zakładki:

- historia;
- wykres;
- technika.

Pokazuje wykonane serie, szacowane 1RM, zmianę wyniku, opis techniki, pracujące mięśnie i ustawienia ćwiczenia w planie. Własne ćwiczenie może otrzymać zdjęcie lub animację z informacją o autorze, licencji i źródle.

### 6.7. Progres

Moduł łączy dane żywieniowe, treningowe i sylwetkowe.

Zawartość:

- wybór zakresu czasu;
- ogólna ocena kierunku;
- realizacja diety i treningów;
- trend masy;
- objętość treningowa;
- kalorie w czasie;
- makroskładniki;
- obwody ciała;
- kalendarz, w którym jeden dzień pokazuje stan diety i treningu;
- szczegóły dnia;
- skróty do pomiarów, zdjęć, tygodniowego podsumowania i osiągnięć.

### 6.8. Pomiary, zdjęcia i tydzień

Pomiary:

- masa ciała;
- obwody;
- data pomiaru;
- edycja i usuwanie wpisów;
- wybór zakresu prezentacji.

Zdjęcia progresu:

- pozycja przodem, bokiem lub tyłem;
- maksymalnie pięć zdjęć na raz;
- wybór dwóch zdjęć do porównania;
- usuwanie zdjęć;
- prywatny charakter materiałów.

Tygodniowe podsumowanie:

1. ocena jakości zapisanych danych;
2. fakty z tygodnia;
3. energia, sen, głód, regeneracja i stres;
4. jedna decyzja na następny tydzień.

Osiągnięcia wynikają z prawdziwych działań i nie są przyznawane za samo otwieranie aplikacji.

### 6.9. Profil

Profil zawiera dziewięciostopniowy kreator celu:

- redukcja, utrzymanie albo budowanie masy;
- płeć;
- data urodzenia;
- wzrost;
- masa aktualna;
- masa docelowa;
- aktywność codzienna;
- aktywność treningowa;
- wynik i cele żywieniowe.

Dodatkowe ustawienia:

- kalorie i makroskładniki;
- nazwy, kolejność i godziny posiłków;
- przypomnienia;
- domyślne parametry treningu;
- początek tygodnia;
- jasny, ciemny lub systemowy wygląd;
- konto i usuwanie konta;
- panel administratora dla uprawnionego użytkownika.

### 6.10. Asystent AI

Asystent obsługuje m.in.:

- propozycję posiłku ze spiżarni;
- analizę dzisiejszego makro;
- szkic planu treningowego;
- zapis wykonanego treningu z opisu.

Każda propozycja zmieniająca dane jest prezentowana jako szkic. Użytkownik może ją poprawić, zatwierdzić albo odrzucić.

### 6.11. Administrator

Panel administratora pozwala:

- wybrać Gemini albo API zgodne z OpenAI;
- ustawić adres usługi i model;
- zapisać klucz po stronie serwera;
- sprawdzić zapisane połączenie.

Klucza API nie wolno wyświetlać w gotowym interfejsie ani przenosić do kodu klienta.

## 7. Najważniejsze przepływy

### Dodanie posiłku

```text
Dzisiaj/Jedzenie
→ wybór sekcji i dnia
→ wyszukanie produktu LUB zdjęcie AI LUB opis AI
→ ustawienie porcji
→ kontrola kalorii i makro
→ zatwierdzenie
→ aktualizacja bilansu dnia
```

### Rozpoczęcie treningu

```text
Dzisiaj/Trening
→ wybór planu lub szybki trening
→ pełny albo skrócony wariant
→ aktywna sesja
→ serie i przerwy
→ zakończenie
→ podsumowanie i decyzje progresji
→ aktualizacja Progresu
```

### Zmiana celu żywieniowego

```text
Profil
→ kreator celu
→ dane ciała i aktywności
→ wyliczona propozycja
→ ręczna korekta
→ zapis celu
→ nowe wartości w Dzisiaj, Jedzeniu i Progresie
```

### Propozycja AI

```text
polecenie użytkownika
→ odpowiedź i edytowalny szkic
→ kontrola danych
→ zatwierdzenie lub odrzucenie
→ zapis dopiero po zatwierdzeniu
```

## 8. Aktualny kierunek wizualny

Obecny system nazywa się `Forma Signal`.

Charakter:

- nowoczesne narzędzie treningowe;
- redakcyjna hierarchia danych;
- jasne rozróżnienie działania, odżywiania, regeneracji i błędu;
- techniczny charakter budowany przez rytm, typografię i dane, nie przez neonowe efekty;
- mało dekoracyjnych cieni;
- zaokrąglenia kontrolowane zamiast wszechobecnych kapsułek;
- brak przypadkowych gradientów;
- mocne, krótkie nagłówki i monospaced dane.

## 9. Aktualne kolory

Arkusz `forma-signal.css` jest ładowany po `app.css`, dlatego jego główne tokeny są nadrzędne w kaskadzie.

### Motyw jasny

| Rola | Zmienna | Kolor |
|---|---|---|
| tło aplikacji | `--canvas` | `#F4F6F3` |
| główna powierzchnia | `--surface` | `#FFFFFF` |
| miękka powierzchnia | `--surface-soft` | `#EEF2EE` |
| główny tekst | `--ink` | `#17211C` |
| tekst drugorzędny | `--muted` | `#596760` |
| linia | `--line`, `--rule` | `#D7DDD8` |
| mocna linia | `--rule-strong` | `#ABB7AF` |
| główna akcja i AI | `--action` | `#3451D1` |
| hover akcji | `--action-hover` | `#283FAE` |
| miękkie tło akcji | `--action-soft` | `#E8EBFB` |
| jedzenie/energia | `--fuel` | `#B9562C` |
| miękkie tło jedzenia | `--fuel-soft` | `#F8E9E1` |
| wykonanie/regeneracja | `--recovery` | `#287454` |
| miękkie tło sukcesu | `--recovery-soft` | `#E2EFE8` |
| błąd/destrukcja | `--danger` | `#B93B35` |
| miękkie tło błędu | `--danger-soft` | `#F8E8E6` |
| focus | `--focus` | `#7185EB` |

### Motyw ciemny

| Rola | Kolor |
|---|---|
| tło aplikacji | `#0F1713` |
| główna powierzchnia | `#16221C` |
| miękka powierzchnia | `#1C2C24` |
| główny tekst | `#F1F5F2` |
| tekst drugorzędny | `#AFBEB5` |
| linia | `#2C3E34` |
| mocna linia | `#5F7468` |
| główna akcja | `#8CA0FF` |
| miękkie tło akcji | `#25335F` |
| jedzenie/energia | `#F0A076` |
| miękkie tło jedzenia | `#402A20` |
| wykonanie/regeneracja | `#71C69A` |
| miękkie tło sukcesu | `#1B3B2C` |
| błąd/destrukcja | `#FF8B84` |
| miękkie tło błędu | `#442523` |
| focus | `#A8B6FF` |

### Kolory sekcji posiłków

Sekcje posiłków mogą używać indywidualnych akcentów:

- pomarańczowy `#F27D3D`;
- zielony `#36A373`;
- niebieski `#3F6FE5`;
- bursztynowy `#E9A823`;
- fioletowy `#7959DF`.

Kolor nie może być jedynym sposobem przekazywania statusu.

## 10. Typografia

| Zastosowanie | Font |
|---|---|
| nagłówki i komunikaty motywacyjne | Barlow Semi Condensed 600–700 |
| tekst, formularze i interfejs | Onest 400–700 |
| kalorie, makro, czas, ciężar i etykiety danych | IBM Plex Mono 500–600 |

Obecne zasady:

- nagłówki są zwarte, mocne i lekko skondensowane;
- tekst podstawowy powinien mieć minimum 15 px;
- małe etykiety nie powinny schodzić poniżej 12 px;
- dane liczbowe powinny zachować stabilną szerokość i być łatwe do skanowania;
- typografia nie może utrudniać odczytania wartości podczas aktywnego treningu.

## 11. Geometria, powierzchnie i ruch

Aktualne tokeny:

- kontrolki: promień `10 px`;
- główne powierzchnie: promień około `15 px`;
- dialogi i większe panele: promień `20 px`;
- cień unoszącej się powierzchni: `0 12px 32px rgba(23, 33, 28, .09)`;
- szerokość wąskiej treści: `860 px`;
- szerokość szerokiej treści: `1180 px`;
- naciśnięcie: około `140 ms`;
- standardowa zmiana stanu: około `200 ms`;
- easing: szybkie fizyczne wyhamowanie.

Ruch powinien:

- potwierdzać wykonaną czynność;
- pokazywać zmianę stanu lub przejście do kolejnego kroku;
- pozostawać krótki;
- respektować `prefers-reduced-motion`;
- nie animować stale wykresów, kart ani dekoracji.

## 12. Responsywność i dostępność

W kodzie istnieją breakpointy m.in. w okolicach:

- 350–420 px dla bardzo małych telefonów;
- 520–640 px dla typowych telefonów;
- 768–900 px dla tabletów;
- 1024–1200 px dla desktopu.

Wymagania:

- podstawowy projekt należy przygotować dla szerokości około 390 px;
- obszary dotykowe powinny mieć minimum 44 × 44 px;
- dolna nawigacja musi uwzględniać bezpieczny obszar telefonu;
- aktywny trening musi działać bez precyzyjnego celowania;
- klawiatura ekranowa nie może zasłaniać głównego przycisku zapisu;
- dialogi na telefonie mogą zmieniać się w dolne arkusze albo pełne ekrany;
- focus klawiatury musi być wyraźny;
- interfejs powinien respektować tryb ograniczonego ruchu;
- jasny i ciemny motyw muszą zachowywać kontrast WCAG AA;
- loading, pusty stan, błąd, brak połączenia i zapis muszą mieć czytelne warianty.

## 13. Obecne komponenty i wzorce

Współdzielone komponenty obejmują:

- podsumowanie kalorii i makroskładników;
- ikonę sekcji posiłku;
- dialog kopiowania posiłku;
- szkielet ładowania;
- ramkę zdjęcia lub animacji ćwiczenia.

Powtarzające się wzorce ekranów:

- nagłówek modułu z kickerem, tytułem, opisem i akcją;
- karty danych;
- wskaźniki kołowe kalorii i makro;
- paski postępu;
- wykresy i kalendarze budowane w CSS;
- kreatory krokowe;
- listy z akcją po prawej stronie;
- dolne arkusze i dialogi;
- stany puste z jednym głównym działaniem;
- formularze MudBlazor;
- snackbar po zapisie albo błędzie.

## 14. Ryzyka obecnego frontendu

To obserwacje wynikające z kodu, a nie z pełnego badania z użytkownikami.

### Dwa równoległe źródła stylu

`app.css` ma około 2739 linii, a `forma-signal.css` około 1391 linii. Oba definiują część tych samych tokenów, motywów i breakpointów. Ostateczny wygląd zależy od kolejności ładowania i szczegółowości selektorów.

Przy redesignie warto doprowadzić do:

- jednego źródła tokenów;
- jednego mechanizmu motywu;
- jasno wydzielonych stylów komponentów i ekranów;
- mniejszej liczby wyjątków w globalnym CSS.

### Duże komponenty stron

Największe pliki:

- aktywny trening: około 937 linii;
- jedzenie: około 756 linii;
- dodawanie posiłku: około 669 linii;
- trening i plany: około 656 linii;
- kreator nowego treningu: około 557 linii;
- profil: około 477 linii.

Zmiana wizualna tych ekranów bez podziału na mniejsze komponenty zwiększa ryzyko regresji i niespójności.

### Mało komponentów współdzielonych

Wiele podobnych nagłówków, kart, list, formularzy i stanów jest zapisanych bezpośrednio w stronach. Redesign powinien najpierw zdefiniować wspólny zestaw wzorców, a dopiero potem przebudowywać ekrany.

### Podwójne zarządzanie motywem

MudBlazor ma własną paletę w `MainLayout.razor`, a CSS ma osobne tokeny jasne i ciemne. Docelowo oba poziomy powinny korzystać z tej samej semantycznej palety.

### Wysoka gęstość funkcji

Jedzenie, Trening i Aktywny trening mają wiele działań na jednym ekranie. Redesign nie powinien usuwać funkcji, ale może:

- zastosować progresywne ujawnianie;
- przenieść rzadkie akcje do menu kontekstowego;
- wyraźniej oddzielić tryb przeglądania od edycji;
- ustalić jedną główną akcję na dany etap.

## 15. Co należy zachować podczas redesignu

- nazwę i znak `FORMA/AI`;
- mobile-first i dolną nawigację;
- pięć głównych modułów;
- szybki dostęp do Asystenta;
- ręczne i wspomagane przez AI dodawanie danych;
- obowiązkowe zatwierdzanie szkiców AI;
- czytelne kalorie, makro, serie, ciężar, powtórzenia, RIR i czas;
- pełny oraz ciemny motyw;
- działanie offline i status połączenia;
- istniejące przepływy API;
- edycję i usuwanie danych;
- stany puste, ładowanie i błędy;
- dostępność klawiaturą i ograniczenie ruchu;
- prywatność zdjęć oraz danych zdrowotnych;
- polski język interfejsu.

## 16. Zalecany kierunek nowego wyglądu

Rekomendowany kierunek: **premium fitness operating system**.

Powinien łączyć:

- precyzję profesjonalnego narzędzia treningowego;
- spokój aplikacji zdrowotnej;
- czytelność dziennika;
- subtelny charakter produktu AI;
- wyrazistą, ale nie agresywną typografię;
- niewielką liczbę dobrze zaprojektowanych powierzchni;
- dane prezentowane jako główny materiał interfejsu.

Interfejs nie powinien wyglądać jak:

- generyczny dashboard SaaS;
- zestaw jednakowych, pływających kart;
- futurystyczny neonowy panel;
- aplikacja hazardowa albo gra mobilna;
- klon popularnej aplikacji fitness;
- projekt z losowymi gradientami i nadmiarem szkła;
- makieta, która ignoruje rzeczywistą liczbę funkcji.

## 17. Priorytety redesignu

1. Aktywny trening — maksymalna czytelność i obsługa jedną ręką.
2. Dzisiaj — jasna odpowiedź, co zrobić teraz.
3. Dodawanie posiłku — szybki wybór metody i kontrola szkicu.
4. Jedzenie — czytelny bilans bez nadmiaru kart.
5. Trening — prostsze zarządzanie planem i dniem.
6. Progres — zrozumiałe połączenie diety, treningu i ciała.
7. Profil — mniej przytłaczający kreator celu.
8. Asystent — wyraźne odróżnienie rozmowy od szkicu do zatwierdzenia.

## 18. Prompt główny do generatora wyglądu

Poniższy prompt można wkleić razem z całym dokumentem do narzędzia generującego makiety lub frontend.

```text
Zaprojektuj kompletny, wysokiej jakości redesign responsywnej aplikacji PWA FormaAI.

FormaAI służy do prowadzenia diety, treningów i postępów sylwetkowych. Jest używana głównie na telefonie, wielokrotnie w ciągu dnia i często jedną ręką. Frontend jest zbudowany w Blazor WebAssembly oraz MudBlazor. Redesign ma zmienić warstwę wizualną i kompozycję, ale zachować istniejącą logikę, dane, trasy i funkcje opisane w tym dokumencie.

Kierunek wizualny:
- premium fitness operating system;
- spokojny, precyzyjny i nowoczesny;
- dane są głównym materiałem interfejsu;
- jasna hierarchia zamiast dużej liczby kart;
- subtelny charakter AI bez neonów, szkła i cyberpunku;
- mocna typografia nagłówków, czytelny tekst i monospaced wartości;
- jasny i ciemny motyw;
- mobile-first, podstawowy viewport 390 × 844 px;
- pełna wersja desktopowa 1440 px;
- polska treść interfejsu.

Zachowaj:
- logo tekstowe FORMA/AI;
- dolną nawigację: Dzisiaj, Jedzenie, Progres, Trening, Profil;
- skrót do Asystenta;
- kolory semantyczne dla akcji, jedzenia, regeneracji i błędu;
- wszystkie funkcje i przepływy opisane w dokumencie;
- obowiązkową kontrolę i zatwierdzanie propozycji AI;
- dostępność WCAG AA i obszary dotykowe minimum 44 × 44 px.

Unikaj:
- generycznego dashboardu SaaS;
- jednakowych zaokrąglonych kart dla każdej sekcji;
- nadmiaru kapsułek;
- przypadkowych gradientów;
- dekoracyjnych wykresów bez wartości;
- małych tekstów i przeładowanych toolbarów;
- chowania głównych działań w menu;
- zmiany modelu danych albo logiki aplikacji.

Przygotuj:
1. spójny design system;
2. paletę jasną i ciemną z tokenami semantycznymi;
3. typografię, spacing, promienie, cienie i zasady ruchu;
4. wersję mobilną oraz desktopową;
5. kompletne ekrany Dzisiaj, Jedzenie, Dodaj posiłek, Trening, Aktywny trening, Progres, Profil i Asystent;
6. warianty loading, empty, error, offline, success i disabled;
7. komponenty gotowe do odwzorowania w MudBlazor;
8. krótkie uzasadnienie decyzji projektowych.

Najpierw pokaż architekturę informacji i system wizualny. Następnie przygotuj ekrany. Nie usuwaj funkcji w celu uproszczenia makiety — zastosuj progresywne ujawnianie i czytelną hierarchię.
```

## 19. Prompty dla najważniejszych ekranów

### Dzisiaj

```text
Zaprojektuj mobilny ekran Dzisiaj dla FormaAI. Na pierwszym ekranie bez przewijania pokaż datę, najważniejszy sygnał dnia, stan kalorii i makro, dzisiejszy trening oraz jedną główną akcję. Dodawanie posiłku i rozpoczęcie treningu mają być dostępne jednym ruchem kciuka. Nie twórz siatki wielu podobnych kart. Pokaż także wariant bez treningu, brak danych i aktywną sesję.
```

### Jedzenie

```text
Zaprojektuj dziennik jedzenia FormaAI dla telefonu. Pokaż tydzień, bilans kalorii i makro oraz sekcje posiłków użytkownika. Każdy posiłek musi dać się szybko otworzyć, skopiować i uzupełnić. Dodawanie ma obsługiwać wyszukiwanie, zdjęcie AI i opis AI. Zaprojektuj czytelną hierarchię, która nie zamienia każdej sekcji w ciężką kartę.
```

### Aktywny trening

```text
Zaprojektuj ekran aktywnego treningu FormaAI do obsługi jedną ręką. Najważniejsze są: aktualne ćwiczenie, numer serii, ciężar, powtórzenia, RIR, zapis serii, timer przerwy i przejście dalej. Uwzględnij ostatni wynik, postęp sesji, superserie, zamianę ćwiczenia i dodanie ćwiczenia. Ekran ma być czytelny przy zmęczeniu i bez precyzyjnego klikania.
```

### Trening

```text
Zaprojektuj ekran Trening FormaAI. Połącz dzisiejszą sesję, wznowienie treningu, wariant pełny/30 min/15 min, listę planów oraz bibliotekę ćwiczeń. Zarządzanie planem może korzystać z osobnego trybu edycji lub kreatora. Użytkownik ma natychmiast rozumieć, co trenuje dzisiaj.
```

### Progres

```text
Zaprojektuj ekran Progres FormaAI, który łączy dietę, trening i ciało. Pokaż ocenę kierunku, trend masy, realizację diety, regularność treningu, objętość oraz kalendarz dni. Wykresy mają odpowiadać na konkretne pytania i posiadać czytelne wartości. Zapewnij skróty do pomiarów, zdjęć, tygodnia i osiągnięć.
```

### Profil

```text
Zaprojektuj profil i dziewięciostopniowy kreator celu FormaAI. Proces ma być krótki w odbiorze, pokazywać postęp, wyjaśniać wpływ każdej odpowiedzi i umożliwiać korektę wyniku. Oddziel codzienne ustawienia od konta, prywatności i konfiguracji technicznej.
```

### Asystent

```text
Zaprojektuj Asystenta FormaAI. Wyraźnie rozdziel zwykłą odpowiedź tekstową od propozycji zmieniającej dane. Szkic posiłku, planu albo treningu musi być edytowalny i mieć jednoznaczne akcje Zatwierdź oraz Odrzuć. Interfejs powinien budować zaufanie i pokazywać, że AI nie zapisuje zmian samodzielnie.
```

## 20. Oczekiwane materiały od projektanta lub generatora

Komplet redesignu powinien zawierać:

- mapę informacji;
- tokeny design systemu;
- bibliotekę komponentów;
- ekrany mobilne w szerokości około 390 px;
- reprezentatywne ekrany desktopowe w szerokości 1440 px;
- jasny i ciemny motyw;
- wszystkie ważne stany komponentów;
- opis zachowania responsywnego;
- opis interakcji i ruchu;
- zalecenia do implementacji w Blazor i MudBlazor;
- listę elementów zachowanych oraz świadomie zmienionych.

## 21. Źródła prawdy w repozytorium

Przed implementacją należy porównać projekt z:

- `PRODUCT.md` — cel, użytkownik i zasady produktu;
- `DESIGN.md` — aktualny kierunek wizualny;
- `src/FormaAI.Web/Layout/MainLayout.razor` — rama i nawigacja;
- `src/FormaAI.Web/Pages` — rzeczywiste ekrany i funkcje;
- `src/FormaAI.Web/Components` — obecne komponenty współdzielone;
- `src/FormaAI.Web/wwwroot/css/app.css` — starsze i ekranowe style;
- `src/FormaAI.Web/wwwroot/css/forma-signal.css` — nadrzędny system wizualny;
- `src/FormaAI.Web/Services` — operacje dostępne dla frontendu.

Ten dokument jest briefem projektowym. Kod i kontrakty API pozostają ostatecznym źródłem prawdy dla zachowania aplikacji.
