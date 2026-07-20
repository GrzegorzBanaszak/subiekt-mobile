# Uruchomienie lokalne

Ten dokument opisuje konfigurację środowiska developerskiego: lokalny PostgreSQL w Dockerze,
migrację bazy aplikacji oraz uruchomienie API i frontendu. Baza Subiekta GT pozostaje źródłem
tylko do odczytu i nie jest uruchamiana przez Docker Compose tego projektu.

## Wymagania

- Git;
- Docker Desktop albo Docker Engine z Docker Compose;
- .NET SDK 10;
- Node.js w wersji `20.19+` albo `22.12+`;
- dostęp sieciowy do SQL Servera z bazą Subiekta GT oraz konto z uprawnieniami tylko do odczytu.

Sprawdź narzędzia:

```powershell
docker compose version
dotnet --version
node --version
npm --version
```

## 1. Konfiguracja PostgreSQL w Dockerze

Utwórz lokalny plik `infra/dev/.env`. Jest ignorowany przez Git i może zawierać hasło do
lokalnej bazy. Nie kopiuj do niego danych produkcyjnych.

```powershell
@'
APPLICATION_DB_NAME=subiekt_mobile
APPLICATION_DB_USER=subiekt_mobile
APPLICATION_DB_PASSWORD=ZmienToNaLokalneHaslo
APPLICATION_DB_PORT=5432
'@ | Set-Content -Encoding utf8 infra/dev/.env
```

| Zmienna | Wymagana | Opis |
| --- | --- | --- |
| `APPLICATION_DB_NAME` | tak | Nazwa lokalnej bazy aplikacji. |
| `APPLICATION_DB_USER` | tak | Użytkownik PostgreSQL tworzony przez kontener. |
| `APPLICATION_DB_PASSWORD` | tak | Hasło tego użytkownika. |
| `APPLICATION_DB_PORT` | nie | Port wystawiony na hoście; domyślnie `5432`. Zmień go, gdy ten port jest zajęty. |

Uruchom i sprawdź bazę:

```powershell
docker compose -f infra/dev/compose.yaml --env-file infra/dev/.env up -d application-db
docker compose -f infra/dev/compose.yaml --env-file infra/dev/.env ps
```

Connection string odpowiadający powyższej konfiguracji ma postać:

```text
Host=localhost;Port=5432;Database=subiekt_mobile;Username=subiekt_mobile;Password=ZmienToNaLokalneHaslo
```

Jeżeli ustawiono inny `APPLICATION_DB_PORT`, `APPLICATION_DB_NAME` lub `APPLICATION_DB_USER`,
zastosuj te same wartości w connection stringu backendu.

## 2. Sekrety i zmienne backendu

API nie uruchomi się bez dwóch connection stringów. Lokalnie zapisuj je w .NET User Secrets,
a nie w `appsettings*.json` ani w repozytorium.

```powershell
dotnet user-secrets set "ConnectionStrings:ApplicationDb" "Host=localhost;Port=5432;Database=subiekt_mobile;Username=subiekt_mobile;Password=ZmienToNaLokalneHaslo" --project backend/src/SubiektMobile.Api

dotnet user-secrets set "ConnectionStrings:SubiektGt" "Server=NAZWA_LUB_ADRES_SERWERA,1433;Database=NAZWA_BAZY_SUBIEKTA;User Id=UZYTKOWNIK_TYLKO_ODCZYT;Password=HASLO;Encrypt=True;TrustServerCertificate=True" --project backend/src/SubiektMobile.Api
```

| Klucz konfiguracji | Wymagany | Jak ustawić | Opis |
| --- | --- | --- | --- |
| `ConnectionStrings:ApplicationDb` | tak | `dotnet user-secrets set` | PostgreSQL aplikacji uruchomiony lokalnie w Dockerze. |
| `ConnectionStrings:SubiektGt` | tak | `dotnet user-secrets set` | SQL Server Subiekta GT; używaj konta tylko do odczytu. |
| `Identity:BootstrapAdministrator:Username` | opcjonalnie, razem z pozostałymi dwoma | `dotnet user-secrets set` | Login pierwszego konta root. |
| `Identity:BootstrapAdministrator:DisplayName` | opcjonalnie, razem z pozostałymi dwoma | `dotnet user-secrets set` | Nazwa wyświetlana pierwszego konta root. |
| `Identity:BootstrapAdministrator:Password` | opcjonalnie, razem z pozostałymi dwoma | `dotnet user-secrets set` | Hasło pierwszego konta root, minimum 12 znaków. |
| `Cors:MainOrigin` | nie lokalnie | zmienna środowiskowa lub User Secrets | Dodatkowy origin frontendu; domyślne adresy Vite są już dozwolone. |
| `DataProtection:KeyPath` | nie lokalnie | zmienna środowiskowa lub User Secrets | Trwały katalog kluczy; wymagany przy wielu instancjach lub kontenerowym API. |

Aby przy pierwszym uruchomieniu automatycznie utworzyć administratora, ustaw **wszystkie trzy**
pola bootstrapu albo nie ustawiaj żadnego:

```powershell
dotnet user-secrets set "Identity:BootstrapAdministrator:Username" "admin" --project backend/src/SubiektMobile.Api
dotnet user-secrets set "Identity:BootstrapAdministrator:DisplayName" "Administrator lokalny" --project backend/src/SubiektMobile.Api
dotnet user-secrets set "Identity:BootstrapAdministrator:Password" "UstawTuSilneHaslo" --project backend/src/SubiektMobile.Api
```

Wartości można sprawdzić bez ujawniania ich w plikach projektu:

```powershell
dotnet user-secrets list --project backend/src/SubiektMobile.Api
```

## 3. Migracja lokalnej bazy

Po uruchomieniu PostgreSQL i ustawieniu `ConnectionStrings:ApplicationDb` wykonaj migracje.
Rekomendowane polecenie korzysta z tego samego trybu migracyjnego co wdrożenie:

```powershell
dotnet run --project backend/src/SubiektMobile.Api -- --migrate
```

Polecenie kończy pracę po zastosowaniu wszystkich oczekujących migracji. Nie uruchamia serwera
HTTP. W razie potrzeby równoważne polecenie EF Core to:

```powershell
dotnet ef database update --context ApplicationDbContext `
  --project backend/src/SubiektMobile.Infrastructure `
  --startup-project backend/src/SubiektMobile.Api
```

Przy kolejnej aktualizacji kodu wystarczy ponownie wykonać `--migrate`; EF Core zastosuje tylko
nowe migracje. Lokalnych danych nie usuwaj przez kasowanie wolumenu, chyba że świadomie chcesz
zacząć od pustej bazy.

## 4. Uruchomienie API

W osobnym terminalu:

```powershell
dotnet run --project backend/src/SubiektMobile.Api
```

API działa domyślnie pod `http://localhost:5118`. W środowisku Development dostępne są:

```text
http://localhost:5118/swagger
http://localhost:5118/openapi/v1.json
http://localhost:5118/health
```

## 5. Uruchomienie frontendu

W drugim terminalu:

```powershell
Set-Location frontend
npm install
npm run dev
```

Frontend jest dostępny pod `http://127.0.0.1:5173`. Domyślna konfiguracja Vite przekazuje
żądania `/api` do `http://localhost:5118`, więc plik `frontend/.env.local` nie jest wymagany.

Jeżeli frontend ma łączyć się bezpośrednio z innym API, utwórz `frontend/.env.local`:

```text
VITE_API_BASE_URL=http://localhost:5118
```

Po zmianie `VITE_API_BASE_URL` uruchom ponownie `npm run dev`.

## Codzienny start i zatrzymanie

Po jednorazowej konfiguracji typowa sesja lokalna wymaga trzech terminali:

```powershell
# Terminal 1 — PostgreSQL
docker compose -f infra/dev/compose.yaml --env-file infra/dev/.env up -d application-db

# Terminal 2 — API
dotnet run --project backend/src/SubiektMobile.Api

# Terminal 3 — frontend
Set-Location frontend
npm run dev
```

Zatrzymanie lokalnej bazy bez usuwania danych:

```powershell
docker compose -f infra/dev/compose.yaml --env-file infra/dev/.env stop application-db
```

Pełną procedurę migracji na środowisku wdrożeniowym opisuje
[deployment.md](deployment.md).

