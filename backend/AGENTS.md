# backend/AGENTS.md

## Zakres

Instrukcje dotyczą wszystkich zmian w katalogu `backend/`.

Backend jest aplikacją ASP.NET Core opartą o Clean Architecture i integrację odczytową z bazą Subiekta GT.

Z Subiekta pobierany jest katalog towarów. Zamówienia, rezerwacje pozycji, palety i audyt są danymi aplikacji i są przechowywane w osobnej bazie PostgreSQL.

## Warstwy

### `SubiektMobile.Api`

Odpowiada tylko za:

- konfigurację aplikacji,
- rejestrację middleware,
- mapowanie endpointów / kontrolerów,
- obsługę HTTP,
- OpenAPI,
- health checks.

Nie umieszczaj tutaj logiki biznesowej ani zapytań EF Core.

### `SubiektMobile.Application`

Odpowiada za:

- przypadki użycia,
- komendy i zapytania,
- DTO request / response,
- kontrakty portów,
- walidację aplikacyjną,
- mapowanie danych z modelu infrastruktury do modelu aplikacji.

Nowe funkcjonalności zaczynaj od Application.

### `SubiektMobile.Domain`

Odpowiada za:

- model domenowy aplikacji,
- reguły kompletacji zamówień,
- statusy i wartości biznesowe,
- logikę niezależną od HTTP, EF Core i Subiekta GT.

Domain nie może znać EF Core, ASP.NET Core ani struktury tabel Subiekta.

### `SubiektMobile.Infrastructure`

Odpowiada za:

- `DbContext`,
- encje odwzorowujące tabele Subiekta GT,
- konfiguracje EF Core,
- implementacje repozytoriów i portów,
- osobną trwałość danych należących do aplikacji,
- kontrolę współbieżności operacji kompletacji,
- generowanie etykiet palet i integracje techniczne związane z wydrukiem.

## Zasady integracji z Subiektem GT

- Domyślnie baza Subiekta GT jest źródłem odczytu.
- Nie zapisuj do tabel Subiekta bez zaakceptowanej decyzji architektonicznej.
- W aktualnym zakresie z Subiekta odczytuj wyłącznie dane towarowe wymagane przez aplikację.
- Zamówienia i przebieg kompletacji zapisuj przez `ApplicationDbContext` w PostgreSQL.
- Encje EF Core powinny odwzorowywać strukturę bazy, ale nie powinny być modelem domenowym aplikacji.
- Nie zwracaj encji Subiekta bezpośrednio z API.

## Priorytet implementacji

Implementuj w następującej kolejności:

1. Towary — odczyt, wyszukiwanie, paginacja i masa jednostkowa.
2. Użytkownicy oraz minimalne role i uprawnienia.
3. Zamówienia tworzone w aplikacji — zamawiający, termin i pozycje.
4. Wieloosobowa kompletacja z atomowym rezerwowaniem pozycji.
5. Paletyzacja i obliczanie masy.
6. Generowanie i wydruk etykiet palet.

## Kontrolery i endpointy

Kontroler lub endpoint powinien:

- przyjąć parametry,
- wywołać przypadek użycia z Application,
- zwrócić wynik HTTP.

Kontroler lub endpoint nie powinien:

- budować zapytań EF Core,
- znać nazw tabel Subiekta,
- zawierać reguł kompletacji,
- obliczać mas palety ani generować etykiety bezpośrednio.

## EF Core

- Dla odczytów używaj `AsNoTracking()`.
- Dla list używaj paginacji.
- Dla API używaj projekcji do DTO lub modeli odczytowych.
- Nie dodawaj `Include`, jeżeli wystarczy projekcja.
- Nie publikuj connection stringów w logach.
- Rozdziel kontekst odczytowy Subiekta od kontekstu zapisu danych aplikacji.
- Dla rezerwacji pozycji i przypisywania do palet stosuj transakcje oraz jawne wykrywanie konfliktów współbieżności.
- Każda mutacja procesu powinna zapisać identyfikator użytkownika i czas operacji.

## Komendy sprawdzające

Jeśli środowisko ma właściwe SDK:

```powershell
dotnet build backend/SubiektMobile.slnx
```

Jeżeli powstaną testy:

```powershell
dotnet test backend/SubiektMobile.slnx
```
