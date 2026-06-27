# Subiekt Mobile

Mobilna aplikacja wspierająca pracę z danymi programu Subiekt GT.

> Ten plik jest szkieletem dokumentacji projektu. Opisy umieszczone pod nagłówkami
> wskazują, jakie informacje należy dodać wraz z rozwojem aplikacji.

## Opis projektu

Aplikacja ma za zadanie obudować aplikacje Subiekt GT w funkcje pozwalajace na prace mobilną za pomocą telefonu lub tableta.

Główne założenia projektu:

- tworzenie nowych zamówień w Subiekcie GT z poziomu aplikacji mobilnej,
- pobieranie zamówień klienta i wspieranie procesu ich kompletowania,
- przeglądanie danych towarów.

## Status projektu

Aktualny status: **w trakcie tworzenia MVP**.

## Zakres MVP

Sekcja powinna zawierać minimalny zestaw funkcji wymaganych do uznania pierwszej
wersji aplikacji za gotową. Lista kontrolna ułatwia śledzenie postępu.

- [ ] Wyświetlanie listy towarów.
- [ ] Wyświetlanie szczegółowych informacji o towarze w strukturze zbliżonej do Subiekta GT.
- [ ] Pobieranie zamówień klienta.
- [ ] Obsługa kompletowania zamówienia.
- [ ] Tworzenie zamówienia w Subiekcie GT.

## Technologie

### Backend

- ASP.NET Core Web API (.NET 10),
- Entity Framework Core,
- SQL Server.

### Frontend (planowany)

- React, Vite i TypeScript,
- Tailwind CSS,
- TanStack Query i Axios.

## Architektura i struktura katalogów

Sekcja powinna opisywać podział odpowiedzialności między warstwami aplikacji
oraz wskazywać, gdzie znajduje się backend, frontend, dokumentacja i testy.

```text
subiekt-mobile/
├── backend/
│   ├── src/
│   │   ├── SubiektMobile.Api/             # API HTTP i konfiguracja aplikacji
│   │   ├── SubiektMobile.Application/     # logika przypadków użycia
│   │   ├── SubiektMobile.Infrastructure/  # baza danych i integracje
│   │   └── SubiektMobile.Domain/          # model domenowy i reguły biznesowe
│   └── SubiektMobile.slnx
├── docs/                                  # dokumentacja techniczna
└── Readme.md
```

Po utworzeniu frontendu należy dodać jego katalog i krótko opisać przyjętą
strukturę komponentów, widoków oraz komunikacji z API.

## Wymagania

Ta sekcja powinna wymieniać oprogramowanie wymagane na komputerze programisty,
na przykład odpowiednią wersję .NET SDK, SQL Server, Node.js i dostęp do bazy
Subiekta GT.

Do uzupełnienia przed udostępnieniem projektu innym osobom:

- obsługiwane systemy operacyjne,
- wymagana wersja SQL Server,
- wymagana wersja Node.js po dodaniu frontendu,
- wymagane uprawnienia do bazy Subiekta GT.

## Instalacja

W tej sekcji powinny znaleźć się kompletne polecenia prowadzące od sklonowania
repozytorium do zainstalowania zależności backendu i frontendu. Kroki powinny
działać na czystym środowisku.

Przykładowy zakres instrukcji:

1. Sklonowanie repozytorium.
2. Przywrócenie pakietów NuGet.
3. Instalacja zależności frontendu.
4. Przygotowanie konfiguracji lokalnej.
5. Uruchomienie aplikacji.

## Konfiguracja

Sekcja powinna opisywać wszystkie wymagane ustawienia bez publikowania haseł
ani innych sekretów. Należy podać nazwy kluczy konfiguracyjnych, sposób ustawienia
ich lokalnie i przykładowe, nieprawdziwe wartości.

Backend wymaga ciągu połączenia o nazwie `ConnectionStrings:SubiektGt`.
W środowisku deweloperskim sekret można ustawić poleceniem:

```powershell
dotnet user-secrets set "ConnectionStrings:SubiektGt" "Server=NAZWA_SERWERA;Database=NAZWA_BAZY;..." --project backend/src/SubiektMobile.Api
```

Nie należy zapisywać prawdziwego ciągu połączenia w repozytorium.

## Uruchomienie projektu

Ta sekcja powinna zawierać dokładne polecenia uruchamiające backend i frontend,
adresy lokalnych usług oraz informację, w jakiej kolejności je uruchomić.

Backend można uruchomić poleceniem:

```powershell
dotnet run --project backend/src/SubiektMobile.Api
```

Po uruchomieniu dostępny jest endpoint diagnostyczny `GET /health`. Adres i port
API zależą od lokalnego profilu uruchomieniowego.

## Dokumentacja API

W tej sekcji należy opisać udostępnione endpointy, ich przeznaczenie, wymagane
parametry, przykładowe odpowiedzi oraz możliwe błędy. Można również podać adres
dokumentacji OpenAPI generowanej w środowisku deweloperskim.

## Integracja z Subiektem GT

Sekcja powinna wyjaśniać sposób komunikacji z Subiektem GT: używane tabele,
mechanizm zapisu i odczytu, ograniczenia integracji oraz wymagane uprawnienia.
Należy też zaznaczyć, które operacje są tylko odczytem, a które modyfikują dane.

Dokumentacja rozpoznanej struktury danych znajduje się w katalogu `docs/`.

## Testy

W tej sekcji powinny znaleźć się polecenia uruchamiające testy jednostkowe,
integracyjne i frontendowe. Warto również opisać wymagania testów korzystających
z bazy danych oraz sposób przygotowania danych testowych.

## Bezpieczeństwo

Sekcja powinna opisywać sposób uwierzytelniania i autoryzacji, ochronę danych
dostępowych, zasady dostępu do bazy oraz sposób zgłaszania podatności. Jest to
szczególnie ważne, ponieważ aplikacja będzie operować na danych handlowych.

## Plan rozwoju

W tej sekcji należy umieszczać funkcje planowane po MVP, najlepiej w kolejności
priorytetów. Szczegółowe zadania techniczne lepiej prowadzić w systemie zgłoszeń.

Przykładowe dalsze kierunki:

- skanowanie kodów kreskowych,
- częściowa kompletacja zamówienia,
- historia operacji,
- obsługa pracy przy chwilowym braku sieci,
- role i uprawnienia użytkowników.

## Współtworzenie projektu

Sekcja powinna opisywać zasady zgłaszania zmian: sposób tworzenia gałęzi,
konwencję commitów, wymagane testy, formatowanie kodu i proces przeglądu zmian.

## Licencja

W tej sekcji należy podać licencję projektu albo jednoznacznie zaznaczyć, że kod
jest prywatny i nie może być kopiowany ani rozpowszechniany bez zgody właściciela.

## Autorzy i kontakt

Sekcja powinna wskazywać właściciela projektu, osoby odpowiedzialne za jego
utrzymanie oraz preferowany sposób zgłaszania pytań i problemów.
