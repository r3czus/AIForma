# Kierunek interfejsu treningowego

Referencje produkcyjne znajdują się w `docs/design/training-references/`. Są north starem kompozycji, a nie bitmapami do odtworzenia 1:1. Tekst, kontrolki, responsywność i stany pozostają natywnymi komponentami Blazor/MudBlazor.

## Elementy wspólne

- Zostaje jasna, ciepła baza FormaAI, ciemny atrament, kobaltowy akcent i delikatne miętowe linie.
- Hierarchię budują rytm, typografia i listy. Karty są używane tylko dla wyraźnych grup funkcjonalnych.
- Najważniejsze akcje mają minimum 44 px wysokości. Wartości serii są wygodne do edycji kciukiem.
- Ruch jest funkcjonalny: przejście między dniami, rozwinięcie listy i timer. Bez dekoracyjnych animacji wejścia.

## Plan

- Pasek dni pokazuje trzy równe kolumny; kolejne dni są dostępne przez poziome przewinięcie.
- Wybrany dzień nie rozwija ćwiczeń w swojej kolumnie. Jego ćwiczenia tworzą jedną pełnoszeroką listę pod całym paskiem.
- Każdy wiersz prowadzi do szczegółów ćwiczenia i pokazuje nazwę, zakres serii/powtórzeń, odpoczynek oraz mały podgląd ruchu.
- Superseria ma jeden wspólny znacznik i czytelne pozycje A1, A2, bez dodatkowych zagnieżdżonych kart.

## Aktywna sesja

- Media ćwiczenia są pierwszym elementem treści. Brak pliku daje kontrolowany placeholder, a nie pustą białą przestrzeń.
- Timer przerwy i interwał są blisko tytułu. Tabela serii zajmuje centralną część ekranu.
- Superseria pokazuje kolejność, aktywne ćwiczenie i numer rundy.
- Wymiana jest częścią workflow sesji i nie usuwa wykonanych już serii.

## Wymiana

- Kontekst aktualnego ćwiczenia pozostaje widoczny.
- Zamienniki są listą z wyszukiwaniem i metadanymi, nie galerią kart.
- Wybór wymaga jawnego zatwierdzenia; bieżące ćwiczenie jest oznaczone i nieaktywne.

## Szczegóły

- Górny podgląd GIF/obrazu zachowuje proporcje 16:9.
- Zakładki `Historia`, `Wykres`, `Technika` rozdzielają trzy różne zadania.
- Historia grupuje serie według dat i wyróżnia najlepsze 1RM kolorem akcentu.
- Nie literalizujemy fotograficznych kadrów z makiet. Aplikacja korzysta z istniejących adresów mediów ćwiczeń i poprawnego stanu zastępczego.

