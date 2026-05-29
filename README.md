# Innovation4Albania Backend

Backend `ASP.NET Core` për platformën `Innovation4Albania Dashboard`.

## Përmbajtja

- API `.NET` për projektet, dashboard-in, OKR, riskun, kalendarin dhe përditësimet
- strukturë e gatshme për deploy në Render me `Docker`
- konfigurim CORS me `ALLOWED_ORIGINS`

## Struktura

- `Innovation4Albania.DashboardBackend.Api/`
- `Innovation4Albania.DashboardBackend.slnx`
- `Innovation4Albania.Render.Deploy.md`
- `render.yaml`

## Nisja lokale

```bash
dotnet restore Innovation4Albania.DashboardBackend.slnx
dotnet run --project Innovation4Albania.DashboardBackend.Api/Innovation4Albania.DashboardBackend.Api.csproj
```

Backend-i hap endpoint-et nën `/api`.

## Health Check

```text
/api/health
```

## Deploy në Render

Ky repo përfshin:

- `render.yaml`
- `Dockerfile`

Render nuk mbështet `.NET` si native runtime në Blueprints, ndaj ky backend deploy-ohet si `docker` service.

Shiko:

- `Innovation4Albania.Render.Deploy.md`

## Environment Variables

- `ALLOWED_ORIGINS`
  Vendos URL-në e frontend-it të Render, p.sh.
  `https://innovation4albania-frontend.onrender.com`
  E detyrueshme ne production/staging. Nese mungon jashte `Development`, backend-i ndalon ne startup.
  Fallback me localhost perdoret vetem gjate zhvillimit lokal.

- `PORT`
  `10000`

- `Auth__Users__admin__Username` / `Auth__Users__admin__Password`
  Kredencialet fillestare të adminit. `Password` mund të jetë password i thjeshtë
  nga environment variable ose BCrypt hash; nuk ruhet në source code dhe nuk
  mbishkruan password-in e një llogarie ekzistuese.

- `Auth__Users__drejtor_agjencie__Username` / `Auth__Users__drejtor_agjencie__Password`
  Kredencialet fillestare për rolin `Drejtor Innovation4Albania`, kur duhen seed-uar
  nga environment variables.

- `Auth__Users__drejtor_inovacioni_publik__Username` / `Auth__Users__drejtor_inovacioni_publik__Password`
  Kredencialet fillestare për rolin `Drejtor i Inovacionit Publik`, kur duhen seed-uar
  nga environment variables.

## Performance Tests

Testi i ngarkeses me `k6` simulon mbi 50 perdorues njekohesisht ne dashboard,
projects, risk deviations dhe, ne menyre opsionale, weekly updates.

Shiko `performance/README.md` per ekzekutimin, pragjet dhe interpretimin e
rezultateve.
