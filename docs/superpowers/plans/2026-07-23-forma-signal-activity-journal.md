# Forma Signal Activity Journal Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Uporządkować trening, profil, dodawanie jedzenia i progres jako spójny, responsywny dziennik aktywności.

**Architecture:** Zmiany pozostają w istniejących stronach Blazor i w arkuszu rozszerzającym Forma Signal. Logika API i modele domenowe nie zmieniają się; dokładamy jedynie lokalny stan prezentacji wyboru sekcji posiłku i zaznaczonego dnia.

**Tech Stack:** .NET 8, Blazor WebAssembly, MudBlazor, CSS.

## Global Constraints

- Nie zmieniać logiki biznesowej ani kontraktów API.
- Projektować mobile-first i zachować obsługę jasnego oraz ciemnego motywu.
- Nie dodawać zależności, gradientów dekoracyjnych ani zbędnych animacji.
- Każdy większy moduł zakończyć polskim commitem.

---

### Task 1: Plan tygodnia

**Files:**
- Modify: `src/FormaAI.Web/Pages/Training.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/forma-signal.css`

- [ ] Nadać planowi semantyczny nagłówek i spójną siatkę dni.
- [ ] Ustawić układ dni 3/2/1 kolumna zależnie od szerokości.
- [ ] Sprawdzić rozwinięcie ćwiczeń i długie nazwy.
- [ ] Zbudować rozwiązanie i zapisać osobny commit.

### Task 2: Profil i strefa zagrożenia

**Files:**
- Modify: `src/FormaAI.Web/Pages/Profile.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/forma-signal.css`

- [ ] Zbudować responsywny układ ustawienia + konto.
- [ ] Wydzielić czerwony Danger zone wraz z formularzem potwierdzenia.
- [ ] Sprawdzić kolejność klawiatury i zachowanie na telefonie.
- [ ] Zbudować rozwiązanie i zapisać osobny commit.

### Task 3: Wybór posiłku i katalog

**Files:**
- Modify: `src/FormaAI.Web/Pages/Home.razor`
- Modify: `src/FormaAI.Web/Pages/Food.razor`
- Modify: `src/FormaAI.Web/Pages/AddMeal.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/forma-signal.css`

- [ ] Pokazać chooser sekcji przy wejściu bez parametru `slot`.
- [ ] Zachować szybkie wejście z preselektowanym slotem.
- [ ] Rozdzielić gotowe dania i pojedyncze produkty nagłówkami oraz stylem.
- [ ] Ustabilizować układ listy posiłków na szerokim i wąskim ekranie.
- [ ] Zbudować rozwiązanie i zapisać osobny commit.

### Task 4: Kalendarz progresu

**Files:**
- Modify: `src/FormaAI.Web/Pages/Progress.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/forma-signal.css`

- [ ] Zastąpić niejednoznaczne zakładki i zakresy opisanymi segmentami.
- [ ] Zmienić koła dni na kafelki z dwoma niezależnymi sygnałami.
- [ ] Dodać wyraźny stan dzisiaj, selected i kompaktowy inspektor dnia.
- [ ] Usunąć biały pusty panel wyniku okresu.
- [ ] Zbudować rozwiązanie i zapisać osobny commit.

### Task 5: Weryfikacja i integracja

**Files:**
- Verify: `FormaAI.sln`

- [ ] Uruchomić `dotnet build FormaAI.sln`.
- [ ] Uruchomić `dotnet test FormaAI.sln --no-build`.
- [ ] Sprawdzić kontrast, focus, overflow i breakpointy w kodzie oraz działającym środowisku.
- [ ] Scalić `redesign/forma-signal` do `main` bez dołączania niezwiązanych plików użytkownika.
- [ ] Uruchomić aplikację po scaleniu i sprawdzić publiczny Quick Tunnel.
