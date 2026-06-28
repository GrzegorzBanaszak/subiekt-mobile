# Subiekt Mobile

Aplikacja API + web wspierająca mobilną pracę z danymi programu Subiekt GT.

Projekt ma umożliwić bezpieczny podgląd danych z Subiekta GT, a następnie obsługę procesów magazynowo-handlowych bez ryzykownego bezpośredniego zapisu do bazy Subiekta.

## Status projektu

Aktualny status: **przygotowanie fundamentów MVP**.

Najbliższy cel nie polega jeszcze na implementacji kompletacji ani generowania dokumentów. Najpierw trzeba zbudować stabilny system podglądu:

1. towarów,
2. zamówień od klientów,
3. przyjęć magazynowych.

Dopiero po tym etapie projekt powinien przejść do funkcji kompletowania zamówień i generowania plików EPP / EDI++.

## Główne funkcjonalności docelowe

### 1. Kompletowanie zamówienia klienta

Aplikacja będzie pobierać zamówienie od klienta z Subiekta GT i przetwarzać je na modele aplikacji.

Planowany proces:

1. Użytkownik wybiera zamówienie klienta.
2. Aplikacja pobiera pozycje zamówienia z Subiekta.
3. System tworzy sesję kompletacji w modelu aplikacji.
4. Użytkownik odhacza kolejne pozycje zamówienia.
5. System zapisuje postęp kompletacji.
6. Użytkownik może wygenerować raport kompletacji.

Ważne: sesja kompletacji jest funkcją aplikacji, a nie bezpośrednią modyfikacją dokumentu w bazie Subiekta.

### 2. Zamówienie do dostawcy przez EPP / EDI++

Aplikacja będzie umożliwiać przygotowanie zamówienia do dostawcy, ale nie przez bezpośrednie dodanie dokumentu do bazy Subiekta.

Planowany proces:

1. Użytkownik przygotowuje projekt zamówienia do dostawcy.
2. Aplikacja waliduje pozycje, ilości i wymagane dane.
3. System generuje plik EPP / EDI++.
4. Plik jest importowany do Subiekta GT.

Ważne: domyślna zasada projektu to **brak bezpośredniego zapisu do bazy Subiekta GT**. Integracja zapisu ma odbywać się przez pliki importu, chyba że zostanie podjęta osobna decyzja architektoniczna.

## Zakres MVP

### Etap 1 — podgląd towarów

- [ ] Lista towarów.
- [ ] Wyszukiwanie towarów.
- [ ] Szczegóły towaru.
- [ ] Dane identyfikacyjne, kody, jednostki.
- [ ] Dane magazynowe i cenowe, jeśli są dostępne w rozpoznanej strukturze bazy.

### Etap 2 — podgląd zamówień od klientów

- [ ] Lista zamówień od klientów.
- [ ] Filtrowanie po dacie, numerze dokumentu i kontrahencie.
- [ ] Szczegóły zamówienia.
- [ ] Pozycje zamówienia.
- [ ] Ilości, jednostki, towary i status dokumentu.

### Etap 3 — podgląd przyjęć magazynowych

- [ ] Lista przyjęć magazynowych.
- [ ] Szczegóły dokumentu.
- [ ] Pozycje dokumentu.
- [ ] Powiązane towary, daty i kontrahent / dostawca.

### Etap 4 — kompletowanie zamówienia

- [ ] Utworzenie sesji kompletacji na podstawie zamówienia klienta.
- [ ] Odhaczanie pozycji.
- [ ] Obsługa kompletacji częściowej.
- [ ] Status kompletacji.
- [ ] Raport kompletacji.

### Etap 5 — zamówienie do dostawcy jako EPP / EDI++

- [ ] Projekt zamówienia do dostawcy.
- [ ] Dodawanie pozycji.
- [ ] Walidacja danych.
- [ ] Generowanie pliku EPP / EDI++.
- [ ] Pobranie pliku przez użytkownika.
- [ ] Instrukcja importu pliku do Subiekta GT.

## Technologie

### Backend

- ASP.NET Core Web API,
- .NET 10,
- Entity Framework Core,
- SQL Server,
- Clean Architecture.

### Frontend

Frontend jest planowany jako aplikacja webowa przystosowana do pracy na telefonie, tablecie i komputerze.

Planowany stos:

- React / Vite / TypeScript albo Next.js,
- Tailwind CSS,
- klient API współdzielony dla wszystkich funkcji,
- struktura feature-based.

## Architektura

Projekt stosuje Clean Architecture po stronie backendu.

```text
subiekt-mobile/
├── AGENTS.md
├── backend/
│   ├── AGENTS.md
│   ├── src/
│   │   ├── SubiektMobile.Api/             # API HTTP i konfiguracja aplikacji
│   │   ├── SubiektMobile.Application/     # przypadki użycia i kontrakty
│   │   ├── SubiektMobile.Domain/          # model domenowy aplikacji
│   │   └── SubiektMobile.Infrastructure/  # EF Core, Subiekt GT, integracje
│   └── SubiektMobile.slnx
├── frontend/
│   └── AGENTS.md                          # zasady dla przyszłej aplikacji web
├── docs/
│   ├── architecture.md
│   └── roadmap.md
└── Readme.md
```

Najważniejsza zasada architektoniczna:

```text
Subiekt GT database -> Infrastructure -> Application -> Api / Frontend
```

Baza Subiekta GT jest technicznym źródłem danych. Modele domenowe aplikacji nie powinny być bezpośrednim odwzorowaniem tabel Subiekta.

Szczegóły są opisane w pliku [`docs/architecture.md`](docs/architecture.md).

## Instrukcje dla Codex

W repozytorium znajdują się pliki `AGENTS.md`, które opisują zasady pracy dla Codex:

- [`AGENTS.md`](AGENTS.md) — ogólne zasady projektu,
- [`backend/AGENTS.md`](backend/AGENTS.md) — zasady backendu,
- [`frontend/AGENTS.md`](frontend/AGENTS.md) — zasady przyszłego frontendu.

Przed większą zmianą Codex powinien przeczytać także:

- [`docs/architecture.md`](docs/architecture.md),
- [`docs/roadmap.md`](docs/roadmap.md).

## Wymagania developerskie

Minimalnie potrzebne:

- .NET SDK zgodny z projektem backendu,
- SQL Server z bazą Subiekta GT,
- dostęp odczytowy do bazy Subiekta GT,
- Node.js po dodaniu frontendu.

## Konfiguracja

Backend wymaga connection stringa o nazwie:

```text
ConnectionStrings:SubiektGt
```

Przykład ustawienia lokalnego sekretu:

```powershell
dotnet user-secrets set "ConnectionStrings:SubiektGt" "Server=NAZWA_SERWERA;Database=NAZWA_BAZY;..." --project backend/src/SubiektMobile.Api
```

Nie zapisuj prawdziwego connection stringa w repozytorium.

## Uruchomienie backendu

```powershell
dotnet run --project backend/src/SubiektMobile.Api
```

Po uruchomieniu dostępny jest endpoint:

```text
GET /health
```

## API towarów

Etap 1 udostępnia wyłącznie odczyt danych towarowych z Subiekta GT:

```text
GET /api/products?search=&page=1&pageSize=20
GET /api/products/{id}
GET /api/products/{id}/image
```

Lista jest stronicowana i może być przeszukiwana po nazwie, symbolu, kodzie
towaru oraz podstawowych i dodatkowych kodach kreskowych. Pokazuje stan
magazynu głównego i cenę brutto pierwszego poziomu cenowego. Szczegóły zwracają
stany wszystkich magazynów, stawkę VAT oraz dziesięć poziomów cen sprzedaży
netto i brutto. Zdjęcia są pobierane osobnym endpointem, a kartoteki usunięte
lub zablokowane nie są publikowane.

## Testy i sprawdzenie projektu

Jeżeli środowisko ma właściwe SDK:

```powershell
dotnet build backend/SubiektMobile.slnx
```

Po dodaniu testów:

```powershell
dotnet test backend/SubiektMobile.slnx
```

## Bezpieczeństwo

- Nie logować connection stringów.
- Nie commitować sekretów.
- Nie zwracać szczegółów błędów SQL w środowisku produkcyjnym.
- Endpointy diagnostyczne powinny być ograniczone do developmentu albo zabezpieczone.
- Dane handlowe traktować jako dane wrażliwe firmy.
- Domyślnie nie wykonywać bezpośredniego zapisu do bazy Subiekta GT.

## Plan rozwoju

Szczegółowa roadmapa znajduje się w [`docs/roadmap.md`](docs/roadmap.md).

Najbliższa kolejność:

1. Podgląd towarów.
2. Podgląd zamówień od klientów.
3. Podgląd przyjęć magazynowych.
4. Kompletowanie zamówienia.
5. Generowanie zamówienia do dostawcy jako EPP / EDI++.

## Licencja

Do uzupełnienia.

## Autor

Grzegorz Banaszak
