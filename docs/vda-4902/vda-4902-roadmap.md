# Roadmapa wdrożenia dostaw VDA 4902

## Założenie główne

Subiekt GT pozostaje źródłem danych katalogowych i magazynowych. Subiekt Mobile prowadzi proces logistyczny wymagany dla dostaw VDA 4902: od zamówienia klienta, przez wysyłkę i pakowanie, do etykiet Single oraz Master.

Pierwszy pilotaż obejmuje jednego odbiorcę, jeden zakład odbiorczy i jeden zatwierdzony profil etykiety. Szkice interfejsu oraz wstępny układ etykiety są poglądowe; końcowe pola i format wymagają potwierdzenia przez odbiorcę.

## Zasady prowadzenia zmian

- Dane procesowe, konfiguracja klienta, palety, opakowania i historia wydruków są prowadzone w Subiekt Mobile.
- Z Subiekt GT wykorzystujemy przede wszystkim nazwę, symbol, kod towaru, jednostkę, masę jednostkową, stan oraz dostępność.
- Numer dokumentu dostawy `N` jest osobnym polem: może zostać wpisany przy zamówieniu klienta, a na wysyłce jest potwierdzany jako wartość obowiązująca na etykiecie.
- W MVP jedno opakowanie Single zawiera jedną część klienta, jedną partię i jeden typ opakowania.
- Wydrukowana etykieta jest migawką danych; korekta fizycznego opakowania lub palety nie zmienia historii wydruku.

## Stan wyjściowy

Poniższe elementy obecnej aplikacji pozostają podstawą kolejnych etapów:

- [x] Katalog towarów odczytywany z Subiekt GT.
- [x] Lista, szczegóły i statusy obecnych zamówień.
- [x] Kompletacja zamówień.
- [x] Paletyzacja, masy oraz historia wydruków obecnej etykiety palety.
- [x] Użytkownicy, uprawnienia i audyt operacji.

## Etap 0 — potwierdzenie pilotażu i reguł odbiorcy

**Cel:** uzgodnić minimalny, kompletny proces dla pierwszego odbiorcy.

Zakres:

- [ ] Wybrać odbiorcę i zakład odbiorczy do pilotażu.
- [ ] Potwierdzić wymagane pola VDA 4902 dla Single i Master.
- [ ] Ustalić źródło i sposób nadawania numeru dokumentu dostawy `N`.
- [ ] Ustalić numer dostawcy `V`, dock, typy opakowań, tary i wymagania pakowania.
- [ ] Potwierdzić zasady partii `H`, numeru części klienta `P` oraz ilości `Q`.
- [ ] Zatwierdzić format etykiety, drukarkę i przykładowe dane testowe.

**Warunek przejścia:** zatwierdzona karta wymagań pilotażu oraz wzorzec testowego zamówienia.

## Etap 1 — porządkowanie procesu i nazewnictwa

**Cel:** rozdzielić zamówienie klienta od pracy magazynu, nie przerywając obecnej obsługi zamówień.

Zakres:

- [ ] Zmienić nazwę obecnego modułu „Zamówienia” na „Zamówienia magazynowe”.
- [ ] Dodać w nawigacji moduły „Klienci”, „Zamówienia klientów” i „Wysyłki”.
- [ ] Ustalić statusy zamówienia klienta, zamówienia magazynowego, wysyłki, opakowania i palety.
- [ ] Określić przejście istniejących zamówień do nowego procesu bez utraty danych.
- [ ] Ujednolicić widoki list, szczegółów i historii dla nowych etapów procesu.

**Warunek przejścia:** użytkownik rozumie różnicę między zamówieniem klienta, zamówieniem magazynowym i wysyłką; obecny proces pozostaje dostępny.

## Etap 2 — klienci, zakłady i profile logistyczne

**Cel:** przechowywać dane odbiorcy potrzebne do dostaw VDA, których nie zapewnia katalog Subiekt GT.

Zakres:

- [ ] Utworzyć listę klientów z aktywnością i liczbą zakładów odbiorczych.
- [ ] Dodać szczegóły klienta oraz historię jego zamówień.
- [ ] Dodać wiele zakładów odbiorczych dla jednego klienta.
- [ ] Zdefiniować profil logistyczny zakładu: adres, dock, godziny przyjęć, numer dostawcy `V`, wymagania pakowania i profil etykiety VDA.
- [ ] Dodać domyślny typ palety, limity i wymagania zabezpieczenia ładunku, jeżeli są wymagane przez odbiorcę.
- [ ] Umożliwić wybór profilu zakładu zamiast ręcznego przepisywania jego danych.

**Warunek przejścia:** można skonfigurować klienta z co najmniej dwoma zakładami, a wybór zakładu podpowiada dane logistyczne.

## Etap 3 — słowniki opakowań i mapowanie części klienta

**Cel:** połączyć język odbiorcy z kartoteką magazynową bez zmieniania danych towaru w Subiekt GT.

Zakres:

- [ ] Dodać słownik typów opakowań: kod, nazwa, tara, pojemność i kod klienta.
- [ ] Dodać mapowanie numeru części klienta `P` na towar/symbol wewnętrzny.
- [ ] Umożliwić mapowanie w kontekście klienta lub konkretnego zakładu.
- [ ] Pokazać na zamówieniu status: zmapowane / wymaga uwagi.
- [ ] Oznaczać brak mapowania przed utworzeniem zamówienia magazynowego.
- [ ] Obsłużyć opcjonalną zmianę konstrukcyjną, jeżeli wymaga jej profil odbiorcy.

**Warunek przejścia:** numer części klienta jednoznacznie wskazuje produkt magazynowy, a brak mapowania jest widoczny przed realizacją.

## Etap 4 — zamówienia klientów

**Cel:** wprowadzić dokument źródłowy, z którego powstaje praca magazynowa.

Zakres:

- [ ] Dodać listę, filtrowanie, szczegóły i statusy zamówień klientów.
- [ ] Umożliwić wybór klienta oraz zakładu odbiorczego.
- [ ] Dodać pozycje z numerem części klienta, towarem z mapowania, ilością i wymaganym opakowaniem.
- [ ] Dodać termin realizacji oraz uwagi klienta.
- [ ] Dodać osobne pole numeru dokumentu dostawy `N`, a nie wyłącznie uwagi lub podtytuł.
- [ ] Pokazać kontrolę kompletności mapowań, danych zakładu i wymagań pakowania.
- [ ] Udostępnić akcję „Utwórz zamówienie magazynowe”.

**Warunek przejścia:** kompletne zamówienie klienta można przetworzyć tylko raz na zamówienie magazynowe; `N` i wymagania odbiorcy są przekazane dalej.

## Etap 5 — zamówienie magazynowe i wysyłka

**Cel:** przekształcić zapotrzebowanie klienta w kontrolowaną instrukcję pracy magazynu i dostawy.

Zakres:

- [ ] Tworzyć zamówienie magazynowe z pozycji zamówienia klienta.
- [ ] Zachować powiązanie z klientem, zakładem, numerem `N` i pozycjami źródłowymi.
- [ ] Wykorzystać dostępność oraz dane towaru z Subiekt GT.
- [ ] Utworzyć wysyłkę powiązaną z zamówieniem magazynowym.
- [ ] Na wysyłce potwierdzić datę wysyłki, `N`, `V`, dock oraz profil etykiety.
- [ ] Pokazać listę brakujących danych i blokować pakowanie, gdy wymagania odbiorcy nie są spełnione.
- [ ] Zapisać migawkę wybranego profilu logistycznego dla wysyłki.

**Warunek przejścia:** wysyłka ma komplet danych potrzebnych do oznaczenia opakowania i palety, zanim magazynier zacznie pakowanie.

## Etap 6 — nowy przebieg kompletacji i pakowanie Single

**Cel:** odwzorować fizyczne opakowanie i przypisać je do palety już podczas pakowania.

Zakres:

- [ ] Utworzyć otwartą paletę Master przed pakowaniem towaru.
- [ ] Podczas kompletacji wybrać paletę docelową albo utworzyć nową.
- [ ] Utworzyć opakowanie Single z towarem, ilością, partią, typem opakowania i masą.
- [ ] Zablokować przekroczenie ilości pozostałej do spakowania.
- [ ] Umożliwić podział jednej pozycji na wiele opakowań i palet.
- [ ] Pokazać postęp: zamówiono, spakowano, na paletach i pozostało.
- [ ] Zamknąć opakowanie po potwierdzeniu fizycznej zawartości.
- [ ] Udostępnić podgląd i wydruk etykiety VDA Single po zamknięciu opakowania.

**Warunek przejścia:** każde zamknięte opakowanie ma unikalny numer Single, znaną zawartość, partię, masę i paletę nadrzędną.

## Etap 7 — zamknięcie palety Master i etykieta zbiorcza

**Cel:** uzyskać kompletną, wiarygodną jednostkę wysyłkową dla palety.

Zakres:

- [ ] Pokazać zawartość palety: opakowania Single, towary, partie, ilości i masy.
- [ ] Wyliczać masę towarów, tarę oraz masę całkowitą palety.
- [ ] Powiązać paletę z wysyłką, dokumentem dostawy i punktem rozładunku.
- [ ] Kontrolować kompletność palety przed zamknięciem.
- [ ] Po zamknięciu nadawać trwały numer Master.
- [ ] Udostępnić podgląd i wydruk etykiety VDA Master.
- [ ] Udostępnić listę pakunkową PDF, jeżeli wymaga jej pilotaż.
- [ ] Zapisać historię pierwszego wydruku i reprintów dla Single oraz Master.

**Warunek przejścia:** zamknięta paleta ma pełną zawartość, poprawne masy, powiązanie z wysyłką i możliwy do odtworzenia wydruk Master.

## Etap 8 — profile VDA 4902 i kontrola jakości wydruku

**Cel:** zapewnić zgodność wydruku z potwierdzonym profilem konkretnego odbiorcy.

Zakres:

- [ ] Dodać konfigurowalne profile pól obowiązkowych i opcjonalnych dla odbiorcy.
- [ ] Obsłużyć co najmniej dane: odbiorca, dock, `N`, `P`, `Q`, `V`, `S/M`, `H`, liczba opakowań i masy.
- [ ] Obsłużyć format i rodzaj kodu kreskowego zaakceptowany przez odbiorcę.
- [ ] Walidować kompletność danych bezpośrednio przed wygenerowaniem dokumentu.
- [ ] Zapisywać niezmienną migawkę wydanej etykiety oraz osobę wydającą.
- [ ] Porównać wydruki testowe z próbkami i wymaganiami odbiorcy.
- [ ] Uzyskać akceptację wydruku pilotażowego.

**Warunek przejścia:** wydruk Single i Master przechodzi uzgodniony test odbiorcy oraz może być powtórzony bez zmiany danych historycznych.

## Etap 9 — pilotaż operacyjny i stabilizacja

**Cel:** przeprowadzić realną dostawę dla jednego odbiorcy i ustabilizować proces.

Zakres:

- [ ] Przeprowadzić pełny scenariusz: zamówienie klienta → zamówienie magazynowe → wysyłka → opakowania Single → paleta Master → wydruk.
- [ ] Przetestować ilości częściowe, wiele opakowań, wiele palet oraz reprint.
- [ ] Przetestować sytuacje brzegowe: brak mapowania, brak partii, brak `N`, niewłaściwa masa i niekompletna paleta.
- [ ] Przeszkolić użytkowników biurowych i magazynowych.
- [ ] Zebrać uwagi odbiorcy i użytkowników.
- [ ] Ustalić reguły korekt: anulowanie opakowania, ponowne otwarcie palety, anulowanie wydruku oraz reprint.
- [ ] Zatwierdzić gotowość do obsługi kolejnych zakładów lub klientów.

**Warunek przejścia:** co najmniej jedna rzeczywista dostawa została wykonana zgodnie z uzgodnionym procesem i zaakceptowana przez odbiorcę.

## Etap 10 — rozwój po pilotażu

- [ ] Rozszerzyć konfigurację o kolejne zakłady i profile odbiorców.
- [ ] Dodać skanowanie kodów kreskowych towarów, partii i jednostek Single/Master.
- [ ] Dodać raporty kompletności, mas, wydruków i błędów mapowania.
- [ ] Rozważyć integrację EDI/ASN, jeżeli wymaga jej odbiorca.
- [ ] Rozważyć pracę offline dla magazynu.
- [ ] Dodać monitoring procesów i rozszerzony audyt.
- [ ] Obsłużyć mieszane opakowania lub palety wyłącznie po odrębnym uzgodnieniu z odbiorcą.

## Zależności między etapami

```text
Etap 0 → Etap 1 → Etap 2 → Etap 3 → Etap 4 → Etap 5 → Etap 6 → Etap 7 → Etap 8 → Etap 9
```

- Etap 3 wymaga skonfigurowanego klienta i zakładu z etapu 2.
- Etap 4 wymaga mapowania towarów z etapu 3.
- Etap 5 wymaga kompletnego zamówienia klienta z etapu 4.
- Etapy 6–8 wymagają kompletnej wysyłki z etapu 5.
- Etap 9 uruchamiamy dopiero po zatwierdzeniu wydruków z etapu 8.

## Miary powodzenia pilotażu

- 100% opakowań Single ma przypisaną paletę, numer części klienta, ilość i partię.
- 100% palet Master ma powiązaną wysyłkę, masę oraz historię wydruku.
- Brak wydruku etykiety przy niekompletnych danych wymaganych przez profil odbiorcy.
- Brak ręcznego przepisywania powtarzalnych danych zakładu i numeru dostawcy.
- Każdy reprint jest rozróżnialny i widoczny w historii.
