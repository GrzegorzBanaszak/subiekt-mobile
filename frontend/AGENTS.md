# frontend/AGENTS.md

## Status

Frontend jest planowany jako aplikacja webowa dla telefonu, tabletu i komputera.

Jeżeli katalog frontendu nie jest jeszcze zaimplementowany, traktuj ten plik jako docelowe zasady dla przyszłej części webowej.

## Architektura

Frontend powinien być prowadzony feature-based.

Preferowana struktura:

```text
frontend/
├── src/
│   ├── app/ albo pages/        # routing
│   ├── features/               # funkcje biznesowe
│   ├── shared/                 # komponenty i helpery współdzielone
│   ├── api/                    # klient API i konfiguracja requestów
│   └── types/                  # typy współdzielone
```

## Główne funkcje webowe

Najpierw zaimplementuj widoki podglądu:

1. Lista i szczegóły towarów.
2. Lista i szczegóły zamówień od klientów.
3. Lista i szczegóły przyjęć magazynowych.

Dopiero potem:

1. Ekran kompletowania zamówienia.
2. Raport kompletacji.
3. Ekran tworzenia zamówienia do dostawcy i pobierania pliku EPP / EDI++.

## Zasady

- Nie wykonuj requestów HTTP bezpośrednio w wielu komponentach.
- Używaj wspólnego klienta API.
- Komponenty UI nie powinny zawierać logiki biznesowej.
- Logikę formularzy i mapowania wydzielaj do feature lub hooków.
- Nie używaj `any`, jeżeli można zdefiniować typ.
- Nie duplikuj typów response z API.
- Nie dodawaj globalnego state management bez realnej potrzeby.
- Dla list używaj paginacji, filtrowania i stanów ładowania.
- Ekrany magazynowe projektuj pod obsługę mobilną i skaner kodów kreskowych.

## Komendy sprawdzające

Dopasuj do faktycznego menedżera pakietów projektu:

```bash
npm run lint
npm run build
npm test
```
