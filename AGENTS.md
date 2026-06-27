# AGENTS.md

## Cel projektu

Subiekt Mobile to aplikacja API + web wspierająca pracę z danymi programu Subiekt GT na telefonie lub tablecie.

Projekt ma rozwijać się w kierunku dwóch głównych procesów biznesowych:

1. Kompletowanie zamówienia klienta pobranego z Subiekta GT, przetworzonego na modele aplikacji, z odhaczaniem pozycji i raportem kompletacji.
2. Tworzenie zamówienia do dostawcy przez wygenerowanie pliku EPP / EDI++, który następnie jest importowany do Subiekta GT.

Przed implementacją tych procesów należy najpierw zbudować stabilny system podglądu danych:

- towary,
- zamówienia od klientów,
- przyjęcia magazynowe.

## Najważniejsze zasady

- Nie zapisuj bezpośrednio do bazy danych Subiekta GT, jeżeli zadanie nie mówi o tym jednoznacznie i nie ma zaakceptowanej decyzji architektonicznej.
- Dla zamówień do dostawcy domyślnym kierunkiem jest generowanie pliku EPP / EDI++, a nie bezpośredni zapis do tabel Subiekta.
- Najpierw implementuj funkcje odczytowe i model aplikacyjny, dopiero potem procesy biznesowe.
- Nie mieszaj logiki biznesowej z kontrolerami HTTP ani z EF Core.
- Nie twórz skrótów typu „byle działało”, które omijają warstwy projektu.
- Nie zapisuj haseł, connection stringów ani danych produkcyjnych w repozytorium.

## Architektura backendu

Backend stosuje Clean Architecture:

- `SubiektMobile.Domain` — model domenowy i reguły biznesowe aplikacji,
- `SubiektMobile.Application` — przypadki użycia, DTO, kontrakty portów, CQRS,
- `SubiektMobile.Infrastructure` — EF Core, mapowanie tabel Subiekta GT, integracje techniczne,
- `SubiektMobile.Api` — endpointy HTTP, konfiguracja aplikacji, DI, OpenAPI.

Dozwolone zależności:

```text
Api -> Application
Api -> Infrastructure, tylko do rejestracji DI
Application -> Domain
Infrastructure -> Application
Infrastructure -> Domain
```

Zabronione zależności:

```text
Domain -> Application
Domain -> Infrastructure
Domain -> Api
Application -> Infrastructure
Application -> Api
```

## Kolejność prac

1. Uporządkować fundamenty architektury i dokumentację.
2. Zaimplementować podgląd towarów.
3. Zaimplementować podgląd zamówień od klientów.
4. Zaimplementować podgląd przyjęć magazynowych.
5. Dopiero potem rozpocząć kompletowanie zamówień.
6. Na końcu rozpocząć generowanie zamówień do dostawcy jako plik EPP / EDI++.

## Zasady dla Codex

Przed zmianą kodu:

- sprawdź istniejącą strukturę projektu,
- przeczytaj `docs/architecture.md`,
- przeczytaj lokalny `AGENTS.md` w katalogu, którego dotyczy zmiana,
- zaplanuj zmianę zgodnie z aktualną architekturą.

Podczas implementacji:

- dodawaj kod w najmniejszym sensownym zakresie,
- nie dodawaj nowych bibliotek bez uzasadnienia,
- nie twórz duplikatów mapowania ani zapytań,
- nie zwracaj encji EF Core bezpośrednio z API,
- dla odczytów preferuj projekcje do DTO,
- dla nowych funkcji stosuj osobne przypadki użycia w Application.

Po zmianie:

- uruchom `dotnet build backend/SubiektMobile.slnx`, jeśli środowisko ma właściwe SDK,
- uruchom testy, jeżeli istnieją,
- opisz, czego nie dało się sprawdzić lokalnie.

## Definition of Done

Zmiana jest gotowa dopiero wtedy, gdy:

- kod kompiluje się,
- nie narusza zależności między warstwami,
- nie wprowadza bezpośredniego zapisu do bazy Subiekta bez decyzji architektonicznej,
- nie ujawnia sekretów,
- README albo dokumentacja zostały zaktualizowane, jeżeli zmieniło się zachowanie aplikacji,
- testy zostały dodane lub zaktualizowane, jeśli zmiana dotyczy logiki biznesowej.
