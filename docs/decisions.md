# Decyzje techniczne

## 2026-07-17 — .NET 8 do czasu dostępności .NET 10

Środowisko robocze nie ma SDK .NET 10. Używamy .NET 8 LTS, czyli wariantu dopuszczonego w specyfikacji, zamiast .NET 9 o krótszym wsparciu. Przejście na .NET 10 będzie osobną, kontrolowaną aktualizacją po zainstalowaniu SDK.

## 2026-07-17 — Cookie i ochrona CSRF

Blazor oraz API są hostowane pod jednym adresem. Identity używa ciasteczka `HttpOnly` z `SameSite=Strict`, a operacje modyfikujące wymagają tokenu w nagłówku `X-CSRF-TOKEN`.
