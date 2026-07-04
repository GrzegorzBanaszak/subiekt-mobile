# Wstępny zarys ekranów

## Cel dokumentu

Dokument opisuje wstępny podział interfejsu aplikacji Subiekt Mobile. Jest podstawą do dalszego projektowania UX i nie przesądza jeszcze o bibliotece komponentów, dokładnym układzie graficznym ani technologii wydruku.

Interfejs powinien być responsywny, ze szczególnym uwzględnieniem telefonów i tabletów używanych podczas kompletacji.

## Nawigacja główna

Minimalne sekcje:

- `Towary`,
- `Zamówienia`,
- `Kompletacja`,
- `Palety`,
- `Użytkownicy` lub `Administracja` — tylko dla uprawnionych osób.

Na telefonie nawigacja może mieć formę dolnego paska lub wysuwanego menu. Najczęstsze działania magazynowe powinny być dostępne bez wielopoziomowego przechodzenia przez menu.

## 1. Logowanie

**Cel:** identyfikacja użytkownika przed wykonaniem operacji objętych audytem.

Elementy:

- login,
- hasło lub inny przyjęty mechanizm uwierzytelniania,
- komunikat o błędzie,
- informacja o aktualnie zalogowanym użytkowniku po wejściu do aplikacji.

Po zalogowaniu użytkownik trafia do listy zamówień lub kompletacji zależnie od roli.

## 2. Lista towarów

**Cel:** przegląd katalogu pobranego z Subiekta oraz wybór towarów podczas tworzenia zamówienia.

Elementy:

- wyszukiwarka po nazwie, symbolu lub kodzie,
- paginowana lista,
- nazwa, symbol, jednostka miary i masa jednostkowa,
- oznaczenie braku masy jednostkowej,
- podgląd szczegółów towaru,
- akcja `Dodaj do zamówienia`, gdy ekran został otwarty z formularza zamówienia.

Stany interfejsu:

- ładowanie,
- brak wyników,
- brak połączenia z bazą Subiekta,
- towar z niepełnymi danymi.

## 3. Lista zamówień

**Cel:** znalezienie zamówienia i szybka ocena postępu.

Elementy:

- numer zamówienia,
- zamawiający,
- termin realizacji,
- status,
- liczba lub procent spakowanych pozycji,
- liczba utworzonych palet,
- filtry po statusie, terminie i zamawiającym,
- wyszukiwanie po numerze,
- akcja `Nowe zamówienie` widoczna dla uprawnionego użytkownika.

Zamówienia po terminie lub z bliskim terminem powinny być czytelnie wyróżnione, bez polegania wyłącznie na kolorze.

## 4. Tworzenie i edycja zamówienia

**Cel:** przygotowanie zamówienia przed przekazaniem go do kompletacji.

Sekcje formularza:

1. Dane podstawowe:
   - zamawiający,
   - termin realizacji.
2. Pozycje:
   - wybór towaru z katalogu,
   - nazwa i jednostka,
   - ilość,
   - masa jednostkowa,
   - usunięcie pozycji.
3. Podsumowanie:
   - liczba pozycji,
   - szacowana masa towarów,
   - komunikaty o brakujących danych.

Główne akcje:

- `Zapisz wersję roboczą`,
- `Udostępnij do kompletacji`,
- `Anuluj`.

Udostępnienie powinno być zablokowane, gdy brakuje zamawiającego, terminu, pozycji albo poprawnej ilości. Sposób obsługi brakującej masy jednostkowej zależy od decyzji biznesowej.

## 5. Szczegóły zamówienia

**Cel:** pełny podgląd danych i postępu zamówienia.

Elementy:

- numer, zamawiający, termin, status, autor i data utworzenia,
- postęp kompletacji,
- lista pozycji z ilością, masą i statusem,
- użytkownik kompletujący daną pozycję,
- lista palet i ich masy,
- historia najważniejszych operacji.

Akcje zależne od statusu i uprawnień:

- edycja wersji roboczej,
- rozpoczęcie lub otwarcie kompletacji,
- anulowanie zamówienia,
- przejście do paletyzacji.

## 6. Kompletacja współdzielona

**Cel:** równoległa praca wielu osób nad pozycjami jednego zamówienia.

Nagłówek zawiera:

- numer zamówienia i zamawiającego,
- termin realizacji,
- ogólny postęp,
- informację o stanie połączenia i czasie ostatniej aktualizacji.

Lista pozycji powinna rozróżniać:

- `Do kompletacji` — dostępna akcja `Podejmij`,
- `W kompletacji` — widoczna osoba realizująca; właściciel widzi `Oznacz jako spakowaną` i `Zwolnij`,
- `Spakowana` — gotowa do przypisania do palety,
- `Przypisana do palety` — widoczny numer palety.

Po naciśnięciu `Podejmij` interfejs czeka na potwierdzenie serwera. Nie może optymistycznie przedstawiać pozycji jako zarezerwowanej bez obsługi konfliktu. Jeśli inny użytkownik był pierwszy, aplikacja odświeża pozycję i pokazuje czytelny komunikat.

Widok powinien umożliwiać filtrowanie co najmniej do:

- wszystkich pozycji,
- dostępnych pozycji,
- moich pozycji,
- spakowanych pozycji.

## 7. Tworzenie palety

**Cel:** przypisanie spakowanych pozycji do konkretnej palety i obliczenie masy.

Elementy:

- numer palety,
- masa pustej palety,
- lista spakowanych pozycji nieprzypisanych jeszcze do palety,
- wybór pozycji,
- podsumowanie masy aktualizowane po każdej zmianie:
  - masa towarów,
  - masa pustej palety,
  - masa całkowita.

Główne akcje:

- `Zapisz paletę`,
- `Zamknij paletę`,
- `Anuluj`.

Zamknięcie palety powinno wymagać co najmniej jednej pozycji, poprawnej masy pustej palety i mas jednostkowych wszystkich pozycji. Serwer musi ponownie sprawdzić, czy wybrane pozycje nie zostały w międzyczasie przypisane do innej palety.

## 8. Szczegóły palety i wydruk etykiety

**Cel:** kontrola zawartości zamkniętej palety i przygotowanie etykiety.

Elementy:

- numer zamówienia i palety,
- zamawiający,
- lista pozycji z nazwą i ilością,
- masa towarów,
- masa pustej palety,
- masa całkowita,
- status palety,
- autor i czas zamknięcia.

Główne akcje:

- `Podgląd etykiety`,
- `Drukuj etykietę`,
- `Pobierz etykietę` — jeśli przyjęty format będzie to umożliwiał.

Podgląd wydruku musi pokazywać dokładnie dane wysyłane do drukarki. Ponowny wydruk powinien być odnotowany w historii.

## 9. Podgląd etykiety

**Cel:** weryfikacja danych przed wydrukiem.

Minimalna zawartość etykiety:

- numer zamówienia,
- numer palety,
- zamawiający,
- pozycje i ilości,
- masa towarów,
- masa palety,
- masa całkowita.

Po ustaleniu standardu etykieta może dodatkowo zawierać kod kreskowy albo kod QR, ale nie jest to obecnie wymaganie bazowe.

## 10. Użytkownicy i role

**Cel:** zarządzanie dostępem do tworzenia zamówień i operacji magazynowych.

Wstępny zakres:

- lista użytkowników,
- status aktywności,
- przypisane role,
- nadanie lub odebranie uprawnienia do tworzenia zamówień,
- dezaktywacja konta.

Dokładny zakres ekranu zależy od wybranego mechanizmu uwierzytelniania i autoryzacji.

## Główne przejście procesu

```text
Lista zamówień
    -> Nowe zamówienie
    -> Wersja robocza
    -> Udostępnione do kompletacji
    -> Kompletacja współdzielona
    -> Tworzenie palety
    -> Zamknięta paleta
    -> Podgląd i wydruk etykiety
```

Jedno zamówienie może zawierać wiele palet. Zamknięcie pojedynczej palety nie musi oznaczać zakończenia całego zamówienia.

## Kwestie UX do rozstrzygnięcia

- czy kompletacja będzie wykonywana na poziomie całej pozycji, czy części ilości,- ilości bo gdy zabraknie towaru można spakować to co jest w magazynie
- czy pozycje mogą być dzielone między palety, - tak
- czy wybór zamawiającego będzie listą z osobnego źródła, czy polem tekstowym,- polem tekstowym
- jak długo rezerwacja pozycji pozostaje aktywna bez działania użytkownika,- brak limitu
- czy aktualizacja współdzielonego widoku będzie działać w czasie rzeczywistym, - aplikacja dla osób kompletujących będzie na jednym komputerze
- jaki format i rozmiar ma mieć etykieta,
- czy aplikacja drukuje bezpośrednio, czy generuje plik do wydruku, - na początku generuje plik
- czy skanowanie kodów kreskowych będzie częścią pierwszej wersji. - nie
