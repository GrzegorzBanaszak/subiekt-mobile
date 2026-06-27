# Architektura projektu

## Cel architektury

Architektura ma oddzielić:

- aplikacyjny model pracy magazynowej,
- odczyt danych z Subiekta GT,
- API HTTP,
- przyszły frontend,
- generowanie plików EPP / EDI++.

Najważniejsze założenie: baza Subiekta GT nie jest modelem domenowym aplikacji. Tabele Subiekta są źródłem danych technicznych, które należy mapować na modele aplikacji.

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

- `OrderPickingSession`,
- `OrderPickingItem`,
- `PickingReport`,
- `SupplierOrderDraft`,
- `EppExportFile`.

Ta warstwa nie powinna zależeć od EF Core, ASP.NET Core, SQL Servera ani fizycznej struktury bazy Subiekta.

### Application

Warstwa aplikacyjna zawiera przypadki użycia.

Przykładowe przypadki użycia:

- pobranie listy towarów,
- pobranie szczegółów towaru,
- pobranie listy zamówień od klientów,
- pobranie szczegółów zamówienia klienta,
- pobranie listy przyjęć magazynowych,
- utworzenie sesji kompletacji,
- oznaczenie pozycji jako skompletowanej,
- wygenerowanie raportu kompletacji,
- przygotowanie zamówienia do dostawcy,
- wygenerowanie pliku EPP / EDI++.

Application definiuje kontrakty, które Infrastructure implementuje.

### Infrastructure

Warstwa infrastruktury odpowiada za techniczne szczegóły.

Przykładowe elementy:

- `SubiektDbContext`,
- encje EF Core odwzorowujące tabele Subiekta,
- konfiguracje EF Core,
- implementacje zapytań odczytowych,
- implementacja generatora EPP / EDI++.

Infrastructure może znać nazwy tabel, kolumn i ograniczenia bazy Subiekta.

### Api

Warstwa API odpowiada za HTTP.

Endpointy powinny być cienkie:

1. Przyjmują request.
2. Wywołują przypadek użycia z Application.
3. Zwracają response.

API nie powinno budować zapytań EF Core ani znać szczegółów tabel Subiekta.

## Integracja z Subiektem GT

### Odczyt

Dane z Subiekta GT są pobierane przez EF Core i mapowane na modele odczytowe aplikacji.

Priorytetem są:

1. Towary.
2. Zamówienia od klientów.
3. Przyjęcia magazynowe.

### Zapis

Domyślnie aplikacja nie zapisuje bezpośrednio do bazy Subiekta GT.

Zamówienia do dostawcy mają być przygotowywane przez wygenerowanie pliku EPP / EDI++ i importowane do Subiekta GT z poziomu Subiekta.

Bezpośredni zapis do bazy może zostać rozważony tylko po osobnej decyzji architektonicznej.

## Planowane moduły aplikacyjne

### Inventory Browse

Podgląd danych z Subiekta GT:

- towary,
- szczegóły towaru,
- zamówienia od klientów,
- przyjęcia magazynowe.

To jest fundament dla kolejnych funkcji.

### Order Picking

Kompletowanie zamówienia klienta:

1. Użytkownik wybiera zamówienie klienta pobrane z Subiekta.
2. Aplikacja przetwarza dokument i pozycje na model kompletacji.
3. Użytkownik odhacza pozycje.
4. System zapisuje stan kompletacji w modelu aplikacji.
5. System generuje raport kompletacji.

### Supplier Orders

Tworzenie zamówienia do dostawcy:

1. Użytkownik przygotowuje projekt zamówienia.
2. Aplikacja waliduje pozycje i ilości.
3. System generuje plik EPP / EDI++.
4. Użytkownik importuje plik do Subiekta GT.

## Granice bezpieczeństwa

- Nie logować connection stringów.
- Nie commitować sekretów.
- Nie pokazywać w API wewnętrznych błędów SQL w środowisku produkcyjnym.
- Endpointy diagnostyczne powinny być ograniczone do środowiska developerskiego albo zabezpieczone.
- Dane handlowe traktować jako wrażliwe dane firmowe.

## Decyzje architektoniczne do podjęcia później

- Czy sesje kompletacji będą zapisywane w osobnej bazie aplikacji, czy tymczasowo w pamięci.
- Format i dokładna struktura pliku EPP / EDI++ dla zamówienia do dostawcy.
- Mechanizm autoryzacji użytkowników.
- Obsługa pracy offline.
- Strategia skanowania kodów kreskowych.
