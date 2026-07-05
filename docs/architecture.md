# Architektura projektu

## Cel architektury

Architektura ma oddzielić:

- aplikacyjny model zamówień, kompletacji i palet,
- odczyt katalogu towarów z Subiekta GT,
- zapis danych należących do aplikacji,
- API HTTP,
- frontend,
- generowanie etykiet palet.

Najważniejsze założenie: baza Subiekta GT nie jest modelem domenowym ani magazynem danych roboczych aplikacji. Tabele Subiekta są technicznym źródłem danych o towarach, mapowanych na modele odczytowe aplikacji. Zamówienia, rezerwacje pozycji, palety i historia operacji są przechowywane poza bazą Subiekta.

## Backend

Backend stosuje Clean Architecture.

```text
SubiektMobile.Api
    -> SubiektMobile.Application
    -> SubiektMobile.Infrastructure

SubiektMobile.Application
    -> SubiektMobile.Domain

SubiektMobile.Infrastructure
    -> SubiektMobile.Application
    -> SubiektMobile.Domain
```

## Warstwy

### Domain

Warstwa domenowa zawiera pojęcia aplikacji, a nie nazwy tabel Subiekta.

Przykładowe pojęcia domenowe:

- `Order`,
- `OrderItem`,
- `OrderItemAssignment`,
- `Pallet`,
- `PalletItem`,
- `PalletLabelData`.

Reguły domenowe obejmują między innymi:

- dozwolone przejścia statusów zamówień i pozycji,
- rezerwowanie i zwalnianie pozycji,
- warunki uznania pozycji za spakowaną,
- przypisanie spakowanej pozycji tylko do jednej palety,
- obliczanie masy towarów i masy całkowitej palety,
- warunki zamknięcia palety.

Ta warstwa nie zależy od EF Core, ASP.NET Core, SQL Servera ani fizycznej struktury bazy Subiekta.

### Application

Warstwa aplikacyjna zawiera przypadki użycia i koordynuje reguły domenowe.

Przykładowe przypadki użycia:

- pobranie listy i szczegółów towaru,
- utworzenie i edycja zamówienia,
- udostępnienie zamówienia do kompletacji,
- pobranie listy i szczegółów zamówienia,
- zarezerwowanie lub zwolnienie pozycji,
- oznaczenie pozycji jako spakowanej,
- utworzenie palety,
- przypisanie pozycji do palety,
- zamknięcie palety,
- przygotowanie danych etykiety,
- zapis informacji o wydruku.

Application definiuje kontrakty, które Infrastructure implementuje. Przypadki użycia otrzymują tożsamość użytkownika i weryfikują wymagane uprawnienia za pośrednictwem odpowiednich kontraktów lub polityk.

### Infrastructure

Warstwa infrastruktury odpowiada za techniczne szczegóły:

- kontekst odczytowy i encje EF Core odwzorowujące wymagane tabele Subiekta,
- implementacje projekcji towarów,
- osobny kontekst lub adapter trwałości danych aplikacji,
- transakcje i kontrolę współbieżności,
- implementację audytu,
- generator dokumentu etykiety,
- przyszłą integrację z drukarką.

Integracja odczytowa z Subiektem i trwałość danych aplikacji muszą mieć oddzielne kontrakty. Nie należy używać encji Subiekta jako encji zamówienia aplikacji.

### Api

Warstwa API odpowiada za HTTP.

Endpointy powinny być cienkie:

1. Przyjmują i walidują kształt requestu.
2. Przekazują tożsamość użytkownika do przypadku użycia z Application.
3. Mapują wynik na odpowiedź HTTP.

API nie powinno budować zapytań EF Core, znać szczegółów tabel Subiekta ani implementować reguł zmiany statusu.

## Magazyny danych

### Baza Subiekta GT

- Jest używana wyłącznie do odczytu danych towarowych wymaganych przez aplikację.
- Zapytania powinny być projekcjami bez śledzenia encji, jeśli nie ma technicznej potrzeby śledzenia.
- Uprawnienia połączenia powinny być ograniczone do odczytu, o ile środowisko na to pozwala.
- Connection string nie może być zapisany w repozytorium ani logowany.

### Baza aplikacji — PostgreSQL

Silnikiem bazy aplikacji jest PostgreSQL. Dostęp z backendu realizuje osobny `ApplicationDbContext` przez dostawcę Npgsql dla EF Core.

Przechowuje co najmniej:

- użytkowników lub ich identyfikatory z zewnętrznego systemu tożsamości,
- zamówienia i pozycje zamówień,
- migawki wymaganych danych towaru,
- rezerwacje i statusy kompletacji,
- palety oraz przypisane pozycje,
- masy użyte do obliczeń,
- historię operacji i wydruków.

Schemat będzie rozwijany migracjami EF Core wraz z zatwierdzaniem kolejnych części modelu domenowego. Dane aplikacji nie są zapisywane do tabel Subiekta.

## Współbieżność kompletacji

Rezerwacja pozycji i przypisanie jej do palety są operacjami konkurencyjnymi. Każda z nich musi:

1. Sprawdzić bieżący stan po stronie serwera.
2. Zapisać zmianę atomowo.
3. Wykryć konflikt współbieżności.
4. Zwrócić wynik pozwalający frontendowi odświeżyć dane i wyświetlić jednoznaczny komunikat.

Mechanizm może wykorzystywać znacznik wersji, warunkową aktualizację lub inną funkcję zapewnianą przez wybrany magazyn danych. Samo ukrycie przycisku we frontendzie nie zabezpiecza procesu.

## Model masy

Pozycja zamówienia zachowuje masę jednostkową używaną w procesie, niezależnie od późniejszych zmian danych źródłowych.

```text
masa pozycji na palecie = masa jednostkowa × ilość na palecie
masa towarów = suma mas pozycji na palecie
masa całkowita = masa towarów + masa pustej palety
```

Masa jednostkowa i masa pustej palety powinny używać typu dziesiętnego oraz jawnie określonej jednostki. Zaokrąglanie należy wykonywać według jednej reguły domenowej, a nie osobno w interfejsie i API.

## Planowane moduły aplikacyjne

### Product Catalog

- lista i wyszukiwanie towarów z Subiekta,
- szczegóły towaru,
- wybór towaru do zamówienia,
- sygnalizowanie brakującej masy jednostkowej.

### Order Management

- tworzenie i edycja wersji roboczej,
- walidacja zamawiającego, terminu, pozycji i ilości,
- udostępnianie zamówienia do kompletacji,
- lista, szczegóły i historia zamówienia.

### Collaborative Picking

- wspólny widok postępu,
- atomowe rezerwowanie pozycji,
- zwalnianie rezerwacji,
- oznaczanie pozycji jako spakowanej,
- zapis autora i czasu każdej operacji.

### Palletization

- tworzenie palet dla zamówienia,
- wybór spakowanych pozycji,
- obliczanie mas,
- zamykanie palety,
- przygotowanie i ponowny wydruk etykiety.

### Identity and Access

- administratorzy z indywidualnym loginem i hasłem,
- organizacje grupujące pracowników magazynu,
- wybór pracownika bez hasła na współdzielonym stanowisku,
- odwoływalne sesje przechowywane w PostgreSQL,
- stałe polityki uprawnień dla administratora i pracownika,
- audyt wykonawcy i czasu każdej mutacji.

Organizacja nie jest wykonawcą operacji. Sesja magazynowa zawsze wskazuje konkretnego
pracownika należącego do aktywnej organizacji. Publiczny wybór pracownika upraszcza pracę
na wspólnym komputerze, ale nie stanowi silnego uwierzytelnienia i pozwala osobie mającej
dostęp do aplikacji wybrać innego aktywnego pracownika.

Administrator ma uprawnienia do zarządzania tożsamością, katalogiem, zamówieniami,
kompletacją i paletami. Pracownik ma odczyt katalogu i udostępnionych zamówień oraz
uprawnienia do kompletacji i paletyzacji. Autoryzacja jest wykonywana w API oraz ponownie
w przypadkach użycia Application.

Token sesji jest losową wartością zapisywaną wyłącznie w cookie `HttpOnly`. PostgreSQL
przechowuje tylko jego skrót, czas wygaśnięcia i informację o unieważnieniu. Dezaktywacja
administratora, pracownika lub organizacji natychmiast odcina powiązane sesje.

## Granice bezpieczeństwa

- Nie logować connection stringów ani danych uwierzytelniających.
- Nie commitować sekretów.
- Nie pokazywać w API wewnętrznych błędów SQL w środowisku produkcyjnym.
- Endpointy diagnostyczne ograniczyć do środowiska developerskiego albo zabezpieczyć.
- Dane handlowe i dane użytkowników traktować jako wrażliwe dane firmowe.
- Każdą mutację chronić autoryzacją po stronie serwera.
- Nie polegać na uprawnieniach ani walidacji wykonywanej wyłącznie we frontendzie.

## Decyzje architektoniczne do podjęcia

- sposób wdrażania i wykonywania kopii zapasowych PostgreSQL,
- źródło zamawiających,
- źródło i sposób korekty brakującej masy jednostkowej,
- kompletacja częściowa i dzielenie ilości pozycji,
- dzielenie pozycji pomiędzy palety,
- strategia aktualizacji współdzielonego ekranu,
- czas ważności rezerwacji oraz odzyskiwanie porzuconych pozycji,
- format etykiety i integracja z drukarką,
- zasady korekt po zamknięciu palety lub wydruku,
- obsługa pracy offline i skanowania kodów kreskowych.
