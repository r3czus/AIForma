# FormaAI — plan rozwoju osobistego coacha

## Cel dokumentu

Ten dokument opisuje rozszerzenie FormaAI jako prywatnej aplikacji do szybkiego zapisywania jedzenia, prowadzenia treningów, obserwowania progresu i podtrzymywania regularności.

FormaAI nie ma być portalem społecznościowym ani aplikacją dla trenerów. Główna pętla produktu powinna wyglądać tak:

1. użytkownik szybko zapisuje jedzenie i trening;
2. aplikacja pokazuje, co wynika z zebranych danych;
3. aplikacja wskazuje jeden sensowny kolejny krok;
4. użytkownik dostaje przypomnienie tylko wtedy, kiedy naprawdę jest potrzebne;
5. regularność i realny progres są zauważane i wzmacniają motywację.

## Zakres

Dokument obejmuje:

- Coach dnia;
- inteligentne przypomnienia;
- regularność i momentum;
- tygodniowe podsumowanie poprzedzone uzupełnieniem danych;
- propozycje progresji ćwiczeń;
- skrócone wersje treningów;
- elastyczne przekładanie treningów;
- prywatne zdjęcia progresu;
- osiągnięcia oparte na realnych danych.

Świadomie poza zakresem pozostają:

- osobne nagrywanie głosu — użytkownik może korzystać z dyktowania dostępnego w klawiaturze telefonu;
- uczenie AI indywidualnych nazw i porcji na podstawie każdej ręcznej poprawki;
- widgety systemowe;
- funkcje społecznościowe, rankingi i publiczne profile;
- automatyczne zapisywanie zmian zaproponowanych przez AI.

## Zasady wspólne

### Jedna najważniejsza akcja

FormaAI powinna ograniczać liczbę jednoczesnych wezwań do działania. Na ekranie `Dzisiaj` aplikacja wybiera jedną najważniejszą akcję. Pozostałe informacje pozostają dostępne niżej, ale nie konkurują o uwagę.

### Reguły wybierają, AI wyjaśnia

Wybór akcji, obliczenia progresu, serie regularności i wykrywanie brakujących danych powinny być deterministyczne. AI może:

- napisać krótkie, naturalne wyjaśnienie;
- podsumować zależności w danych;
- zaproponować następny krok;
- przygotować szkic zmiany.

AI nie powinno samodzielnie zmieniać celu kalorii, planu, harmonogramu ani ciężaru. Zmiana zawsze wymaga zatwierdzenia.

### Brak udawanej wiedzy

Brak wpisu nie oznacza zjedzenia zera kalorii, wykonania postu ani pominięcia posiłku. Niepełny dzień jest oznaczany jako `brak danych` albo `dane częściowe`. AI otrzymuje tę informację wprost i nie może uzupełniać jej domysłami.

### Motywacja bez karania

Komunikaty powinny wspierać powrót do rytmu. Nie należy stosować komunikatów typu „zmarnowałeś serię” albo „porażka”. Ważniejsza jest regularność tygodniowa niż perfekcyjna seria dni.

---

# 1. Coach dnia

## Cel

Coach dnia ma odpowiedzieć na pytanie: **co jest teraz najważniejszą rzeczą, którą mogę zrobić w FormaAI?**

Nie jest to osobna rozmowa z asystentem. Jest to dynamiczna karta na ekranie `Dzisiaj`, której treść wynika z aktualnego stanu aplikacji.

## Umiejscowienie

Karta powinna znajdować się na stronie `Dzisiaj`:

1. pod nagłówkiem i paskiem `Trening / Energia / Kierunek`;
2. przed kartą dzisiejszego treningu i dziennikiem posiłków;
3. na telefonie powinna być widoczna bez długiego przewijania.

## Budowa karty

Karta zawiera:

- małą etykietę `Twój kolejny krok`;
- jednozdaniowy tytuł;
- krótkie wyjaśnienie, dlaczego aplikacja to proponuje;
- jedną główną akcję;
- opcjonalną akcję drugorzędną, np. `Przypomnij później`;
- opcjonalny link `Dlaczego to widzę?`.

Przykład treningowy:

> **Dzisiaj wypada trening Pull**  
> Ostatnią sesję wykonałeś zgodnie z planem. Dzisiejszy trening zajmie około 52 minut.  
> `[Rozpocznij trening]` `[Przypomnij za godzinę]`

Przykład żywieniowy:

> **Zostało około 38 g białka**  
> Dzienny cel jest jeszcze możliwy do osiągnięcia bez dużego przekroczenia kalorii.  
> `[Zaproponuj posiłek]`

Przykład po przerwie:

> **Wróć dzisiaj małym krokiem**  
> Od ostatniego treningu minęło 6 dni. Możesz rozpocząć pełną albo skróconą sesję.  
> `[Wybierz trening]` `[Wersja 25 min]`

## Kolejność priorytetów

Coach dnia wybiera pierwszą pasującą sytuację:

1. aktywna, niedokończona sesja treningowa — `Wróć do treningu`;
2. zaległy trening wymagający decyzji — `Przełóż lub pomiń`;
3. zaplanowany trening na dziś — `Rozpocznij trening`;
4. dostępne tygodniowe podsumowanie — `Uzupełnij tydzień`;
5. brak ważnego pomiaru masy przez ustalony czas — `Dodaj pomiar`;
6. dziennik wygląda na częściowy pod koniec dnia — `Uzupełnij lub oznacz jako częściowy`;
7. na podstawie zapisanych posiłków można sensownie uzupełnić makro — `Zaproponuj posiłek`;
8. wszystkie najważniejsze zadania wykonano — komunikat wzmacniający, bez kolejnego obowiązku.

## Dane wejściowe

- dzisiejszy i zaległy harmonogram treningów;
- aktywna sesja;
- dzisiejsze posiłki i cel żywieniowy;
- status kompletności dziennika;
- data ostatniego pomiaru;
- dostępność tygodniowego check-inu;
- godzina lokalna i ustawione godziny posiłków;
- odłożone wcześniej przypomnienia.

## Proponowany kontrakt odpowiedzi

```text
DailyCoachCard
- type
- priority
- title
- description
- primaryActionLabel
- primaryActionUrl
- secondaryAction
- reasonCode
- validUntil
```

Treść podstawowa powinna mieć gotowy wariant bez AI. Dzięki temu karta działa także wtedy, gdy model jest wyłączony albo niedostępny.

## Kryteria akceptacji

- jednocześnie wyświetlana jest maksymalnie jedna karta Coacha dnia;
- karta nie twierdzi, że dziennik jest kompletny bez potwierdzenia użytkownika;
- aktywny trening ma pierwszeństwo przed innymi sugestiami;
- użytkownik może odłożyć przypomnienie bez zmiany planu;
- każda główna akcja prowadzi bezpośrednio do właściwego ekranu.

---

# 2. Inteligentne przypomnienia

## Cel

Przypomnienie powinno pojawić się wtedy, gdy może zmienić zachowanie, a nie tylko dlatego, że nadeszła określona godzina.

## Typy przypomnień

### Trening

- przed planowaną godziną treningu;
- gdy trening nie został rozpoczęty w wybranym oknie;
- gdy w planie pozostała zaległa sesja;
- gdy aktywna sesja pozostaje niedokończona;
- po dłuższej przerwie od treningów.

### Jedzenie

- o porze aktywnego posiłku, jeżeli użytkownik chce takich powiadomień;
- wieczorem, jeśli dziennik prawdopodobnie jest niekompletny;
- bez przypomnienia, jeżeli wpis w danej sekcji już istnieje.

### Progres

- o pomiarze masy w wybrany dzień i godzinę;
- o zdjęciu progresu co 2 lub 4 tygodnie;
- o tygodniowym check-inie;
- o gotowym podsumowaniu tygodnia.

## Ustawienia

W `Profil → Ustawienia → Przypomnienia` należy rozdzielić:

- przypomnienia o posiłkach;
- przypomnienia o treningach;
- pomiar masy;
- zdjęcia progresu;
- tygodniowy check-in;
- podsumowanie tygodnia;
- godziny ciszy;
- maksymalną liczbę przypomnień dziennie;
- domyślny czas odłożenia, np. 30, 60 lub 120 minut.

## Zachowanie powiadomienia treningowego

Kliknięcie powiadomienia otwiera kartę decyzji w aplikacji:

- `Rozpocznij trening`;
- `Przypomnij za godzinę`;
- `Przenieś na jutro`;
- `Wybierz inny termin`;
- `Pomiń tę sesję`.

Akcje bezpośrednio w systemowym powiadomieniu można potraktować jako ulepszenie zależne od obsługi przeglądarki. Podstawowy przepływ musi działać po zwykłym kliknięciu powiadomienia.

## Wymagania techniczne PWA

Obecne timery uruchomione w otwartej stronie nie wystarczą. Docelowy mechanizm powinien wykorzystywać:

- subskrypcję Web Push;
- service workera;
- harmonogram przypomnień po stronie serwera;
- zapis strefy czasowej;
- unieważnianie przypomnienia po wykonaniu czynności;
- deduplikację, aby użytkownik nie otrzymał kilku komunikatów o tym samym zadaniu.

## Kryteria akceptacji

- przypomnienia mogą działać po zamknięciu karty przeglądarki, jeśli system i przeglądarka wspierają Web Push;
- wykonany trening nie generuje późniejszego przypomnienia o tym treningu;
- godziny ciszy są zawsze respektowane;
- użytkownik może wyłączyć każdy typ przypomnienia osobno;
- odmowa zgody na powiadomienia nie blokuje pozostałych funkcji aplikacji.

---

# 3. Regularność i momentum

## Cel

Użytkownik powinien widzieć, że regularność rośnie, nawet jeśli nie każdy dzień jest idealny.

## Najważniejszy wskaźnik

Głównym wskaźnikiem treningowym powinna być seria udanych tygodni:

> **4 tygodnie w rytmie**  
> W każdym z ostatnich 4 tygodni wykonałeś swój minimalny cel treningowy.

Udany tydzień oznacza wykonanie celu skonfigurowanego przez użytkownika, np. 3 treningów. Można dopuścić tolerancję, np. 2 z 3 treningów oznacza `częściowo`, ale nie przedłuża pełnej serii.

## Dodatkowe wskaźniki

- treningi wykonane w bieżącym tygodniu;
- procent wykonania planowanych sesji w miesiącu;
- liczba tygodni z rzędu z minimum jednym treningiem;
- liczba dni z kompletnym dziennikiem jedzenia;
- liczba tygodni z wykonanym check-inem;
- powrót po przerwie, np. `pierwszy trening po 8 dniach`.

## Umiejscowienie

### Dzisiaj

Mały pasek momentum pod kartą Coacha dnia:

> `3/3 treningów w tym tygodniu · 4 tygodnie w rytmie`

### Progres

Osobna sekcja `Regularność`:

- kalendarz ostatnich 12 tygodni;
- treningi planowane i wykonane;
- kompletność dziennika jedzenia;
- tygodniowe check-iny;
- najlepsza i aktualna seria.

### Podsumowanie treningu

Po zakończeniu sesji:

> To trzeci trening w tym tygodniu. Cel tygodniowy wykonany.

## Status kompletności dziennika

Każdy dzień żywieniowy powinien otrzymać jeden status:

- `kompletny` — użytkownik świadomie zakończył dzień;
- `częściowy` — użytkownik potwierdził brak części wpisów;
- `brak danych` — nie wiadomo, czy wpisy są kompletne;
- `bez zapisu` — brak posiłków i brak potwierdzenia.

Samo dodanie jednego posiłku nie może automatycznie tworzyć pełnej serii żywieniowej.

## Kryteria akceptacji

- jeden pominięty dzień nie usuwa serii udanych tygodni;
- serie są liczone z danych domenowych, nie przez AI;
- użytkownik może sprawdzić, dlaczego tydzień otrzymał dany status;
- niepełne dane żywieniowe nie są traktowane jako realizacja kalorii.

---

# 4. Tygodniowe podsumowanie z uzupełnieniem danych

## Cel

Podsumowanie ma być wiarygodne. Przed uruchomieniem AI aplikacja prowadzi użytkownika przez krótki workflow uzupełnienia informacji, których nie da się bezpiecznie wywnioskować.

## Uruchomienie

Podsumowanie jest dostępne:

- w ostatnim dniu tygodnia ustawionym przez użytkownika;
- z karty Coacha dnia;
- w ekranie `Progres → Podsumowania tygodniowe`;
- później na żądanie dla zakończonego tygodnia.

## Workflow

### Krok 1 — kompletność jedzenia

Aplikacja pokazuje siedem dni i ich statusy. Dla dni niepewnych użytkownik wybiera:

- `Dziennik jest kompletny`;
- `Zapisałem tylko część jedzenia`;
- `Nie pamiętam / brak danych`;
- `Uzupełnij posiłki`.

FormaAI nie pyta użytkownika o odtwarzanie wszystkich posiłków z pamięci. Chodzi przede wszystkim o oznaczenie jakości danych.

### Krok 2 — brakujące pomiary

Jeżeli w tygodniu brakuje masy ciała, aplikacja proponuje:

- dodanie aktualnego pomiaru;
- kontynuowanie bez pomiaru;
- przypomnienie następnego ranka.

Brak pomiaru nie blokuje podsumowania, ale ogranicza wnioski dotyczące trendu masy.

### Krok 3 — samopoczucie

Krótki check-in:

- energia 1–5;
- sen 1–5;
- głód 1–5;
- regeneracja 1–5;
- opcjonalnie stres 1–5;
- jedno pole: `Co miało największy wpływ na ten tydzień?`.

### Krok 4 — powody odstępstw

Jeżeli były pominięte treningi albo dużo niepełnych dni, użytkownik może zaznaczyć przyczyny:

- brak czasu;
- choroba lub ból;
- podróż;
- słaby sen lub zmęczenie;
- zmiana planu dnia;
- brak motywacji;
- inny powód.

Pole jest opcjonalne. Dane mają pomóc odróżnić problem z planem od jednorazowego tygodnia.

### Krok 5 — podgląd danych dla AI

Przed analizą aplikacja pokazuje:

- liczbę kompletnych, częściowych i nieznanych dni;
- liczbę wykonanych treningów;
- dostępne pomiary;
- odpowiedzi check-inu;
- zakres historii użytej w porównaniu.

Użytkownik wybiera `Generuj podsumowanie`.

### Krok 6 — wynik

Podsumowanie zawiera maksymalnie pięć części:

1. **Najważniejszy sukces**;
2. **Co pokazują dane**;
3. **Czego nie da się stwierdzić** ze względu na braki;
4. **Jedna rzecz do poprawy**;
5. **Jedna propozycja na następny tydzień**.

Przykład:

> Wykonałeś wszystkie 3 treningi i zwiększyłeś wynik w wyciskaniu. Trend masy pozostaje stabilny. Dwa dni żywieniowe były częściowe, dlatego nie można wiarygodnie ocenić średniej kalorii. W następnym tygodniu najważniejsze będzie regularne domykanie dziennika wieczorem.

## Zakres historii

AI powinno otrzymywać:

- bieżący tydzień;
- zagregowane dane z poprzednich 4–8 tygodni;
- trend masy, a nie tylko pierwszy i ostatni pomiar;
- zmianę objętości oraz e1RM dla głównych ćwiczeń;
- historię check-inów;
- status kompletności każdego tygodnia;
- wcześniej zaakceptowane cele lub zalecenia.

Nie należy wysyłać całej historii pojedynczych posiłków, jeśli do wniosku wystarczą agregaty. Szczegółowe posiłki można dołączyć tylko wtedy, gdy użytkownik pyta o konkretny problem żywieniowy.

## Propozycje zmian

Podsumowanie może przygotować szkic:

- zmiany celu kalorii lub makro;
- przesunięcia treningów;
- tygodnia deload;
- ograniczenia objętości;
- prostego celu zachowania, np. `3 kompletne dni dziennika`.

Każda zmiana ma osobne przyciski `Zatwierdź`, `Edytuj`, `Pomiń`. Samo wygenerowanie podsumowania niczego nie zmienia.

## Kryteria akceptacji

- podsumowanie wskazuje braki danych;
- częściowe dni nie są liczone jako faktyczne niskie spożycie;
- użytkownik może wygenerować podsumowanie mimo braków;
- analiza porównuje kilka tygodni, ale wyraźnie odróżnia tygodnie kompletne od niekompletnych;
- żadna rekomendacja nie jest automatycznie zapisywana.

---

# 5. Progresja ćwiczeń

## Cel

Po treningu FormaAI proponuje następne obciążenie lub zakres powtórzeń, a zaakceptowana wartość staje się podpowiedzią przy kolejnym wystąpieniu tego ćwiczenia.

## Podsumowanie treningu

Pod listą wyników pojawia się sekcja `Propozycje na następny raz`.

Przykład:

> **Wyciskanie sztangi**  
> Dzisiaj: 80 kg × 10, RIR 2  
> Cel planu: 8–10 powtórzeń, RIR 2  
> Następnym razem: **82,5 kg × 8–10**  
> `[Zatwierdź]` `[Zmień]` `[Bez zmiany]`

## Początkowe reguły

- osiągnięty górny zakres we wszystkich seriach i odpowiedni RIR → propozycja zwiększenia ciężaru;
- osiągnięty środek zakresu → pozostawienie ciężaru i próba dodania powtórzeń;
- wynik poniżej dolnego zakresu lub RIR niższy od celu → brak zwiększenia;
- dwa lub trzy kolejne regresy → propozycja zmniejszenia ciężaru albo deloadu;
- ćwiczenia jednostronne i z masą ciała wymagają osobnych typów progresji;
- skok ciężaru powinien uwzględniać dostępny krok, np. 1, 2,5 lub 5 kg.

## Następna sesja

Przy pierwszej serii tego samego ćwiczenia pola są wypełniane zaakceptowaną propozycją. Interfejs pokazuje źródło:

> Sugerowane na podstawie treningu z 14 lipca.

Użytkownik może dowolnie zmienić ciężar. Propozycja nie zmienia na stałe planu treningowego ani zakresu powtórzeń.

## Dane

Warto rozdzielić:

- wyliczoną rekomendację;
- decyzję użytkownika;
- wartość zaakceptowaną;
- sesję źródłową;
- datę wykorzystania rekomendacji.

## Kryteria akceptacji

- rekomendacja powstaje dopiero po zakończeniu treningu;
- użytkownik widzi podstawę propozycji;
- odrzucenie nie wpływa na plan;
- zaakceptowana wartość wypełnia pola przy kolejnym ćwiczeniu;
- brak RIR lub niepełne serie powodują ostrożniejszą rekomendację.

---

# 6. Skrócony trening

## Cel

Użytkownik, który nie ma czasu albo energii na pełną sesję, powinien móc wykonać sensowną krótszą wersję zamiast całkowicie rezygnować.

## Wejście

Przed rozpoczęciem treningu karta oferuje:

- `Pełny trening`;
- `Około 30 minut`;
- `Około 15 minut`.

Coach dnia może pokazać skrót bezpośrednio po dłuższej przerwie albo po odłożeniu treningu.

## Zasady skracania

Wersja skrócona:

- nie zmienia oryginalnego planu;
- zachowuje najważniejsze ruchy danego dnia;
- najpierw ogranicza ćwiczenia dodatkowe;
- następnie zmniejsza liczbę serii;
- nie skraca przerw do wartości pogarszających bezpieczeństwo głównych ćwiczeń;
- pokazuje szacowany czas przed rozpoczęciem.

Przykład sesji 60-minutowej:

- pełna: 6 ćwiczeń, 18 serii;
- 30 minut: 4 ćwiczenia, 10–12 serii;
- 15 minut: 2–3 ćwiczenia, 6–8 serii.

## AI i reguły

Pierwsza wersja może używać kolejności ćwiczeń i ich grup mięśniowych. AI może później wyjaśnić wybór, ale nie jest wymagane do utworzenia skrótu.

## Zapis progresu

Sesja otrzymuje oznaczenie `skrócona`, wybrany limit czasu i stopień wykonania wygenerowanego wariantu. W podsumowaniu tygodnia skrócony trening jest wykonanym treningiem, ale raport może odróżnić go od pełnej sesji.

## Kryteria akceptacji

- oryginalny plan pozostaje bez zmian;
- użytkownik przed startem widzi ćwiczenia i szacowany czas;
- skrócony trening zapisuje się w normalnej historii;
- trening jest liczony do regularności;
- podsumowanie nie przedstawia mniejszej objętości jako regresu bez uwzględnienia trybu skróconego.

---

# 7. Przekładanie treningów

## Cel

Pominięcie jednego dnia nie powinno wymuszać ręcznej przebudowy całego planu ani powodować, że aplikacja stale pokazuje nieaktualną sesję.

## Wykrycie zaległości

Jeśli dzień treningowy minął bez ukończonej sesji, Coach dnia pokazuje:

> Wczoraj wypadał trening nóg. Co robimy z tą sesją?

Opcje:

- `Wykonaj dzisiaj`;
- `Przenieś na jutro`;
- `Wybierz datę`;
- `Przesuń pozostałe treningi tygodnia`;
- `Pomiń sesję`.

## Zasada implementacyjna

Nie należy zmieniać bazowego dnia tygodnia w definicji planu przy każdym przesunięciu. Potrzebna jest warstwa wykonawcza harmonogramu, np. wyjątek wskazujący:

- pierwotną datę;
- nową datę;
- powód lub decyzję;
- status `przeniesiony`, `pominięty`, `wykonany`;
- identyfikator dnia treningowego.

Dzięki temu plan `Poniedziałek / Środa / Piątek` pozostaje planem, a konkretny tydzień może mieć wyjątki.

## Konflikty

Jeżeli przeniesienie tworzy dwa treningi jednego dnia albo sesje dzień po dniu, aplikacja ostrzega i proponuje:

- zamianę kolejności;
- przesunięcie kolejnych sesji;
- pominięcie jednej sesji;
- pozostawienie decyzji użytkownikowi.

AI może opisać konsekwencje, ale nie powinno samodzielnie podejmować decyzji.

## Widoki

- `Dzisiaj` — najbliższa obowiązująca sesja;
- `Trening` — pasek bieżącego tygodnia z datami i statusami;
- `Progres` — planowane, wykonane, przeniesione i pominięte sesje;
- podsumowanie tygodnia — liczba przesunięć oraz powody, jeżeli je podano.

## Kryteria akceptacji

- przesunięcie nie modyfikuje szablonu planu;
- każda sesja w tygodniu ma jednoznaczny status;
- pominięty trening nie wraca bez końca jako zaległy;
- Coach dnia uwzględnia wyjątki harmonogramu;
- użytkownik może cofnąć przesunięcie przed rozpoczęciem sesji.

---

# 8. Zdjęcia progresu

## Cel

Zdjęcia mają pokazywać zmiany, których nie widać w samej masie i obwodach. Są całkowicie prywatne.

## Umiejscowienie

### Progres

Nowa karta `Zdjęcia sylwetki` pod kartą masy i obwodów:

- ostatnie zdjęcie;
- data następnego przypomnienia;
- przycisk `Dodaj zdjęcia`;
- przycisk `Porównaj`.

### Osobny ekran

Trasa `Progres → Zdjęcia` zawiera:

- chronologiczną galerię;
- filtrowanie `przód / bok / tył`;
- wybór dwóch dat do porównania;
- możliwość poprawienia daty i typu zdjęcia;
- usunięcie zdjęcia.

## Dodawanie

Workflow:

1. wybór daty;
2. opcjonalne zdjęcie przodem;
3. opcjonalne zdjęcie bokiem;
4. opcjonalne zdjęcie tyłem;
5. podgląd;
6. zapis.

Aplikacja pokazuje krótkie wskazówki: podobne światło, odległość, ustawienie telefonu i pora dnia. Nie wykonuje automatycznej oceny wyglądu.

## Porównanie

Widok porównawczy pokazuje:

- zdjęcia obok siebie lub suwak przed/po;
- daty;
- różnicę masy;
- różnicę dostępnych obwodów;
- czas między zdjęciami.

## Prywatność i przechowywanie

- zdjęcia są prywatne i dostępne tylko właścicielowi;
- nie są przekazywane do modelu AI domyślnie;
- osobna, jawna zgoda byłaby wymagana dla przyszłej analizy zdjęcia;
- pliki nie powinny znajdować się w publicznym katalogu aplikacji;
- dostęp powinien odbywać się przez autoryzowany endpoint lub krótkotrwały adres;
- użytkownik może usunąć pojedyncze zdjęcie i wyeksportować wszystkie zdjęcia;
- usunięcie konta usuwa również pliki.

## Kryteria akceptacji

- możliwe jest zapisanie tylko jednego wybranego ujęcia;
- zdjęcie innego użytkownika nigdy nie jest dostępne;
- porównanie działa dla tej samej pozycji;
- usunięcie zdjęcia usuwa rekord oraz właściwy plik;
- AI nie otrzymuje zdjęć bez osobnej akcji użytkownika.

---

# 9. Osiągnięcia i kamienie milowe

## Cel

Osiągnięcia mają zauważać realną pracę, a nie tworzyć system punktów. Nie ma rankingów, poziomów ani porównywania z innymi osobami.

## Rodzaje

### Regularność

- pierwszy pełny tydzień planu;
- 4, 8, 12 i 26 tygodni w rytmie;
- 10, 25, 50 i 100 wykonanych treningów;
- powrót do treningu po dłuższej przerwie.

### Trening

- pierwszy zapisany trening;
- nowy rekord ciężaru, powtórzeń albo e1RM;
- wzrost e1RM o 5%, 10% i 20% względem punktu startowego;
- ukończenie wszystkich zaplanowanych treningów w miesiącu;
- pierwszy zaakceptowany i wykonany krok progresji.

### Sylwetka

- pierwszy pomiar;
- pierwsze zdjęcie progresu;
- połowa drogi do celu masy;
- osiągnięcie docelowego zakresu masy;
- istotna zmiana obwodu zgodna z celem.

### Jedzenie

- pierwszy kompletny dzień;
- 7 kompletnych dni, niekoniecznie z rzędu;
- pierwszy tydzień z osiągniętym własnym celem regularności;
- regularny check-in przez 4 tygodnie.

## Umiejscowienie

### Po treningu

Nowe osiągnięcie pojawia się na ekranie podsumowania, bez blokującego pełnoekranowego dialogu.

### Dzisiaj

Ostatnie osiągnięcie może pojawić się jako krótka karta przez 24–48 godzin:

> **25 treningów za Tobą**  
> W ciągu ostatnich 10 tygodni wykonałeś 25 sesji.

### Progres

Sekcja `Kamienie milowe` pokazuje:

- ostatnie osiągnięcia;
- najbliższe naturalne kamienie milowe;
- historię z datami.

Nie należy pokazywać wielu zablokowanych odznak, które nie mają związku z celem użytkownika.

## Reguły

- osiągnięcia są wyliczane deterministycznie;
- każde osiągnięcie jest nadawane tylko raz, chyba że opisuje bieżącą serię;
- rekord uwzględnia typ serii i nie powinien korzystać z serii rozgrzewkowych;
- skrócony trening może liczyć się do regularności;
- korekta lub usunięcie błędnych danych przelicza osiągnięcia wymagające tych danych;
- język komunikatów pozostaje rzeczowy i wspierający.

## Kryteria akceptacji

- użytkownik może wyłączyć celebracje, pozostawiając historię;
- osiągnięcia nie korzystają z AI do ustalenia, czy warunek został spełniony;
- karta osiągnięcia pokazuje konkretne dane będące podstawą;
- nie ma punktów, publicznych rankingów ani presji społecznej.

---

# 10. Połączenie funkcji w jeden przepływ

## Zwykły dzień treningowy

1. Coach dnia pokazuje trening.
2. Przypomnienie pojawia się tylko wtedy, gdy trening nie został rozpoczęty.
3. Użytkownik wybiera pełną albo skróconą wersję.
4. Po treningu widzi progres, osiągnięcia i propozycje ciężaru na następny raz.
5. Zaakceptowana propozycja wypełnia pierwszą serię przy kolejnym wystąpieniu ćwiczenia.

## Opuszczony trening

1. FormaAI wykrywa zaległą sesję.
2. Coach dnia prosi o decyzję.
3. Użytkownik przekłada, skraca albo pomija trening.
4. Harmonogram tygodnia aktualizuje się bez zmiany szablonu planu.
5. Tygodniowe podsumowanie zna decyzję i nie interpretuje jej jako niewyjaśnionego braku danych.

## Koniec tygodnia

1. Coach dnia zaprasza do zamknięcia tygodnia.
2. Użytkownik oznacza kompletność dni żywieniowych.
3. Dodaje brakujący pomiar albo świadomie go pomija.
4. Uzupełnia check-in i opcjonalne powody odstępstw.
5. Widzi zakres danych przekazywanych do analizy.
6. AI tworzy podsumowanie na tle poprzednich tygodni.
7. Użytkownik osobno zatwierdza albo odrzuca każdą propozycję.
8. Momentum i kamienie milowe są aktualizowane na podstawie faktycznych danych.

---

# 11. Kolejność realizacji

## Etap 1 — fundament motywacyjny

- status kompletności dziennika żywieniowego;
- Coach dnia oparty na regułach;
- pasek momentum na stronie `Dzisiaj`;
- widok regularności w `Progres`.

Ten etap nie wymaga rozszerzania AI i daje natychmiastową wartość.

## Etap 2 — przypomnienia

- szczegółowe ustawienia powiadomień;
- subskrypcje Web Push;
- harmonogram serwerowy;
- anulowanie nieaktualnych przypomnień;
- odłożenie i obsługa godzin ciszy.

## Etap 3 — zamknięcie tygodnia

- workflow kompletności danych;
- rozszerzony check-in;
- historia zagregowanych tygodni;
- podgląd danych dla AI;
- generowanie podsumowania;
- szkice zmian wymagające zatwierdzenia.

## Etap 4 — elastyczny trening

- wyjątki harmonogramu i przekładanie sesji;
- warianty 15 i 30 minut;
- oznaczenie skróconych treningów;
- propozycje progresji po sesji;
- domyślne wartości na następny trening.

## Etap 5 — motywacja wizualna

- kamienie milowe;
- celebracje po treningu;
- prywatne zdjęcia progresu;
- porównywanie dwóch terminów;
- przypomnienia o kolejnych zdjęciach.

---

# 12. Mierniki powodzenia

Po wdrożeniu warto obserwować prywatne, zagregowane wskaźniki produktu albo mierzyć je wyłącznie lokalnie dla użytkownika:

- czas od wejścia do zapisania posiłku;
- procent rozpoczętych zaplanowanych treningów;
- procent zaległych sesji, dla których podjęto świadomą decyzję;
- liczba tygodni z wykonanym check-inem;
- odsetek podsumowań z oznaczoną kompletnością danych;
- częstotliwość wyboru skróconego treningu zamiast pominięcia;
- procent zaakceptowanych propozycji progresji;
- liczba tygodni utrzymanego momentum;
- powrót do aplikacji po przerwie.

Sukcesem nie jest maksymalna liczba powiadomień ani czas spędzony w aplikacji. Sukcesem jest szybsze zapisywanie danych, większa liczba świadomie wykonanych działań i lepsza regularność.
