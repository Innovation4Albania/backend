# User Endpoint Authorization

Ky dokument pershkruan rregullat e autorizimit per endpoint-et e llogarive te perdoruesve.

## Parime

- `admin` eshte i vetmi rol qe menaxhon llogari perdoruesish.
- Roli `drejtor_agjencie` dhe `drejtor_inovacioni_publik` mund te lexojne listen e llogarive vetem per te zgjedhur anetare ne grupet e punes te projekteve.
- Rolet qe lexojne listen per grup pune nuk mund te krijojne, modifikojne, aktivizojne, caktivizojne, fshijne apo ndryshojne fjalekalime te llogarive.
- Endpoint-et `me` jane per llogarine aktuale dhe nuk konsiderohen administrim global.

## Helper-at e autorizimit

- `ApplicationRoles.CanManageUsers(role)`
  Lejon vetem `admin`. Perdoret per veprime administrative.

- `ApplicationRoles.CanReadManagedUsers(role)`
  Lejon `admin`, `drejtor_agjencie` dhe `drejtor_inovacioni_publik`. Perdoret vetem per `GET /auth/users`.

## Matrica e aksesit

| Endpoint | Qellimi | Rolet e lejuara |
| --- | --- | --- |
| `GET /auth/users` | Lexon listen e llogarive te menaxhueshme. Admin e perdor per administrim; drejtoret e perdorin per zgjedhje anetaresh ne projekte. | `admin`, `drejtor_agjencie`, `drejtor_inovacioni_publik` |
| `POST /auth/users` | Krijon llogari te re. | `admin` |
| `PUT /auth/users/{id}` | Modifikon emrin, username, rolin, ministrine ose password-in e nje llogarie. | `admin` |
| `PUT /auth/users/{id}/password` | Reseton password-in e nje llogarie. | `admin` |
| `DELETE /auth/users/{id}` | Caktivizon llogarine. | `admin` |
| `PUT /auth/users/{id}/activate` | Aktivizon llogarine. | `admin` |
| `DELETE /auth/users/{id}/permanent` | Fshin perfundimisht llogarine. | `admin` |
| `PUT /auth/me/credentials` | Ndryshon kredencialet e llogarise aktuale. | Cdo llogari me token dhe `username`; view-only pa kredenciale nuk lejohet |

## Rolet qe mund te administrohen

Admini mund te krijoje dhe menaxhoje llogari per rolet:

- `kryeminister`
- `minister`
- `minister_ekonomie_inovacioni`
- `admin`
- `drejtor_agjencie`
- `drejtor_inovacioni_publik`
- `staf_agjencie`
- `ekspert`
- `ekspert_ekosistemi_startupeve`
- `ekspert_programet_mbeshtetjes`
- `ekspert_financimi_alternativ`
- `ekspert_projekte_be`
- `specialist`
- `staf_ministrie`
- `perfaqesues_institucioni`
