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

Implementuj w następującej kolejności:

1. Lista i szczegóły towarów pobieranych z Subiekta.
2. Logowanie oraz obsługa uprawnień zwracanych przez API.
3. Lista, tworzenie i szczegóły zamówień należących do aplikacji.
4. Współdzielony ekran kompletacji z rezerwacją pozycji.
5. Tworzenie palety i podsumowanie masy.
6. Podgląd oraz wydruk etykiety palety.

Szczegółowy punkt wyjścia dla widoków znajduje się w `docs/zarys-ekranow.md`.

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
- Nie uznawaj rezerwacji pozycji ani przypisania do palety za zakończone przed potwierdzeniem przez API.
- Obsługuj konflikty współbieżności czytelnym komunikatem i odświeżeniem aktualnego stanu.
- Nie wyliczaj wiążącej masy palety wyłącznie po stronie klienta; prezentuj wynik zatwierdzony przez API.
- Ukrywaj niedostępne akcje dla wygody użytkownika, ale nie traktuj frontendu jako granicy autoryzacji.

## Komendy sprawdzające

Dopasuj do faktycznego menedżera pakietów projektu:

```bash
npm run lint
npm run build
npm test
```
