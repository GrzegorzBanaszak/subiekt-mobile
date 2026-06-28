# Subiekt Mobile — frontend

Frontend aplikacji jest oparty na React, Vite i TypeScript. Struktura kodu jest
feature-based, a kontrakty API są generowane z dokumentu OpenAPI backendu.

## Uruchomienie

```powershell
npm install
npm run dev
```

Serwer developerski działa pod `http://127.0.0.1:5173` i przekazuje żądania
`/api` do backendu pod `http://localhost:5118`.

## Struktura

```text
src/
├── app/                 # router i providerzy aplikacji
├── api/                 # wspólny, typowany klient HTTP i schema OpenAPI
├── features/            # moduły biznesowe
├── shared/              # współdzielone komponenty, strony i style
├── test/                # konfiguracja testów
└── types/               # pozostałe typy współdzielone
```

Pierwszym modułem biznesowym jest `features/products`, przeznaczony na listę i
szczegóły towarów.

## Kontrakt API

Przy uruchomionym backendzie typy można odświeżyć poleceniem:

```powershell
npm run api:types
```

Nie należy ręcznie duplikować typów odpowiedzi API. Wszystkie requesty powinny
korzystać ze wspólnego klienta z `src/api/client.ts`.

## Weryfikacja

```powershell
npm run lint
npm test
npm run build
```
