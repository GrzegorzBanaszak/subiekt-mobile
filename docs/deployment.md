# Wdrożenie Docker Compose za Cloudflare Tunnel

Ten wariant jest przeznaczony dla hosta Linux (np. kontenera LXC lub maszyny w Proxmoxie)
z zainstalowanymi Docker Engine i wtyczką Docker Compose.

## Wymagania sieciowe

- Host musi mieć dostęp do SQL Servera z bazą Subiekta GT na skonfigurowanym porcie.
- Konto SQL używane przez aplikację powinno mieć wyłącznie uprawnienia do odczytu.
- Host musi mieć wychodzący dostęp do Internetu dla `cloudflared` i pobierania obrazów.
- Nie trzeba przekierowywać żadnego portu na routerze. Compose nie publikuje PostgreSQL ani API na hoście.

Jeżeli Docker działa wewnątrz LXC, kontener Proxmox musi zezwalać na wymagane funkcje
konteneryzacji (typowo `nesting=1`). Szczegóły zależą od konfiguracji Proxmoxa.

## Konfiguracja

1. Skopiuj `.env.example` do `.env` i ustaw unikalne hasła, connection string Subiekta oraz
   token tunelu. Plik `.env` jest ignorowany przez Git. Na Linuxie ogranicz jego prawa:

   ```bash
   cp .env.example .env
   chmod 600 .env
   ```

2. W Cloudflare Zero Trust utwórz zdalnie zarządzany Tunnel i skopiuj jego token do
   `CLOUDFLARE_TUNNEL_TOKEN`.

3. W konfiguracji tunelu dodaj publiczny hostname:

   ```text
   Hostname: quiz-volt.pl
   Service type: HTTP
   URL: http://web:80
   ```

   Opcjonalny `www.quiz-volt.pl` dodaj jako drugi publiczny hostname kierujący do tego
   samego serwisu. Nazwa `web` działa, ponieważ `cloudflared` i frontend są w tej samej
   sieci Compose.

4. Uruchom wdrożenie:

   ```bash
   docker compose up -d --build
   docker compose ps
   docker compose logs -f migrate api web cloudflared
   ```

Serwis `migrate` najpierw wykonuje migracje PostgreSQL, a `data-protection-init` ustawia
uprawnienia trwałego wolumenu kluczy. Oba kończą pracę kodem `0` i dopiero potem uruchamia
się API. Frontend przekazuje ścieżki `/api/*` do API, dzięki czemu przeglądarka
korzysta z jednego originu i poprawnie obsługuje cookies sesji oraz CSRF.

Po pierwszym poprawnym uruchomieniu można usunąć trzy zmienne `BOOTSTRAP_ADMIN_*` z `.env`.
Nie zmieni to istniejącego konta root i ograniczy ekspozycję hasła bootstrapowego.

## Aktualizacja i kopia zapasowa

Przed aktualizacją wykonaj kopię bazy PostgreSQL. Następnie pobierz kod i przebuduj usługi:

```bash
docker compose exec -T application-db pg_dump -U subiekt_mobile -d subiekt_mobile -Fc > subiekt-mobile.dump
git pull --ff-only
docker compose up -d --build
```

Jeżeli zmieniono `APPLICATION_DB_USER` lub `APPLICATION_DB_NAME`, użyj tych wartości w
poleceniu `pg_dump`. Wolumeny `application-db-data` i `data-protection-keys` muszą pozostać
trwałe między aktualizacjami.
