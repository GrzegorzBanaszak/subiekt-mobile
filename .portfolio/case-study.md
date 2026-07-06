# Subiekt Mobile

## Cel projektu

Celem projektu było przygotowanie aplikacji webowej oraz API wspierających pracę z danymi systemu Subiekt GT.

Projekt ma być rozwijany jako narzędzie ułatwiające podgląd danych, obsługę wybranych procesów oraz dalszą automatyzację pracy z systemem magazynowo-sprzedażowym.

## Problem

Praca bezpośrednio na danych systemu Subiekt GT jest niewygodna dla użytkownika końcowego i wymaga znajomości struktury bazy danych.

Potrzebne było rozwiązanie, które pozwoli oddzielić logikę aplikacji od interfejsu użytkownika i przygotuje fundament pod dalsze funkcje.

## Decyzja techniczna

Zdecydowałem się na architekturę z osobnym backendem API oraz frontendem webowym.

Backend odpowiada za komunikację z danymi i logikę aplikacji, natomiast frontend odpowiada za prezentację danych użytkownikowi.

## Architektura

![Schemat architektury](./images/architecture.png)

Główne elementy:

- frontend webowy;
- backend API;
- baza danych SQL Server;
- konfiguracja środowiskowa;
- możliwość dalszego uruchamiania w Dockerze lub na serwerze lokalnym.

## Implementacja

W ramach projektu przygotowano:

- strukturę backendu API;
- strukturę frontendu;
- podstawową komunikację między warstwami;
- konfigurację połączenia z SQL Server;
- podstawy pod dalsze funkcje aplikacji.

## Technologie

- .NET
- ASP.NET Core
- Next.js
- React
- TypeScript
- SQL Server
- Docker

## Rezultat

Powstał fundament aplikacji, który można dalej rozwijać o kolejne moduły związane z obsługą danych, zamówień i procesów biznesowych.

## Czego się nauczyłem

- Jak projektować aplikację rozdzieloną na frontend i backend.
- Jak przygotować API pod dane biznesowe.
- Jak dokumentować projekt techniczny.
- Jak rozwijać projekt etapami bez przepisywania całej architektury.

## Status

Projekt jest w trakcie rozwoju.
