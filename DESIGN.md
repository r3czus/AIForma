# Forma Signal Design System

<!-- impeccable:design-schema 1 -->

## Direction

Forma Signal to motywujący panel codziennego działania. Łączy precyzję narzędzia treningowego z redakcyjną hierarchią danych. Technologiczny charakter wynika z czytelnych zależności, rytmu i rekomendacji, a nie z neonów, poświat ani dekoracyjnych gradientów.

## Mode

Operate. Szybkość wykonania zadania, czytelność stanu i znajome zachowanie kontrolek mają pierwszeństwo przed ekspresją.

## Color Strategy

Pełna paleta z jasno przypisanymi rolami:

- `--canvas: #f4f6f3` — mineralne tło aplikacji.
- `--surface: #ffffff` — powierzchnie robocze.
- `--surface-soft: #eef2ee` — spokojne grupowanie bez kolejnej karty.
- `--ink: #17211c` — tekst i główne dane.
- `--muted: #596760` — opisy pomocnicze.
- `--action: #3451d1` — działanie, aktywny stan i sygnał AI.
- `--fuel: #b9562c` — energia i żywienie.
- `--recovery: #287454` — wykonanie i pozytywny stan.
- `--danger: #b93b35` — błąd i działanie destrukcyjne.

Kolory akcentowe nie są używane jako drobny tekst na jasnym tle bez ciemniejszego wariantu zapewniającego kontrast.

## Typography

- Barlow Semi Condensed: nagłówki i krótkie komunikaty motywacyjne.
- Onest: interfejs, formularze i tekst ciągły.
- IBM Plex Mono: wartości, czas, makro i krótkie etykiety techniczne.

Skala ma być zwarta: nagłówek strony 34–52 px zależnie od szerokości, nagłówek sekcji 24–32 px, tekst podstawowy minimum 15 px, etykiety i podpisy minimum 12 px.

## Geometry and Elevation

- Kontrolki: 10 px.
- Główne powierzchnie: 14–16 px.
- Dialogi i panele pełnoekranowe: maksymalnie 20 px.
- Kapsułki tylko dla krótkich statusów i filtrów.
- Cień tylko dla elementów rzeczywiście unoszących się; grupowanie treści opiera się przede wszystkim na przestrzeni i liniach.
- Grube kolorowe paski z boku kart są zabronione.

## Layout

- Telefon: dolna nawigacja, kompaktowy nagłówek, podstawowe akcje w strefie kciuka.
- Tablet: boczna kompaktowa nawigacja od 768 px i szersza przestrzeń treści.
- Desktop: stabilna szyna nawigacyjna i maksymalna szerokość treści dostosowana do typu ekranu.
- Najważniejsza akcja ma poprzedzać zestaw szczegółów.
- Sekcje danych używają wierszy i separatorów zamiast stosu zagnieżdżonych kart.

## Signature

„Sygnał dnia” to cienki tor postępu i zestaw krótkich, konkretnych informacji łączących jedzenie, trening i regularność. Pokazuje stan i następne działanie bez osobnego systemu punktów.

## Components

- Przyciski podstawowe są pełne i spokojne, bez ciężkiego cienia.
- Przyciski drugorzędne mają subtelne obramowanie; akcje tekstowe nie udają osobnych kart.
- Pola formularzy mają wyraźne etykiety, stan focus i wysokość minimum 48 px.
- Karty są zarezerwowane dla samodzielnych modułów; listy produktów, ćwiczeń i ustawień są rytmicznymi wierszami.
- Stany wyboru łączą kolor z obramowaniem, ikoną albo tekstem.
- AI używa znaku `/AI`, krótkiego wyjaśnienia i jawnego etapu zatwierdzenia.

## Motion

Jeden krótki ruch wejścia strony oraz szybkie potwierdzenia wykonanej akcji. Brak animacji dekoracyjnych. `prefers-reduced-motion` wyłącza przejścia.

## Responsive and Accessibility Rules

- Cel dotykowy minimum 44 × 44 px.
- Brak tekstu mniejszego niż 12 px.
- Brak poziomego przewijania całej strony przy 320 px.
- Widoczny `:focus-visible` na każdym interaktywnym elemencie.
- Tekst podstawowy spełnia kontrast 4.5:1, duże etykiety i elementy graficzne co najmniej 3:1.
- Znaczenie nie może być przekazywane wyłącznie kolorem.
