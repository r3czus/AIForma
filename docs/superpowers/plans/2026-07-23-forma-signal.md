# Forma Signal Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wdrożyć kompletny, responsywny system wizualny Forma Signal bez zmiany logiki biznesowej aplikacji.

**Architecture:** Warstwa wizualna zostanie wydzielona do arkusza `forma-signal.css`, ładowanego po istniejącym `app.css`, co ograniczy ryzyko zmian działania i pozwoli przeprowadzić spójny redesign wszystkich ekranów. Niewielkie zmiany Razor dodadzą jasną hierarchię i skróty do istniejących ścieżek ręcznych oraz AI.

**Tech Stack:** .NET 8, Blazor WebAssembly, MudBlazor, CSS, Cloudflare Quick Tunnel.

## Global Constraints

- Nie zmieniać modeli domenowych, kontraktów API ani zapisu danych.
- Zachować potwierdzanie propozycji AI.
- Telefon jest podstawowym środowiskiem.
- WCAG 2.1 AA, cel dotykowy minimum 44 × 44 px, tekst minimum 12 px.
- Nie dodawać zależności frontendowych.

---

### Task 1: Fundamenty i shell

**Files:**
- Create: `src/FormaAI.Web/wwwroot/css/forma-signal.css`
- Modify: `src/FormaAI.Web/wwwroot/index.html`
- Modify: `src/FormaAI.Web/Layout/MainLayout.razor`

- [ ] Dodać tokeny kolorów, typografii, odstępów, promieni, cieni i focusu zgodne z `DESIGN.md`.
- [ ] Podłączyć arkusz po `app.css`.
- [ ] Uporządkować pasek górny oraz nawigację dla telefonu, tabletu i desktopu.
- [ ] Uruchomić `dotnet build FormaAI.sln` i oczekiwać kodu 0.
- [ ] Zatwierdzić osobnym polskim commitem.

### Task 2: Dzisiaj i szybkie działania

**Files:**
- Modify: `src/FormaAI.Web/Pages/Home.razor`
- Modify: `src/FormaAI.Web/Components/MacroSummary.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/forma-signal.css`

- [ ] Dodać widoczny blok szybkiego dodawania jedzenia, ręcznego rozpoczęcia treningu i pomocy AI, używając istniejących tras.
- [ ] Przekształcić rekomendację dnia w „sygnał dnia” z czytelnym następnym krokiem.
- [ ] Spłaszczyć bilans energii i makro bez zagnieżdżonych dekoracyjnych kart.
- [ ] Uruchomić build i zatwierdzić osobnym polskim commitem.

### Task 3: Jedzenie i trening

**Files:**
- Modify: `src/FormaAI.Web/Pages/Food.razor`
- Modify: `src/FormaAI.Web/Pages/AddMeal.razor`
- Modify: `src/FormaAI.Web/Pages/Training.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/forma-signal.css`

- [ ] Dodać pary akcji „ręcznie / z AI” w nagłówkach modułów.
- [ ] Uporządkować kalendarz jedzenia, sekcje posiłków, katalog produktów, plany i kreator treningu.
- [ ] Zapewnić pełną szerokość kluczowych akcji na telefonie i cel dotykowy 44 px.
- [ ] Uruchomić build i zatwierdzić osobnym polskim commitem.

### Task 4: Progres, Profil i Asystent

**Files:**
- Modify: `src/FormaAI.Web/Pages/Progress.razor`
- Modify: `src/FormaAI.Web/Pages/Profile.razor`
- Modify: `src/FormaAI.Web/Pages/Assistant.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/forma-signal.css`

- [ ] Nadać filtrom okresu i podsumowaniom jasną kolejność.
- [ ] Uprościć geometrię kreatora celu i ustawień.
- [ ] Wyróżnić w Asystencie etap propozycji, uzasadnienia i zatwierdzenia bez futurystycznych efektów.
- [ ] Uruchomić build i zatwierdzić osobnym polskim commitem.

### Task 5: Audyt i udostępnienie

**Files:**
- Modify only when findings require a correction: `src/FormaAI.Web/wwwroot/css/forma-signal.css` and affected Razor files.

- [ ] Uruchomić detektor Impeccable nad `src/FormaAI.Web`.
- [ ] Sprawdzić kontrasty, focus, minimalne rozmiary i układ 320–1440 px.
- [ ] Uruchomić `dotnet build FormaAI.sln`.
- [ ] Uruchomić `dotnet test FormaAI.sln --no-build`.
- [ ] Uruchomić LocalDB, aplikację na `0.0.0.0:5082` i sprawdzić `/health/live`.
- [ ] Uruchomić `cloudflared tunnel --url http://127.0.0.1:5082 --no-autoupdate`, sprawdzić publiczny adres HTTPS i przekazać go użytkownikowi.
