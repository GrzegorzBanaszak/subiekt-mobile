# Subiekt Mobile

Aplikacja API + web wspierająca tworzenie i wieloosobowe kompletowanie zamówień na telefonie lub tablecie.

Projekt odczytuje katalog towarów z bazy Subiekta GT. Zamówienia, stan kompletacji, palety i historia operacji należą do aplikacji i nie są zapisywane w tabelach Subiekta.

## Status projektu

Aktualny status: **katalog towarów i przygotowanie fundamentów procesu zamówień**.

Najbliższa kolejność prac:

1. stabilny odczyt i wyszukiwanie towarów z Subiekta,
2. użytkownicy oraz uprawnienia,
3. zamówienia tworzone w aplikacji,
4. wieloosobowa kompletacja,
5. paletyzacja i etykiety.

## Główne funkcjonalności docelowe

### 1. Tworzenie zamówienia

Uprawniony użytkownik tworzy zamówienie z towarów pobranych z katalogu Subiekta.

Planowany proces:

1. Użytkownik wskazuje zamawiającego i termin realizacji.
2. Dodaje towary oraz zamawiane ilości.
3. Aplikacja zapisuje nazwę i masę jednostkową używaną w zamówieniu.
4. Użytkownik zapisuje wersję roboczą, a po walidacji udostępnia ją do kompletacji.

### 2. Kompletacja współdzielona

Jedno zamówienie może być kompletowane równocześnie przez wiele osób.

Planowany proces:

1. Użytkownik wybiera dostępną pozycję i rezerwuje ją dla siebie.
2. Serwer atomowo potwierdza rezerwację, aby zapobiec podwójnemu przypisaniu.
3. Użytkownik oznacza pozycję jako spakowaną albo zwalnia ją do ponownego podjęcia.
4. Pozostali użytkownicy widzą aktualny stan oraz osobę kompletującą.

### 3. Paletyzacja i etykiety

Planowany proces:

1. Użytkownik tworzy paletę ze spakowanych pozycji.
2. Wprowadza masę pustej palety.
3. System sumuje masę pozycji jako `masa jednostkowa × ilość` i dodaje masę palety.
4. Po zamknięciu palety użytkownik może zweryfikować i wydrukować etykietę z zamawiającym, pozycjami i masą całkowitą.

Ważne: baza Subiekta jest w tym procesie wyłącznie źródłem danych towarowych w trybie odczytu.

## Zakres MVP

### Etap 1 — podgląd towarów

- [ ] Lista towarów.
- [ ] Wyszukiwanie towarów.
- [ ] Szczegóły towaru.
- [ ] Dane identyfikacyjne, kody, jednostki.
- [ ] Masa jednostkowa albo jednoznaczna informacja o jej braku.

### Etap 2 — użytkownicy i uprawnienia

- [x] Logowanie administratora i wybór pracownika organizacji.
- [x] Uprawnienie do tworzenia zamówień.
- [x] Uprawnienie do kompletacji i paletyzacji.
- [x] Audyt użytkownika i czasu operacji.

### Etap 3 — zamówienia aplikacji

- [ ] Lista i szczegóły zamówień.
- [ ] Zamawiający i termin realizacji.
- [ ] Dodawanie towarów i ilości.
- [ ] Wersja robocza i udostępnienie do kompletacji.

### Etap 4 — kompletacja współdzielona

- [ ] Atomowe rezerwowanie pozycji.
- [ ] Zwolnienie rezerwacji.
- [ ] Oznaczanie pozycji jako spakowanej.
- [ ] Wspólny widok postępu.
- [ ] Obsługa konfliktów współbieżności.

### Etap 5 — palety i etykiety

- [ ] Tworzenie palet ze spakowanych pozycji.
- [ ] Masa pustej palety.
- [ ] Masa towarów i masa całkowita.
- [ ] Zamknięcie palety.
- [ ] Podgląd i wydruk etykiety.

## Technologie

### Backend

- ASP.NET Core Web API,
- .NET 10,
- Entity Framework Core,
- SQL Server dla odczytu danych Subiekta,
- PostgreSQL dla danych aplikacji,
- Clean Architecture.

### Frontend

Frontend jest aplikacją webową przystosowaną do pracy na telefonie, tablecie i komputerze.

Stos:

- React 19 / Vite / TypeScript,
- Tailwind CSS,
- TanStack Query i wspólny klient generowany z OpenAPI,
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
│   ├── roadmap.md
│   └── zarys-ekranow.md
└── Readme.md
```

Najważniejsza zasada architektoniczna:

```text
Subiekt GT database --read--> Infrastructure -> Application -> Api / Frontend
Application database <------> Infrastructure
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
- [`docs/roadmap.md`](docs/roadmap.md),
- [`docs/zarys-ekranow.md`](docs/zarys-ekranow.md).

## Wymagania developerskie

Minimalnie potrzebne:

- .NET SDK zgodny z projektem backendu,
- SQL Server z bazą Subiekta GT,
- dostęp odczytowy do bazy Subiekta GT,
- PostgreSQL dla zamówień i pozostałych danych aplikacji,
- Node.js po dodaniu frontendu.

## Konfiguracja

Backend wymaga dwóch connection stringów:

```text
ConnectionStrings:SubiektGt
ConnectionStrings:ApplicationDb
```

Przykład ustawienia lokalnych sekretów:

```powershell
dotnet user-secrets set "ConnectionStrings:SubiektGt" "Server=NAZWA_SERWERA;Database=NAZWA_BAZY;..." --project backend/src/SubiektMobile.Api
dotnet user-secrets set "ConnectionStrings:ApplicationDb" "Host=localhost;Database=subiekt_mobile;Username=NAZWA_UZYTKOWNIKA;Password=HASLO" --project backend/src/SubiektMobile.Api
dotnet user-secrets set "Identity:BootstrapToken" "LOSOWY_SEKRET_MINIMUM_32_ZNAKI" --project backend/src/SubiektMobile.Api
```

Nie zapisuj prawdziwego connection stringa w repozytorium.
Token bootstrapu również jest sekretem i nie powinien trafiać do `appsettings.json` ani repozytorium.

### Automatyczne utworzenie pierwszego administratora

Backend może opcjonalnie utworzyć konto bootstrapowe podczas startu. Mechanizm jest
przeznaczony do wdrożeń automatyzowanych, na przykład przez Docker lub Terraform.
Wymaga jednoczesnego ustawienia trzech wartości konfiguracyjnych:

```text
Identity:BootstrapAdministrator:Username
Identity:BootstrapAdministrator:DisplayName
Identity:BootstrapAdministrator:Password
```

Odpowiadające im zmienne środowiskowe platformy .NET:

```text
Identity__BootstrapAdministrator__Username
Identity__BootstrapAdministrator__DisplayName
Identity__BootstrapAdministrator__Password
```

Przykład lokalny z `user-secrets`:

```powershell
dotnet user-secrets set "Identity:BootstrapAdministrator:Username" "admin" --project backend/src/SubiektMobile.Api
dotnet user-secrets set "Identity:BootstrapAdministrator:DisplayName" "Administrator" --project backend/src/SubiektMobile.Api
dotnet user-secrets set "Identity:BootstrapAdministrator:Password" "SILNE_HASLO_MINIMUM_12_ZNAKOW" --project backend/src/SubiektMobile.Api
```

Jeżeli konto bootstrapowe już istnieje, aplikacja nie zmienia jego loginu, nazwy ani
hasła. Przy równoczesnym starcie wielu instancji dokładnie jedna tworzy konto, a pozostałe
kontynuują uruchamianie po wykryciu istniejącego administratora. Podanie tylko części
konfiguracji zatrzymuje start aplikacji z błędem. Schemat bazy aplikacji musi być
zmigrowany przed uruchomieniem backendu.

Konto bootstrapowe jest stałym kontem root i jako jedyne ma uprawnienie
`identity.administrators.manage`. Zwykli administratorzy zachowują `identity.manage`, które
pozwala zarządzać organizacjami i ich pracownikami, ale nie kontami administratorów.

Hasła nie należy umieszczać w obrazie kontenera, pliku Terraform ani repozytorium.
W środowisku docelowym powinno zostać dostarczone jako sekret wdrożeniowy lub przez
menedżer sekretów. Pozostawienie zmiennych po pierwszym uruchomieniu jest bezpieczne
funkcjonalnie, ale ich usunięcie ogranicza czas ekspozycji danych uwierzytelniających.

Domyślna ważność sesji administratora wynosi 8 godzin, a pracownika 12 godzin. Można ją
zmienić ustawieniami `Identity:AdministratorSessionHours` i `Identity:EmployeeSessionHours`.
Przy wdrożeniu kontenerowym lub wielu instancjach należy ustawić `DataProtection:KeyPath`
na trwały, współdzielony katalog kluczy używanych do ochrony tokenów CSRF.

### Lokalny PostgreSQL w Dockerze

Skopiuj przykładową konfigurację i ustaw lokalne hasło w pliku `.env`:

```powershell
Copy-Item .env.example .env
```

Następnie uruchom bazę aplikacji:

```powershell
docker compose up -d application-db
```

Domyślny connection string odpowiadający konfiguracji z `.env.example`:

```text
Host=localhost;Port=5432;Database=subiekt_mobile;Username=subiekt_mobile;Password=USTAWIONE_HASLO
```

Stan kontenera można sprawdzić poleceniem `docker compose ps`. Dane są zachowywane
w wolumenie `application-db-data` po zatrzymaniu kontenera.

## Uruchomienie backendu

```powershell
dotnet run --project backend/src/SubiektMobile.Api
```

Po uruchomieniu dostępny jest endpoint:

```text
GET /health
```

W środowisku developerskim interfejs do testowania endpointów jest dostępny pod adresem:

```text
http://localhost:5118/swagger
```

Dokument OpenAPI jest dostępny pod `/openapi/v1.json`. Swagger i dokument OpenAPI nie są publikowane poza środowiskiem developerskim.

## Uruchomienie frontendu

```powershell
cd frontend
npm install
npm run dev
```

Frontend jest dostępny pod `http://127.0.0.1:5173`. W trybie developerskim
żądania `/api` są przekazywane do backendu pod `http://localhost:5118`.

Trasa `/login` obsługuje logowanie administratora przez sesję zapisywaną w cookie
`HttpOnly`. Interfejs jest dostępny po polsku i hiszpańsku, a wybór języka jest
zapamiętywany lokalnie w przeglądarce.

Typy klienta API można odświeżyć przy uruchomionym backendzie:

```powershell
npm run api:types
```

## API towarów

Etap 1 udostępnia wyłącznie odczyt danych towarowych z Subiekta GT:

```text
GET /api/products?search=&page=1&pageSize=20
GET /api/products/{id}
GET /api/products/{id}/image
```

Endpointy katalogu wymagają aktywnej sesji administratora albo pracownika.

Lista jest stronicowana i może być przeszukiwana po nazwie, symbolu, kodzie
towaru oraz podstawowych i dodatkowych kodach kreskowych. Pokazuje stan
magazynu głównego i cenę brutto pierwszego poziomu cenowego. Szczegóły zwracają
stany wszystkich magazynów, stawkę VAT oraz dziesięć poziomów cen sprzedaży
netto i brutto. Zdjęcia są pobierane osobnym endpointem, a kartoteki usunięte
lub zablokowane nie są publikowane.

## API użytkowników i organizacji

Przed każdym żądaniem `POST` lub `PUT` klient pobiera token przez
`GET /api/auth/csrf-token` i przesyła go w nagłówku `X-CSRF-TOKEN`. Przeglądarka musi
również wysyłać cookies.

Publiczny proces wejścia:

```text
POST /api/auth/bootstrap-administrator       # jednorazowo, z X-Setup-Token
POST /api/auth/administrator/sign-in
GET  /api/auth/organizations
GET  /api/auth/organizations/{id}/employees
POST /api/auth/employee/select
GET  /api/auth/me
POST /api/auth/sign-out
```

Endpointy administracyjne znajdują się pod `/api/admin` i obejmują zarządzanie
administratorami, organizacjami oraz pracownikami. Zasoby są dezaktywowane zamiast
fizycznie usuwane. Publiczny wybór pracownika bez hasła jest celowym uproszczeniem MVP,
nie zabezpiecza jednak przed wybraniem cudzej tożsamości przez osobę mającą dostęp do aplikacji.

Frontend udostępnia te operacje pod `/administration`. Zakładka administratorów jest widoczna
wyłącznie dla konta root; organizacje i pracownicy są dostępni również zwykłym administratorom.

Pierwszą migrację stosuje polecenie:

```powershell
dotnet ef database update --context ApplicationDbContext `
  --project backend/src/SubiektMobile.Infrastructure `
  --startup-project backend/src/SubiektMobile.Api
```

## Testy i sprawdzenie projektu

Jeżeli środowisko ma właściwe SDK:

```powershell
dotnet build backend/SubiektMobile.slnx
```

Po dodaniu testów:

```powershell
dotnet test backend/SubiektMobile.slnx
```

Testy integracyjne PostgreSQL uruchamiają się po ustawieniu `SUBIEKT_MOBILE_TEST_DB`.
Ze względów bezpieczeństwa connection string musi wskazywać dedykowaną bazę, której nazwa
kończy się `_tests`; testy usuwają i odtwarzają tę bazę.

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
2. Użytkownicy i uprawnienia.
3. Tworzenie zamówień.
4. Kompletacja współdzielona.
5. Paletyzacja i etykiety.

## Licencja

Do uzupełnienia.

## Autor

Grzegorz Banaszak
