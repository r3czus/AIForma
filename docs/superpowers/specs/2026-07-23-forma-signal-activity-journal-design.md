# Forma Signal — dziennik aktywności

## Cel

Dopracować cztery najbardziej używane obszary FormaAI bez zmiany logiki biznesowej: plany treningowe, ustawienia konta, dodawanie posiłków i kalendarz progresu. Interfejs ma szybciej odpowiadać na pytania „co robię teraz?”, „gdzie to zapisuję?” i „co oznacza ten stan?”.

## Kierunek

FormaAI zachowuje istniejący język Forma Signal: jasne płótno, granat akcji, ciepły akcent jedzenia, zielony stan celu oraz oszczędną typografię. Nowym motywem organizującym jest dziennik aktywności — plan tygodnia, oś posiłków i kalendarz powinny czytać się jak jeden zapis działań, nie zbiór niezależnych kart.

## Trening

- Każdy plan jest jednym panelem z czytelnym nagłówkiem, statusem i akcjami.
- Dni planu tworzą równą siatkę na komputerze i pionową listę na telefonie.
- Nazwa dnia, termin i liczba ćwiczeń pozostają widoczne przed rozwinięciem.
- Rozwinięcie pokazuje ćwiczenia wewnątrz tego samego kafelka, bez rozbijania layoutu.

## Profil

- Na szerokim ekranie ustawienia zajmują lewą kolumnę, a dostęp i prywatność prawą.
- Na telefonie sekcje wracają do jednego ciągu.
- Usunięcie konta jest osobną sekcją „Strefa zagrożenia” z czerwonym językiem wizualnym i jasnym opisem skutków.

## Jedzenie

- Wejście ogólnym przyciskiem „Dodaj posiłek” zaczyna się od wyboru sekcji dziennika.
- Wejście przez `+` przy śniadaniu, obiedzie itd. zachowuje szybki wariant i od razu otwiera katalog.
- Wybrana sekcja jest stale widoczna i może być zmieniona.
- Gotowe dania są wizualnie i semantycznie oddzielone od pojedynczych produktów.
- Lista posiłków otrzymuje stabilny układ: identyfikacja po lewej, wartości i akcja po prawej.

## Progres

- Zakresy danych są opisanymi segmentami: tydzień, miesiąc, kwartał, pół roku, rok.
- Główne zakładki używają neutralnego zaznaczenia; kolory kategorii pozostają wyłącznie wskaźnikami znaczenia.
- Dzień jest prostokątnym kafelkiem. Kolorowy pasek pokazuje dietę, osobny niebieski znacznik pokazuje trening.
- Dzisiaj i wybrany dzień mają niezależne, widoczne obramowania.
- Szczegóły wybranego dnia są kompaktowym inspektorem z możliwością zamknięcia.
- Wynik okresu pozostaje bezpośrednio na ciemnym tle, bez białego pustego panelu.

## Responsywność i dostępność

- Punkty kontrolne: 360, 390, 768, 1024 i 1440 px.
- Wszystkie przyciski i kafelki interaktywne mają minimum 44 px na telefonie.
- Stan nie może być komunikowany wyłącznie kolorem: wspierają go pozycja, obramowanie, tekst lub osobny znacznik.
- Zachowane zostają focus-visible, aria-label, aria-expanded i aria-pressed.

## Poza zakresem

- Zmiana kontraktów API, modeli domenowych, zapisu posiłków lub treningów.
- Nowe animacje, gradienty, dodatkowe biblioteki i przebudowa całej nawigacji.
