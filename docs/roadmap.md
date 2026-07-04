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
- [ ] Dodać pierwszą migrację po zatwierdzeniu modelu pierwszego zapisywanego modułu.
- [ ] Przygotować podstawowe testy architektoniczne.
- [ ] Ustalić standard obsługi czasu, mas i zaokrągleń.

## Etap 1 — katalog towarów

Cel: użytkownik może znaleźć towar z bazy Subiekta i zobaczyć dane wymagane do utworzenia zamówienia.

Zakres minimalny:

- [ ] Lista towarów.
- [ ] Wyszukiwanie po nazwie, symbolu lub kodzie.
- [ ] Paginacja.
- [ ] Szczegóły towaru.
- [ ] Jednostka miary.
- [ ] Masa jednostkowa lub informacja o jej braku.

Warunki techniczne:

- połączenie z Subiektem jest tylko do odczytu,
- odczyt przechodzi przez Application,
- EF Core i nazwy tabel pozostają w Infrastructure,
- API zwraca DTO, nie encje EF Core.

## Etap 2 — użytkownicy i uprawnienia

Cel: każda operacja biznesowa ma zidentyfikowanego wykonawcę, a tworzenie zamówień jest ograniczone uprawnieniem.

Zakres minimalny:

- [ ] Uwierzytelnianie użytkownika.
- [ ] Uprawnienie do tworzenia i udostępniania zamówień.
- [ ] Uprawnienie do kompletacji.
- [ ] Audyt autora i czasu mutacji.
- [ ] Serwerowa kontrola dostępu do każdego przypadku użycia.

## Etap 3 — tworzenie zamówień

Cel: uprawniony użytkownik tworzy zamówienie z towarów odczytanych z Subiekta.

Zakres minimalny:

- [ ] Lista i szczegóły zamówień.
- [ ] Zamawiający.
- [ ] Termin realizacji.
- [ ] Dodawanie i usuwanie pozycji.
- [ ] Ilość, jednostka i masa jednostkowa pozycji.
- [ ] Migawka nazwy towaru.
- [ ] Zapis wersji roboczej.
- [ ] Walidacja i udostępnienie zamówienia do kompletacji.

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
