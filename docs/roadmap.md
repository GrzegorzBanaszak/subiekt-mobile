# Roadmapa funkcjonalna

## Założenie główne

Aplikacja odczytuje towary z bazy Subiekta GT, ale tworzy i przechowuje zamówienia we własnym modelu. Docelowy proces obejmuje przygotowanie zamówienia przez uprawnioną osobę, wieloosobową kompletację, paletyzację, obliczanie masy i wydruk etykiety.

Nie wolno zapisywać zamówień ani stanu kompletacji bezpośrednio do tabel Subiekta GT.

## Etap 0 — fundamenty

- [x] Uporządkować podstawową dokumentację projektu.
- [x] Dodać instrukcje dla Codex w `AGENTS.md`.
- [x] Opisać docelowy kierunek architektury w `docs/architecture.md`.
- [x] Przygotować wstępny zarys ekranów.
- [x] Wybrać PostgreSQL jako magazyn danych aplikacji.
- [x] Dodać osobny `ApplicationDbContext` i konfigurację Npgsql.
- [x] Dodać kontrolę dostępności bazy aplikacji do health checku.
- [x] Udostępnić OpenAPI i Swagger UI w środowisku developerskim.
- [x] Dodać pierwszą migrację po zatwierdzeniu modelu pierwszego zapisywanego modułu.
- [ ] Przygotować podstawowe testy architektoniczne.
- [ ] Ustalić standard obsługi czasu, mas i zaokrągleń.

## Etap 1 — katalog towarów

Cel: użytkownik może znaleźć towar z bazy Subiekta i zobaczyć dane wymagane do utworzenia zamówienia.

Zakres minimalny:

- [x] Lista towarów.
- [x] Wyszukiwanie po nazwie, symbolu lub kodzie.
- [x] Paginacja.
- [x] Szczegóły towaru.
- [x] Jednostka miary.
- [ ] Masa jednostkowa lub informacja o jej braku.

Warunki techniczne:

- połączenie z Subiektem jest tylko do odczytu,
- odczyt przechodzi przez Application,
- EF Core i nazwy tabel pozostają w Infrastructure,
- API zwraca DTO, nie encje EF Core.

## Etap 2 — użytkownicy i uprawnienia

Cel: każda operacja biznesowa ma zidentyfikowanego wykonawcę, a tworzenie zamówień jest ograniczone uprawnieniem.

Zakres minimalny:

- [x] Uwierzytelnianie administratora oraz sesja wybranego pracownika.
- [x] Uprawnienie do tworzenia i udostępniania zamówień.
- [x] Uprawnienie do kompletacji.
- [x] Audyt autora i czasu mutacji.
- [x] Serwerowa kontrola dostępu do każdego przypadku użycia.

Przyjęty model MVP:

- pierwszy administrator bootstrapowy jest stałym kontem root i jako jedyny zarządza kontami administratorów,
- zwykły administrator zarządza organizacjami i pracownikami, ale nie kontami administratorów,
- administratorzy logują się własnym loginem i hasłem,
- organizacja grupuje pracowników i nie jest wykonawcą operacji,
- pracownik jest wybierany publicznie bez hasła na wspólnym stanowisku,
- wybór pracownika tworzy odwoływalną sesję wskazującą konkretną osobę,
- publiczny wybór nie chroni przed podszyciem się pod innego pracownika i jest świadomym ograniczeniem MVP.

## Etap 3 — tworzenie zamówień

Cel: uprawniony użytkownik tworzy zamówienie z towarów odczytanych z Subiekta.

Zakres minimalny:

- [x] Lista i szczegóły zamówień.
- [x] Zamawiający.
- [x] Termin realizacji.
- [x] Dodawanie i usuwanie pozycji.
- [ ] Ilość, jednostka i masa jednostkowa pozycji — ilość i jednostka są gotowe; źródło masy wymaga decyzji.
- [x] Migawka nazwy towaru.
- [x] Zapis wersji roboczej.
- [x] Walidacja i udostępnienie zamówienia do kompletacji.

## Etap 4 — kompletacja współdzielona

Cel: wiele osób kompletuje różne pozycje jednego zamówienia bez podwójnego przypisania pracy.

Zakres minimalny:

- [ ] Lista zamówień gotowych do kompletacji.
- [ ] Wspólny widok pozycji i postępu.
- [ ] Atomowe zarezerwowanie pozycji przez użytkownika.
- [ ] Czytelna obsługa konfliktu równoczesnej rezerwacji.
- [ ] Zwolnienie pozycji.
- [ ] Oznaczenie pozycji jako spakowanej.
- [ ] Historia zmian statusów.
- [ ] Testy współbieżności i przejść statusów.

Zakres częściowej kompletacji należy ustalić przed zaprojektowaniem kontraktów API dla ilości spakowanej.

## Etap 5 — paletyzacja

Cel: użytkownik grupuje spakowane pozycje na paletach i otrzymuje wiarygodną masę całkowitą.

Zakres minimalny:

- [ ] Utworzenie palety w ramach zamówienia.
- [ ] Wprowadzenie masy pustej palety.
- [ ] Wybór spakowanych, nieprzypisanych pozycji.
- [ ] Atomowe przypisanie pozycji do palety.
- [ ] Obliczenie masy towarów.
- [ ] Obliczenie masy całkowitej.
- [ ] Walidacja brakujących lub niepoprawnych mas.
- [ ] Zamknięcie palety.
- [ ] Obsługa wielu palet w jednym zamówieniu.

Dzielenie jednej pozycji między palety należy rozstrzygnąć przed implementacją schematu danych.

## Etap 6 — etykiety palet

Cel: użytkownik może zweryfikować i wydrukować etykietę zamkniętej palety.

Zakres minimalny:

- [ ] Ustalenie rozmiaru i formatu etykiety.
- [ ] Dane zamawiającego, zamówienia i palety.
- [ ] Lista pozycji i ilości.
- [ ] Masa towarów, tara i masa całkowita.
- [ ] Podgląd przed wydrukiem.
- [ ] Wydruk lub pobranie dokumentu.
- [ ] Historia pierwszego i ponownego wydruku.
- [ ] Testy generatora etykiety.

## Etap 7 — stabilizacja i funkcje dodatkowe

- [ ] Aktualizacja współdzielonego widoku w czasie rzeczywistym.
- [ ] Obsługa porzuconych rezerwacji.
- [ ] Korekty i ponowne otwieranie procesu według ustalonych reguł.
- [ ] Skanowanie kodów kreskowych.
- [ ] Kody kreskowe lub QR na etykietach.
- [ ] Raporty i eksporty.
- [ ] Obsługa pracy offline.
- [ ] Rozszerzony audyt i monitoring.
