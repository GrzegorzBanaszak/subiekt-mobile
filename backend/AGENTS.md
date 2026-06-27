# backend/AGENTS.md

## Zakres

Instrukcje dotyczą wszystkich zmian w katalogu `backend/`.

Backend jest aplikacją ASP.NET Core opartą o Clean Architecture i integrację odczytową z bazą Subiekta GT.

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
- generowanie plików technicznych, np. EPP / EDI++, jeśli dotyczy integracji zewnętrznej.

## Zasady integracji z Subiektem GT

- Domyślnie baza Subiekta GT jest źródłem odczytu.
- Nie zapisuj do tabel Subiekta bez zaakceptowanej decyzji architektonicznej.
- Dla zamówień do dostawcy generuj plik EPP / EDI++ zamiast pisać bezpośrednio do bazy.
- Encje EF Core powinny odwzorowywać strukturę bazy, ale nie powinny być modelem domenowym aplikacji.
- Nie zwracaj encji Subiekta bezpośrednio z API.

## Priorytet implementacji

Najpierw należy dostarczyć podgląd:

1. Towary — lista, szczegóły, podstawowe dane identyfikacyjne, kody, stany i ceny, jeśli są dostępne.
2. Zamówienia od klientów — lista, szczegóły, pozycje, kontrahent, status i daty.
3. Przyjęcia magazynowe — lista, szczegóły, pozycje i dokument powiązany.

Dopiero potem implementuj:

1. Kompletowanie zamówienia klienta.
2. Raport kompletacji.
3. Generowanie zamówienia do dostawcy jako EPP / EDI++.

## Kontrolery i endpointy

Kontroler lub endpoint powinien:

- przyjąć parametry,
- wywołać przypadek użycia z Application,
- zwrócić wynik HTTP.

Kontroler lub endpoint nie powinien:

- budować zapytań EF Core,
- znać nazw tabel Subiekta,
- zawierać reguł kompletacji,
- generować EPP / EDI++ bezpośrednio.

## EF Core

- Dla odczytów używaj `AsNoTracking()`.
- Dla list używaj paginacji.
- Dla API używaj projekcji do DTO lub modeli odczytowych.
- Nie dodawaj `Include`, jeżeli wystarczy projekcja.
- Nie publikuj connection stringów w logach.

## Komendy sprawdzające

Jeśli środowisko ma właściwe SDK:

```powershell
dotnet build backend/SubiektMobile.slnx
```

Jeżeli powstaną testy:

```powershell
dotnet test backend/SubiektMobile.slnx
```
