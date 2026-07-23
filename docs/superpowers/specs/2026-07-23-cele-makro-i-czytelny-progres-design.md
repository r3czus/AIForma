# Cele, makro i czytelny progres

## Cel

Poprawić przepływ ustawiania celu żywieniowego oraz prezentację diety i treningu bez zmiany kierunku wizualnego Forma Signal. Wyniki muszą reagować na wpisywane dane, zachowywać właściwe daty i przedstawiać postęp względem dni, w których użytkownik faktycznie mógł prowadzić dziennik.

## Zakres

Zmiana obejmuje:

- kreator celu w profilu;
- wyliczanie i zapis celu kalorycznego oraz makroskładników;
- podsumowania diety i treningu na ekranie progresu;
- zaznaczenie dnia w kalendarzu progresu;
- sekcję „Dodaj ponownie” podczas dodawania posiłku;
- akcję „Zaproponuj posiłek” na ekranie dzisiejszego planu.

Nie obejmuje przebudowy całego systemu wizualnego, zmiany modeli treningowych ani automatycznego zapisywania propozycji asystenta.

## Kreator celu masy

Pole „Masa docelowa” nadal przyjmuje końcową masę ciała, a nie liczbę kilogramów do zmiany.

Podpowiedź ma aktualizować się podczas wpisywania, bez konieczności przechodzenia do następnego kroku:

- przy masie aktualnej 80 kg i docelowej 75 kg: „Do zrzucenia 5 kg”;
- przy masie aktualnej 75 kg i docelowej 80 kg: „Do przybrania 5 kg”;
- przy tej samej wartości: „Cel zakłada utrzymanie obecnej masy”.

Niepełna albo nieprawidłowa wartość nie wyświetla mylącego wyniku. Dotychczasowa walidacja kierunku celu pozostaje: redukcja wymaga niższej masy docelowej, a budowanie masy — wyższej.

Po zapisaniu kreator pozostaje na ekranie wyniku i pokazuje wartości faktycznie zwrócone przez serwer: kalorie, białko, tłuszcze, węglowodany, datę rozpoczęcia celu oraz przewidywaną datę osiągnięcia celu.

## Wyliczanie makroskładników

Algorytm nie może zwrócić `0 g` węglowodanów dla poprawnie wypełnionego profilu.

Wyliczenie zachowuje obecne wartości bazowe: białko `2 g/kg` i tłuszcze `0,9 g/kg`. Co najmniej 20% końcowej energii zostaje przeznaczone na węglowodany. Minimalna wartość kalorii potrzebna do zachowania tej proporcji jest liczona jako `(kalorie z białka + kalorie z tłuszczu) / 0,8`. Jeżeli wynik BMR, aktywności i celu masy jest niższy, kalorie są podnoszone do tej wartości. Węglowodany wypełniają pozostałą energię. Dzięki temu zadany deficyt nie może wyzerować węglowodanów.

Reguły obliczeń mają zostać wydzielone do testowalnej logiki aplikacyjnej. Testy obejmą co najmniej redukcję, utrzymanie, budowanie masy oraz przypadek, który wcześniej dawał `0 g` węglowodanów.

Użytkownik nadal może ręcznie skorygować wynik przed zapisem. Wszystkie makroskładniki muszą być nieujemne, a automatycznie wyliczony zestaw musi zawierać więcej niż `0 g` węglowodanów.

## Data obowiązywania celu

Data rozpoczęcia celu jest ustalana według strefy czasowej zapisanej w profilu użytkownika, a nie według lokalnej daty przeglądarki lub UTC.

Po zapisie:

- nowy cel obowiązuje od tej daty w dzienniku jedzenia;
- ekran profilu odświeża zapisany wynik;
- dziennik i progres pobierają cel właściwy dla oglądanego dnia;
- dni sprzed pierwszego obowiązującego celu nie są traktowane jako dni, w których użytkownik mógł realizować ten cel.

## Dni kwalifikujące się i procenty

„Dzień kwalifikujący się” to dzień spełniający wszystkie warunki:

1. mieści się w wybranym okresie;
2. nie leży w przyszłości według strefy czasowej użytkownika;
3. istnieje dla niego obowiązujący cel żywieniowy.

Każdy kwalifikujący się dzień wchodzi do mianownika wyniku diety. Brak wpisu w takim dniu obniża wynik. Dni przyszłe i dni sprzed pierwszego celu nie są liczone.

Przykład: jeśli w miesiącu upłynęło 18 kwalifikujących się dni, a jeden dzień był w celu, interfejs pokazuje `1 / 18` i pasek o szerokości około `5,6%`.

Licznik „zapisane dni” pozostaje osobną informacją i nie może być używany jako mianownik procentu realizacji celu.

Dla treningu bieżący okres jest również obcinany do dzisiaj. Planowane sesje z przyszłej części miesiąca nie obniżają jeszcze wyniku. Okresy całkowicie historyczne są liczone w całości.

Obliczenia procentów powstają po stronie API, aby ekran „Na dziś”, zakładki „Dieta” i „Trening” oraz przyszłe podsumowania używały tej samej reguły.

## Czytelność progresu i kalendarza

Karty podsumowania diety i treningu zachowują obecny styl, ale wartości liczbowe otrzymują wyraźniejszy kontrast względem tła. Etykiety pomocnicze pozostają drugorzędne, lecz muszą być czytelne na jasnym i ciemnym tle sekcji.

Pasek postępu zawsze odpowiada wartości liczbowej i nie może wizualnie sugerować pełnego wyniku przy małym procencie.

Zaznaczony dzień kalendarza ma jeden spójny stan. Połączenie „dzisiaj” i „wybrany” nie może tworzyć podwójnej ani prostokątnej obwódki wokół liczby. Stan jest nadal dostępny przez `aria-pressed`, a fokus klawiatury pozostaje widoczny jako osobny stan dostępności.

## „Dodaj ponownie”

Na ekranie dodawania posiłku sekcja „Dodaj ponownie” jest początkowo zwinięta. Nagłówek pokazuje, że zapisane dania są dostępne, oraz liczbę propozycji. Użytkownik może rozwinąć i ponownie zwinąć sekcję.

Stan rozwinięcia nie zmienia wybranego posiłku, daty ani trybu dodawania. Lista „Twoje gotowe dania” w dedykowanej zakładce może pozostać widoczna, ponieważ użytkownik świadomie otworzył ten zakres katalogu.

## „Zaproponuj posiłek”

Akcja na ekranie dzisiejszego planu prowadzi do `/assistant`, a nie do formularza dodawania posiłku.

Do rozmowy przekazywany jest gotowy prompt zawierający:

- datę;
- pozostałe kalorie;
- pozostałe białko, tłuszcze i węglowodany;
- prośbę o krótką propozycję posiłku;
- informację, że nic nie może zostać zapisane bez zatwierdzenia użytkownika.

Asystent automatycznie rozpoczyna rozmowę przy użyciu istniejącego mechanizmu parametru `prompt`. Ewentualny szkic posiłku nadal wymaga zatwierdzenia.

## Obsługa błędów

- Nieprawidłowe dane w kreatorze zatrzymują przejście do kolejnego kroku i pokazują konkretny komunikat.
- Nieudany zapis nie zastępuje wartości widocznych w kreatorze wartościami pozornie zapisanymi.
- Brak celu dla okresu pokazuje stan „brak danych do oceny”, nie `0%`.
- Brak zaplanowanych treningów pokazuje neutralny stan, nie pełny ani zerowy pasek realizacji.

## Weryfikacja

Testy automatyczne mają potwierdzić:

- wyliczenie różnicy masy dla redukcji, budowania i utrzymania;
- brak zerowych węglowodanów po automatycznym wyliczeniu;
- właściwą datę celu dla strefy `Europe/Warsaw`;
- mianownik obejmujący brakujące, ale kwalifikujące się dni;
- wykluczenie przyszłości i dni sprzed celu;
- obcięcie bieżącego okresu treningowego do dzisiaj;
- wygenerowanie odnośnika „Zaproponuj posiłek” do rozmowy z właściwym promptem.

Po zmianach należy uruchomić:

```powershell
dotnet build FormaAI.sln
dotnet test FormaAI.sln --no-build
```

Widoki należy dodatkowo sprawdzić na szerokości telefonu i komputera: kreator celu, ekran progresu, kalendarz, zwinięte „Dodaj ponownie” oraz przejście do rozmowy z asystentem.
