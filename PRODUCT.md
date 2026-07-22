# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

Głównym użytkownikiem jest właściciel aplikacji, który korzysta z niej przede wszystkim na telefonie. Chce regularnie zapisywać jedzenie, treningi i pomiary bez poświęcania czasu na obsługę narzędzia.

## Product Purpose

FormaAI łączy dziennik jedzenia, prowadzenie treningów i obserwowanie postępów. Sukces oznacza, że użytkownik szybko rozumie stan dnia, wie, jaki jest kolejny krok, i może zapisać działanie ręcznie albo z pomocą AI.

## Positioning

FormaAI zestawia dane żywieniowe i treningowe w jednym codziennym obrazie oraz wykorzystuje asystenta do przygotowania propozycji, które użytkownik zatwierdza przed zapisaniem.

## Operating Context

Aplikacja jest używana wielokrotnie w ciągu dnia, często jedną ręką na telefonie: podczas dodawania produktu, przed treningiem, pomiędzy seriami i podczas szybkiego sprawdzania postępu. Musi również pozostać wygodna na tablecie i komputerze.

## Capabilities and Constraints

- .NET 8, Blazor WebAssembly, MudBlazor i ASP.NET Core API.
- Jedzenie i treningi można dodawać ręcznie albo przy pomocy AI.
- Zmiany przygotowane przez AI wymagają zatwierdzenia użytkownika.
- Logika biznesowa, kontrakty API i sposób zapisu danych pozostają bez zmian podczas redesignu.
- Klucze API pozostają wyłącznie po stronie serwera lub w szyfrowanej konfiguracji administratora.
- Aplikacja jest responsywną PWA.

## Brand Commitments

- Nazwa i znak `FORMA/AI` pozostają rozpoznawalnym elementem produktu.
- Produkt ma motywować do pracy, ale nie stosować infantylnej grywalizacji.
- Interfejs ma być nowoczesny i związany z AI, lecz nie przesadnie futurystyczny.
- Należy unikać przypadkowych gradientów, nadmiaru kart, ogromnych zaokrągleń i zbędnych animacji.

## Evidence on Hand

Repozytorium zawiera kompletne działające przepływy, dane demonstracyjne, ikonę PWA i polską treść interfejsu. Nie ma materiałów fotograficznych ani komercyjnych twierdzeń, których należałoby użyć w designie.

## Product Principles

1. Następne działanie ma być oczywiste bez szukania.
2. Ręczne dodawanie i pomoc AI są równorzędnymi, łatwo dostępnymi ścieżkami.
3. Najważniejszy wynik jest widoczny przed szczegółami.
4. Motywacja wynika z realnego postępu i konkretnego języka.
5. Telefon jest podstawowym środowiskiem użytkowania.

## Accessibility & Inclusion

Interfejs ma spełniać WCAG 2.1 AA, działać z klawiaturą, zachowywać widoczny focus, respektować ograniczenie ruchu i utrzymywać dotykowe cele o wymiarze co najmniej 44 × 44 px.
