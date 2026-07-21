# AGENTS.md

## Cel projektu

Subiekt Mobile to aplikacja API + web wspierająca tworzenie i wieloosobowe kompletowanie zamówień na telefonie lub tablecie.

Źródłem danych o towarach oraz opcjonalnych powiązaniach klientów z kontrahentami jest baza SQL programu Subiekt GT. Zamówienia, przydziały pozycji, palety i historia kompletacji należą do modelu aplikacji i nie są dokumentami Subiekta GT.

Główny proces biznesowy:

1. Aplikacja pobiera towary i minimalny katalog kontrahentów z bazy Subiekta GT w trybie tylko do odczytu.
2. Uprawniony użytkownik tworzy zamówienie z towarów dostępnych w katalogu.
3. Wielu użytkowników równolegle kompletuje pozycje jednego zamówienia.
4. Skompletowane pozycje są przypisywane do palet.
5. Aplikacja oblicza masę palety i umożliwia wydruk etykiety.

## Zakres funkcjonalny

### Katalog towarów

- Towary są pobierane z bazy SQL Subiekta GT bez modyfikowania jej danych.
- Minimalny zakres danych to identyfikator źródłowy, symbol lub kod, nazwa, jednostka miary oraz masa jednostkowa, jeśli jest dostępna.
- Listy towarów powinny obsługiwać wyszukiwanie i paginację.
- Encje i nazwy tabel Subiekta nie mogą być ujawniane poza warstwą Infrastructure.

### Użytkownicy i uprawnienia

- Tylko uprawniony użytkownik może tworzyć, edytować i udostępniać zamówienie do kompletacji.
- Użytkownik kompletujący może przeglądać udostępnione zamówienia, rezerwować pozycje, oznaczać je jako spakowane i kompletować palety w zakresie nadanych uprawnień.
- Każda operacja zmieniająca stan kompletacji powinna zapisywać wykonującego ją użytkownika i czas wykonania.
- Mechanizm autoryzacji oraz dokładna macierz ról wymagają osobnej decyzji, ale przypadki użycia nie mogą zakładać anonimowego użytkownika.

### Zamówienie

Zamówienie jest tworzone i przechowywane w modelu aplikacji. Powinno zawierać co najmniej:

- identyfikator i czytelny numer zamówienia,
- zamawiającego,
- termin realizacji,
- status zamówienia,
- autora oraz datę utworzenia,
- listę pozycji.

Pozycja zamówienia powinna zawierać co najmniej:

- referencję do towaru z Subiekta,
- migawkę nazwy towaru, aby późniejsza zmiana nazwy w Subiekcie nie zmieniała historii zamówienia,
- zamówioną ilość i jednostkę miary,
- masę jednostkową używaną do obliczeń,
- status kompletacji,
- informację o użytkowniku aktualnie kompletującym pozycję, jeśli została zarezerwowana.

Minimalne statusy pozycji:

```text
Do kompletacji -> W kompletacji -> Spakowana -> Przypisana do palety
```

Należy przewidzieć kontrolowane zwolnienie rezerwacji i cofnięcie błędnej operacji wraz ze śladem audytowym.

### Dwa tryby pracy zamówienia

1. **Przygotowanie zamówienia** — uprawniony użytkownik wskazuje zamawiającego i termin realizacji, dodaje towary oraz ilości, a następnie udostępnia zamówienie do kompletacji.
2. **Kompletacja współdzielona** — wiele osób pracuje nad jednym zamówieniem. Użytkownik wybiera dostępną pozycję i rezerwuje ją dla siebie przed rozpoczęciem pakowania.

Rezerwacja pozycji musi być atomowa i odporna na równoczesne żądania. W danym momencie pozycję może kompletować tylko jeden użytkownik. Pozostali użytkownicy muszą od razu widzieć aktualny stan lub otrzymać jednoznaczną informację, że pozycja została już zajęta.

### Paletyzacja

- Paletę tworzy się ze spakowanych pozycji zamówienia.
- Do palety można dodać wybrane pozycje, które nie zostały wcześniej przypisane do innej palety.
- Paleta zawiera własny identyfikator lub numer, przypisane pozycje oraz masę pustej palety (tarę).
- Masa towarów jest sumą `masa jednostkowa × ilość na palecie`.
- Masa całkowita jest sumą masy towarów i masy pustej palety.
- Zamknięcie palety wymaga obecności masy jednostkowej dla każdej przypisanej pozycji i nieujemnej masy palety.
- Jedna pozycja zamówienia może być pakowana częściowo i docelowo dzielona pomiędzy kilka palet. Przypisania do palet muszą przechowywać ilość, a ich suma nie może przekroczyć ilości spakowanej.

### Etykieta palety

Po zamknięciu palety aplikacja umożliwia przygotowanie etykiety do wydruku. Etykieta powinna zawierać co najmniej:

- numer zamówienia i numer palety,
- zamawiającego,
- listę pozycji z nazwami i ilościami,
- masę towarów, masę pustej palety i masę całkowitą.

Format etykiety, rozmiar papieru, technologia generowania oraz obsługiwane drukarki wymagają osobnej decyzji. Generowanie danych etykiety należy oddzielić od integracji z urządzeniem drukującym.

Wstępny opis interfejsu znajduje się w `docs/zarys-ekranow.md`.

## Najważniejsze zasady

- Nie zapisuj bezpośrednio do bazy danych Subiekta GT, jeżeli zadanie nie mówi o tym jednoznacznie i nie ma zaakceptowanej decyzji architektonicznej.
- Integracja z Subiektem służy w tym zakresie wyłącznie do odczytu katalogu towarów oraz minimalnego katalogu kontrahentów do powiązania klienta.
- Zamówienia, kompletacja, palety i audyt są przechowywane w PostgreSQL, w bazie aplikacji oddzielonej od bazy Subiekta.
- Nie mieszaj logiki biznesowej z kontrolerami HTTP ani z EF Core.
- Nie twórz skrótów typu „byle działało”, które omijają warstwy projektu.
- Nie zapisuj haseł, connection stringów ani danych produkcyjnych w repozytorium.
- Operacje rezerwowania pozycji, przypisywania do palety i zamykania palety muszą uwzględniać współbieżność.
- Zmiany statusów i mas mają być walidowane w modelu domenowym lub przypadkach użycia, a nie tylko w interfejsie użytkownika.

## Architektura backendu

Backend stosuje Clean Architecture:

- `SubiektMobile.Domain` — model domenowy i reguły biznesowe aplikacji,
- `SubiektMobile.Application` — przypadki użycia, DTO, kontrakty portów, CQRS,
- `SubiektMobile.Infrastructure` — EF Core, mapowanie tabel Subiekta GT, baza aplikacji i integracje techniczne,
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

Model domenowy aplikacji nie może zależeć od struktury tabel Subiekta. Mapowanie danych towarowych na modele aplikacji odbywa się na granicy Infrastructure/Application.

## Kolejność prac

1. Uporządkować dokumentację i model domenowy; magazynem danych aplikacji jest PostgreSQL.
2. Zaimplementować bezpieczny podgląd i wyszukiwanie towarów z Subiekta GT.
3. Zaimplementować użytkowników oraz minimalne role i uprawnienia.
4. Zaimplementować tworzenie i edycję zamówień w aplikacji.
5. Zaimplementować udostępnienie zamówienia i wieloosobową kompletację z kontrolą współbieżności.
6. Zaimplementować tworzenie i zamykanie palet oraz obliczanie ich masy.
7. Zaimplementować generowanie i wydruk etykiet palet.
8. Dodać historię operacji, obsługę wyjątków procesu i funkcje dodatkowe.

## Decyzje wymagające doprecyzowania

- źródło masy jednostkowej oraz zasady jej korekty, gdy Subiekt jej nie zawiera,
- dane i sposób wyboru zamawiającego,
- dokładne role i uprawnienia,
- format, rozmiar i technologia wydruku etykiety,
- sposób aktualizacji interfejsu przy pracy wielu osób (np. polling albo komunikacja czasu rzeczywistego),
- zasady anulowania zamówienia, ponownego otwierania palety i korekt po wydruku.

Do czasu podjęcia decyzji nie należy utrwalać przypadkowych założeń w schemacie bazy ani publicznym kontrakcie API.

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
- dla nowych funkcji stosuj osobne przypadki użycia w Application,
- oddzielaj modele odczytowe Subiekta od encji zapisywanych w bazie aplikacji,
- uwzględniaj idempotencję i konflikty współbieżności w operacjach kompletacji,
- nie implementuj nierozstrzygniętych decyzji biznesowych bez jawnego założenia zaakceptowanego w zadaniu.
- każdy nowy lub zmieniany tekst interfejsu dodawaj przez istniejący mechanizm i18n we wszystkich obsługiwanych językach; nie tłumacz danych biznesowych zwracanych przez API, takich jak nazwy produktów.

Po zmianie:

- uruchom `dotnet build backend/SubiektMobile.slnx`, jeśli środowisko ma właściwe SDK,
- uruchom testy, jeżeli istnieją,
- opisz, czego nie dało się sprawdzić lokalnie.

## Definition of Done

Zmiana jest gotowa dopiero wtedy, gdy:

- kod kompiluje się,
- nie narusza zależności między warstwami,
- nie wprowadza bezpośredniego zapisu do bazy Subiekta bez decyzji architektonicznej,
- operacje wieloosobowe są zabezpieczone przed utratą aktualizacji i podwójnym przypisaniem,
- reguły statusów, ilości i mas są pokryte testami,
- operacje zmieniające stan zapisują autora i czas,
- nie ujawnia sekretów,
- README albo dokumentacja zostały zaktualizowane, jeżeli zmieniło się zachowanie aplikacji,
- testy zostały dodane lub zaktualizowane, jeśli zmiana dotyczy logiki biznesowej.
