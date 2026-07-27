# Poprawki treningu, jedzenia, zdjęć i asystenta

## Cel

Pakiet usuwa wskazane problemy bez zmiany kierunku wizualnego FormaAI. Interfejs pozostaje mobilny, krótki i oparty na istniejących komponentach. Zmiany obejmują:

- czytelną listę dni planu i szczegóły ćwiczenia;
- dopasowanie zapisanego posiłku oraz propozycji AI do zadanej kaloryczności;
- usunięcie pustej przestrzeni pod formularzem propozycji AI;
- wybór wielu zdjęć posiłku i wielu zdjęć progresu z galerii telefonu;
- konkretne propozycje dań dopasowane do brakującego makro.

Prace powstają na branchu `poprawki/trening-jedzenie-zdjecia-asystent`. Każdy zamknięty moduł będzie osobnym commitem z polskim opisem.

## 1. Plan treningowy i szczegóły ćwiczenia

### Lista dni

Obecna karta planu zostaje zachowana, ale jej wewnętrzna siatka trzech kolumn zostanie zastąpiona listą dni na pełną szerokość. Każdy dzień będzie osobnym rozwijanym wierszem:

- nagłówek nadal pokaże numer, nazwę, dzień tygodnia i liczbę ćwiczeń;
- rozwinięcie pokaże ćwiczenia jedno pod drugim;
- rozwinięty dzień nie będzie tworzył wysokiej pustej kolumny obok pozostałych dni;
- układ na telefonie i komputerze będzie korzystał z tego samego pionowego przepływu.

### Nawigacja do ćwiczenia

Nazwa ćwiczenia w rozwiniętym dniu będzie elementem interaktywnym. Kliknięcie otworzy stronę szczegółów ćwiczenia z kontekstem planu i dnia. Powrót zachowa naturalną nawigację do listy planów.

Strona pokaże:

- nazwę i opis wykonania;
- sprzęt oraz informację o ćwiczeniu jednostronnym;
- główną grupę mięśniową i procentowe zaangażowanie pozostałych grup;
- parametry wpisane w planie: serie, zakres powtórzeń, docelowy RIR i czas przerwy;
- przycisk edycji parametrów danego dnia planu;
- przycisk edycji definicji ćwiczenia tylko wtedy, gdy jest to własne ćwiczenie użytkownika.

Brak opisu lub dodatkowych udziałów mięśniowych będzie pokazany jako krótki stan informacyjny, a nie jako pusta sekcja. API udostępni pobranie pojedynczego ćwiczenia z kontrolą dostępu do ćwiczeń globalnych i własnych.

## 2. Dopasowanie kaloryczności posiłku

### Wspólna reguła przeliczania

Zarówno edycja zapisanego posiłku w dzienniku, jak i formularz propozycji AI otrzymają pole `Docelowe kalorie`. Użytkownik wpisze łączną wartość dania i uruchomi przeliczenie.

Przeliczenie działa według wzoru:

`współczynnik = docelowe kalorie / obecne kalorie`

Każda ilość składnika zostanie pomnożona przez ten sam współczynnik. Kalorie, białko, tłuszcze i węglowodany wynikają następnie z nowych ilości, dzięki czemu proporcje dania pozostają bez zmian. Ilości będą zaokrąglane z dokładnością odpowiednią dla istniejących formularzy, bez zerowania małych dodatków.

Reguły błędów:

- wartość docelowa musi być większa od zera;
- nie można skalować dania, którego aktualna kaloryczność wynosi zero;
- pusta lista składników nie uruchamia przeliczenia;
- komunikat wyjaśnia problem i nie zmienia danych częściowo.

Po przeliczeniu nadal można ręcznie poprawiać ilości oraz wartości odżywcze pojedynczych składników. W zapisanym posiłku zmienione ilości zostaną utrwalone przez istniejący przepływ edycji. W propozycji AI zostaną zapisane dopiero po kliknięciu przycisku dodania do dziennika.

### Formularz bez pustej przestrzeni

Kontener formularza propozycji AI będzie miał wysokość wynikającą z zawartości. Po ostatniej informacji pod formularzem nie będzie wymuszonego pustego obszaru. Zostaną zachowane bezpieczne odstępy dolne dla telefonu i pasek nawigacji PWA.

## 3. Wiele zdjęć

### Zdjęcia posiłku

Ekran analizy posiłku rozdzieli dwa czytelne działania:

- zrobienie jednego zdjęcia aparatem;
- wybranie od jednego do pięciu zdjęć z galerii.

Wszystkie wybrane zdjęcia dotyczą tego samego dania i zostaną wysłane w jednym żądaniu analizy. Model otrzyma komplet obrazów, aby mógł połączyć widok całego talerza, etykiety lub dodatkowe ujęcia w jeden szkic posiłku.

Limity:

- maksymalnie 5 zdjęć;
- maksymalnie 12 MB na plik;
- obsługiwane formaty pozostają zgodne z obecnym formularzem;
- interfejs pokaże nazwy lub liczbę wybranych plików i pozwoli ponowić wybór.

Walidacja nastąpi przed wywołaniem modelu. Niepoprawna partia nie utworzy częściowego szkicu, a komunikat wskaże konkretny plik i powód.

### Zdjęcia progresu

Ekran zdjęć progresu pozwoli zaznaczyć od jednego do pięciu plików z galerii. Wybrana data i poza dotyczą całej partii. Pliki będą zapisywane kolejno przez istniejący, zabezpieczony endpoint, aby awaria jednego pliku nie usuwała poprawnie zapisanych zdjęć.

Po zakończeniu użytkownik otrzyma podsumowanie liczby zapisanych i odrzuconych plików. Lista odświeży się raz po całej operacji. Przycisk zapisu będzie zablokowany podczas wysyłania, co zapobiegnie podwójnym żądaniom.

## 4. Asystent dopasowujący danie do brakującego makro

### Dane

Narzędzie dziennego podsumowania żywienia zwróci:

- cel;
- dotychczas spożyte wartości;
- różnicę `pozostało = cel - spożycie`;
- przekroczenia, jeśli któraś różnica jest ujemna.

Różnice pozostaną wartościami ze znakiem, aby asystent nie próbował „dobijać” składnika, którego użytkownik już zjadł za dużo. Brak aktywnego celu zostanie zwrócony jawnie.

### Zachowanie rozmowy

Instrukcja asystenta określi obowiązkową sekwencję dla próśb o dobicie kalorii lub makro:

1. pobierz dzisiejsze podsumowanie;
2. sprawdź preferencje i alergie;
3. wyszukaj pasujące produkty, przepisy lub zawartość spiżarni;
4. policz każdą propozycję narzędziem kalkulacyjnym;
5. podaj maksymalnie trzy konkretne dania.

Odpowiedź będzie zawierała:

- krótkie podsumowanie brakujących wartości;
- nazwę dania, składniki i gramatury;
- wyliczone kalorie i makro dania;
- wartości, które pozostaną po jego zjedzeniu.

Asystent nie będzie wymyślał produktów ani wartości. Jeśli nie ma ustawionego celu, poprosi o jego ustawienie. Jeśli użytkownik chce tylko propozycję, nie utworzy szkicu. `create_meal_draft` zostanie użyte dopiero na wyraźną prośbę, a zapis nadal wymaga jawnego zatwierdzenia w aplikacji.

## 5. Przepływ danych i zgodność

- Nowa strona ćwiczenia korzysta z kontraktów treningowych i istniejącego klienta API.
- Skalowanie posiłku będzie małą, niezależną funkcją aplikacyjną używaną przez oba formularze.
- Wielozdjęciowa analiza posiłku rozszerzy kontrakt serwera i implementacje Gemini oraz API zgodnego z OpenAI o listę obrazów.
- Zdjęcia progresu nie wymagają migracji bazy danych.
- Obecne dane, zapisane plany, posiłki i zdjęcia pozostają zgodne.
- Klucze modeli pozostają wyłącznie po stronie serwera.

## 6. Testowanie i kryteria akceptacji

Implementacja będzie prowadzona test-first.

Testy automatyczne obejmą:

- poprawne proporcjonalne skalowanie składników;
- odrzucenie zera, pustego dania i dania o zerowej kaloryczności;
- dostęp do szczegółów ćwiczenia globalnego i własnego oraz brak dostępu do cudzego;
- przyjęcie wielu zdjęć posiłku, limit liczby i walidację rozmiaru;
- zapis partii zdjęć progresu z częściowym błędem;
- obecność pozostałego makro i przekroczeń w podsumowaniu asystenta;
- zachowanie potwierdzenia przed zapisaniem szkicu.

Weryfikacja ręczna na szerokości telefonu i komputera potwierdzi:

- pionową listę dni bez pustych kolumn;
- przejście z ćwiczenia do rozpiski i z powrotem;
- edycję zapisanego posiłku oraz propozycji AI do zadanej liczby kalorii;
- brak białego pustego obszaru pod propozycją;
- wybór wielu zdjęć z galerii w obu modułach;
- konkretną odpowiedź asystenta dla brakującego makro.

Na końcu zostaną wykonane wymagane polecenia:

```powershell
dotnet build FormaAI.sln
dotnet test FormaAI.sln --no-build
```
