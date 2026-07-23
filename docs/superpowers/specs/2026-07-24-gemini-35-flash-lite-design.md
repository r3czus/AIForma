# Gemini 3.5 Flash-Lite

## Cel

Ustawić `gemini-3.5-flash-lite` jako domyślny model Google Gemini w całej
aplikacji FormaAI, pozostawiając klucz API do ręcznego wklejenia w panelu
administratora.

## Zakres

- Jeden współdzielony identyfikator modelu jest używany przez serwer, panel
  administratora i odpowiedź domyślnej konfiguracji.
- Dla `gemini-3.5-flash-lite` klient wysyła poziom myślenia `minimal`.
- Istniejący lokalny rekord konfiguracji Gemini zostaje przełączony na nowy
  model bez odczytywania, zmieniania ani ujawniania zaszyfrowanego klucza.
- Panel nadal umożliwia ręczną zmianę modelu i wklejenie nowego klucza.
- Aplikacja zostaje zbudowana, przetestowana, skomitowana, wypchnięta na
  `main` i ponownie wystawiona przez Cloudflare Quick Tunnel.

## Ograniczenia

- Nie wykonywać żadnego żądania do API Gemini.
- Nie używać przycisku `Sprawdź zapisane połączenie`.
- Nie umieszczać klucza API w kodzie, logach, dokumentacji ani historii Git.
- Weryfikacja wdrożenia obejmuje wyłącznie lokalny i publiczny endpoint
  `/health/live`.
