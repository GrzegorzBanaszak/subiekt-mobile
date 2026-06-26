# Mobilna aplikacja dla obsługi danych Subiekt GT

## Funkcje aplikacji

1. Utworzenie nowego zamówienia w programie subiekt w pełni korzystając z aplikacji mobilnej
2. Pobranie zamówienia dla klienta i zkompletowanie go z użyciem aplikacji mobilnej.

## MVP

- [ ] 1. Aplikacja ma mieć możliwość wyświetlania listy towarów.
- [ ] 2. Wyświetlanie wszystkich informacji o towarze w podobnej strukturze jak w Subiekt GT

### Struktura

/subiekt-mobile
/backend

- ASP.NET Core Web API
- EF Core
- SQL Server

/frontend

- React + Vite + TypeScript
- Tailwind
- TanStack Query / Axios

#### Backend

```

backend
├── src
│ ├── SubiektMobile.Api
│ ├── SubiektMobile.Application
│ ├── SubiektMobile.Infrastructure
│ └── SubiektMobile.Domain
└── SubiektMobile.sln
```
