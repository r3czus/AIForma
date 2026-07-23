# Forma Signal Craft Pass Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Dopracować cały interfejs FormaAI jako spójny, szybki i dostępny system Forma Signal bez zmiany zachowania aplikacji.

**Architecture:** Istniejący `app.css` pozostaje warstwą kompatybilności dla obecnego markupu, a `forma-signal.css` staje się jednoznaczną warstwą systemu i usuwa widoczne niespójności. Powtarzalny ekran ładowania zostanie wydzielony do małego komponentu Razor, a shell i powierzchnie modułów będą korzystać ze wspólnych tokenów ruchu, geometrii i stanu.

**Tech Stack:** .NET 8, Blazor WebAssembly, MudBlazor, CSS.

## Global Constraints

- Nie zmieniać modeli domenowych, kontraktów API, tras ani zapisu danych.
- Zachować potwierdzanie propozycji AI.
- Zachować etykiety głównej nawigacji oraz znak `FORMA/AI`.
- Telefon jest podstawowym środowiskiem.
- WCAG 2.1 AA, cel dotykowy minimum 44 x 44 px, tekst minimum 12 px.
- Nie dodawać zależności frontendowych.
- Każdy większy zamknięty moduł kończy się polskim commitem.

---

### Task 1: Wspólny szkielet ładowania

**Files:**
- Create: `src/FormaAI.Web/Components/PageSkeleton.razor`
- Modify: `src/FormaAI.Web/Pages/Home.razor`
- Modify: `src/FormaAI.Web/Pages/Food.razor`
- Modify: `src/FormaAI.Web/Pages/Training.razor`
- Modify: `src/FormaAI.Web/Pages/Progress.razor`
- Modify: `src/FormaAI.Web/Pages/Profile.razor`
- Modify: `src/FormaAI.Web/Pages/ProfileSettings.razor`
- Modify: `src/FormaAI.Web/Pages/Assistant.razor`
- Modify: `src/FormaAI.Web/Pages/AddMeal.razor`
- Modify: `src/FormaAI.Web/Pages/Pantry.razor`
- Modify: `src/FormaAI.Web/Pages/WeeklyReview.razor`
- Modify: `src/FormaAI.Web/Pages/Workout.razor`

**Interfaces:**
- Produces: `<PageSkeleton Message="..." Compact="true|false" />`.
- Consumes: wyłącznie parametry prezentacyjne `string Message` i `bool Compact`.

- [ ] **Step 1: Utworzyć semantyczny komponent**

Komponent renderuje `role="status"`, tekst dla czytnika ekranu oraz bloki odpowiadające nagłówkowi i dwóm sekcjom treści. Nie zawiera logiki ani zależności od API.

- [ ] **Step 2: Zastąpić tylko główne loadery**

Zastąpić bloki `.page-loader` nowym komponentem. Pozostawić małe spinnery wyszukiwania, zapisu i operacji lokalnych.

- [ ] **Step 3: Zweryfikować kompilację**

Run: `dotnet build FormaAI.sln`

Expected: exit 0 i brak błędów Razor.

- [ ] **Step 4: Commit**

Run: `git add src/FormaAI.Web/Components/PageSkeleton.razor src/FormaAI.Web/Pages`

Run: `git commit -m "Ujednolić stany ładowania aplikacji"`

### Task 2: Fundamenty ruchu, typografii i dostępności

**Files:**
- Modify: `src/FormaAI.Web/wwwroot/css/forma-signal.css`

**Interfaces:**
- Produces: tokeny `--ease-out`, `--ease-standard`, `--duration-press`, `--duration-ui`.
- Consumes: istniejące tokeny kolorów i geometrii z `DESIGN.md`.

- [ ] **Step 1: Dodać wspólne tokeny ruchu**

Użyć `cubic-bezier(.23, 1, .32, 1)` dla wejścia i reakcji oraz 140 ms dla nacisku. Animować wyłącznie `transform`, `opacity`, kolor, tło i obramowanie.

- [ ] **Step 2: Dodać reakcję elementów naciskalnych**

Przyciski, elementy nawigacji i własne kontrolki otrzymują `scale(.98)` w `:active`. Hover jest aktywny tylko pod `@media (hover: hover) and (pointer: fine)`.

- [ ] **Step 3: Domknąć minimalne rozmiary**

Nadpisać pozostałe selektory z wartościami poniżej `.75rem`, zachowując hierarchię przez kolor i wagę zamiast mikrotekstu.

- [ ] **Step 4: Uzupełnić reduced motion**

Przy `prefers-reduced-motion: reduce` usunąć przesunięcia, skalowanie, shimmer i nieskończone animacje; nie usuwać focusu ani zmian semantycznego koloru.

- [ ] **Step 5: Uruchomić detektor po zakończeniu wszystkich zmian UI**

Detektor ma zostać uruchomiony dokładnie raz w Task 5, nie na tym etapie.

### Task 3: Shell mobilny i desktopowy

**Files:**
- Modify: `src/FormaAI.Web/Layout/MainLayout.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/forma-signal.css`

**Interfaces:**
- Consumes: istniejące trasy `/`, `/food`, `/progress`, `/training`, `/profile`, `/assistant`.
- Produces: niezmienione linki i etykiety, nowy wygląd aktywnego stanu i responsywnej szyny.

- [ ] **Step 1: Uporządkować semantykę shellu**

Dodać klasy odpowiedzialne za ikonę i etykietę bez zmiany treści linków. Zachować `aria-label` głównej nawigacji.

- [ ] **Step 2: Dopracować trzy zakresy**

Telefon `<768`: dolna nawigacja. Tablet `768-1199`: zwarta boczna szyna. Desktop `>=1200`: szersza szyna z poziomymi pozycjami i stabilnym odstępem treści.

- [ ] **Step 3: Ujednolicić top bar**

Top bar ma 64-68 px, czytelny znak marki, spokojny link Asystenta i brak ciężkiego cienia.

- [ ] **Step 4: Zweryfikować kompilację i commit**

Run: `dotnet build FormaAI.sln`

Expected: exit 0.

Run: `git add src/FormaAI.Web/Layout/MainLayout.razor src/FormaAI.Web/wwwroot/css/forma-signal.css`

Run: `git commit -m "Dopracować responsywny shell FormaAI"`

### Task 4: Powierzchnie operacyjne

**Files:**
- Modify: `src/FormaAI.Web/wwwroot/css/forma-signal.css`
- Modify only when hierarchy requires markup: `src/FormaAI.Web/Pages/Home.razor`
- Modify only when hierarchy requires markup: `src/FormaAI.Web/Pages/Food.razor`
- Modify only when hierarchy requires markup: `src/FormaAI.Web/Pages/Training.razor`
- Modify only when hierarchy requires markup: `src/FormaAI.Web/Pages/Progress.razor`
- Modify only when hierarchy requires markup: `src/FormaAI.Web/Pages/Profile.razor`
- Modify only when hierarchy requires markup: `src/FormaAI.Web/Pages/Assistant.razor`

**Interfaces:**
- Consumes: istniejące klasy modułów i komponenty MudBlazor.
- Produces: spójny rytm sekcji, wierszy, nagłówków, metryk, pustych stanów i zatwierdzania AI.

- [ ] **Step 1: Dzisiaj**

Utrzymać kolejność nagłówek, szybkie akcje, Sygnał dnia, trening i żywienie. Wyraźnie odróżnić akcję główną od tekstowych skrótów.

- [ ] **Step 2: Jedzenie i Trening**

Wzmocnić nazwy pozycji, podnieść czytelność metadanych do minimum 12 px i ograniczyć kapsułki do prawdziwych statusów.

- [ ] **Step 3: Progres**

Utrzymać liczby jako pierwszy poziom, a wykresy i kalendarz jako drugi. Zapewnić czytelne filtry i brak przewijania całej strony przy 320 px.

- [ ] **Step 4: Profil i Asystent**

Wyrównać stany wyboru, pola i akcje zatwierdzania. Zachować spokojne, jednoznaczne rozróżnienie użytkownika, propozycji AI i wyniku zapisu.

### Task 5: Audyt końcowy i weryfikacja

**Files:**
- Modify only when findings require it: changed Razor and CSS files.

**Interfaces:**
- Consumes: wszystkie zmienione pliki UI.
- Produces: build i testy bez błędów oraz raport detektora bez nierozwiązanych usterek.

- [ ] **Step 1: Uruchomić mechaniczny detektor dokładnie raz**

Run: `node C:\Users\Jannu\.codex\skills\impeccable\scripts\detect.mjs --json src/FormaAI.Web/Layout/MainLayout.razor src/FormaAI.Web/Components/PageSkeleton.razor src/FormaAI.Web/wwwroot/css/forma-signal.css`

Expected: JSON bez nierozwiązanych usterek albo konkretna lista poprawek do wdrożenia.

- [ ] **Step 2: Uruchomić pełny build**

Run: `dotnet build FormaAI.sln`

Expected: exit 0.

- [ ] **Step 3: Uruchomić pełne testy**

Run: `dotnet test FormaAI.sln --no-build`

Expected: exit 0 i 0 nieudanych testów.

- [ ] **Step 4: Sprawdzić zakres zmian**

Run: `git diff --check`

Run: `git status --short`

Expected: brak błędów białych znaków i brak zmian w `App_Data` oraz niepowiązanych dokumentach użytkownika.

- [ ] **Step 5: Commit końcowego craft passu**

Run: `git add` tylko dla plików wskazanych w tym planie.

Run: `git commit -m "Dopracować interakcje i czytelność Forma Signal"`
