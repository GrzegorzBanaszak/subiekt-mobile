# Roadmapa funkcjonalna

## Założenie główne

Projekt nie powinien zaczynać od skomplikowanych procesów zapisu i generowania dokumentów. Najpierw trzeba zbudować stabilny podgląd danych z Subiekta GT, ponieważ te same dane będą później potrzebne do kompletowania zamówień i generowania zamówień do dostawcy.

## Etap 0 — fundamenty

- [ ] Uporządkować dokumentację projektu.
- [ ] Dodać instrukcje dla Codex w `AGENTS.md`.
- [ ] Opisać architekturę w `docs/architecture.md`.
- [ ] Ustalić zasadę: brak bezpośredniego zapisu do bazy Subiekta GT bez decyzji architektonicznej.
- [ ] Przygotować podstawowe testy architektoniczne.

## Etap 1 — podgląd towarów

Cel: użytkownik może znaleźć towar i zobaczyć jego podstawowe dane.

Zakres minimalny:

- [ ] Lista towarów.
- [ ] Wyszukiwanie po nazwie, symbolu lub kodzie.
- [ ] Szczegóły towaru.
- [ ] Podstawowe dane identyfikacyjne.
- [ ] Dane magazynowe, jeżeli są dostępne w rozpoznanej strukturze bazy.
- [ ] Dane cenowe, jeżeli są dostępne i bezpieczne do pokazania.

Warunki techniczne:

- odczyt przez Application,
- EF Core tylko w Infrastructure,
- API zwraca DTO, nie encje EF Core,
- listy mają paginację.

## Etap 2 — podgląd zamówień od klientów

Cel: użytkownik może zobaczyć zamówienia klienta, które później będą podstawą kompletacji.

Zakres minimalny:

- [ ] Lista zamówień od klientów.
- [ ] Filtrowanie po dacie, numerze dokumentu i kontrahencie.
- [ ] Szczegóły zamówienia.
- [ ] Pozycje zamówienia.
- [ ] Ilości, jednostki, nazwy towarów.
- [ ] Status albo informacja pozwalająca odróżnić dokumenty aktywne od zakończonych.

Warunki techniczne:

- zamówienie z Subiekta jest mapowane na model odczytowy aplikacji,
- kontroler nie zna nazw tabel Subiekta,
- brak zapisu do bazy Subiekta.

## Etap 3 — podgląd przyjęć magazynowych

Cel: użytkownik może przejrzeć dokumenty przyjęć magazynowych.

Zakres minimalny:

- [ ] Lista przyjęć magazynowych.
- [ ] Szczegóły dokumentu.
- [ ] Pozycje dokumentu.
- [ ] Powiązane towary.
- [ ] Data, numer, kontrahent lub dostawca, jeżeli są dostępne.

## Etap 4 — kompletowanie zamówienia klienta

Cel: użytkownik pobiera zamówienie klienta z Subiekta i kompletuje je w aplikacji.

Zakres minimalny:

- [ ] Utworzenie sesji kompletacji na podstawie zamówienia klienta.
- [ ] Lista pozycji do skompletowania.
- [ ] Odhaczanie pozycji jako skompletowanych.
- [ ] Obsługa ilości skompletowanej częściowo.
- [ ] Status sesji kompletacji.
- [ ] Raport kompletacji.

Ważne zasady:

- sesja kompletacji jest modelem aplikacji, nie dokumentem Subiekta,
- pierwotne zamówienie klienta jest źródłem danych,
- raport kompletacji nie powinien modyfikować zamówienia w Subiekcie bez osobnej decyzji.

## Etap 5 — zamówienie do dostawcy jako EPP / EDI++

Cel: użytkownik przygotowuje zamówienie do dostawcy, a aplikacja generuje plik importowany do Subiekta GT.

Zakres minimalny:

- [ ] Projekt zamówienia do dostawcy.
- [ ] Dodawanie pozycji towarowych.
- [ ] Walidacja wymaganych danych.
- [ ] Generowanie pliku EPP / EDI++.
- [ ] Pobranie pliku przez użytkownika.
- [ ] Instrukcja importu do Subiekta GT.

Ważne zasady:

- brak bezpośredniego dodawania dokumentu do bazy Subiekta,
- format pliku powinien być osobno przetestowany na testowej bazie Subiekta,
- generator EPP / EDI++ powinien mieć testy jednostkowe.

## Etap 6 — funkcje dodatkowe

- [ ] Skanowanie kodów kreskowych.
- [ ] Historia kompletacji.
- [ ] Role i uprawnienia.
- [ ] Eksport raportów do PDF / CSV.
- [ ] Obsługa pracy offline.
- [ ] Audyt operacji użytkownika.
