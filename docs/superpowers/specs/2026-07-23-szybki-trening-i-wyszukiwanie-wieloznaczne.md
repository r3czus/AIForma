# Szybki trening i wyszukiwanie wieloznaczne

## Cel

Ułatwić dodawanie jedzenia i treningu na telefonie bez zmiany istniejącego
kierunku wizualnego FormaAI.

## Zachowanie

- Szczegóły porcji produktu otwierają się od górnej części ekranu. Na telefonie
  nadal zajmują cały ekran i zaczynają się pod bezpiecznym obszarem systemowym.
- Nazwa produktu w wynikach ma czytelny rozmiar i może zająć maksymalnie dwa
  wiersze. Marka i wartości odżywcze pozostają wizualnie drugorzędne.
- Wyszukiwanie produktów i ćwiczeń obsługuje:
  - `fraza` oraz `*fraza*` — zawiera frazę,
  - `fraza*` — zaczyna się od frazy,
  - `*fraza` — kończy się frazą.
- Gwiazdka sama w sobie nie filtruje listy. Wielkość liter nie ma znaczenia
  zgodnie z porównywaniem używanym przez SQL Server.
- Na stronie `Dzisiaj` akcja `Wpisz trening` otwiera formularz osadzony w karcie,
  a nie modal. Użytkownik wybiera czas, wyszukuje ćwiczenia, dodaje je, określa
  liczbę serii i rozpoczyna trening na dziś bez tworzenia planu.
- Szybki trening używa domyślnie zakresu 8–12 powtórzeń, 2 RIR i 90 sekund
  przerwy. Czas jest zapisywany jako limit sesji.
- Nie można rozpocząć drugiej sesji, gdy istnieje aktywny trening.
- Plan `Góra/Dół 4 dni · rekompozycja` ma dni w poniedziałek, wtorek, czwartek
  i sobotę, korzysta z istniejącego katalogu ćwiczeń i po utworzeniu jest
  aktywnym planem konta demonstracyjnego.

## Ograniczenia

- Bez wywołań API AI.
- Bez zmian schematu bazy danych.
- Zachować istniejący styl Forma Signal, cele dostępności i minimalny obszar
  dotykowy 44×44 px.
- Zmiany muszą przejść `dotnet build FormaAI.sln` oraz
  `dotnet test FormaAI.sln --no-build`.
