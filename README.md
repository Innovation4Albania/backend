# Innovation4Albania Backend

Backend `ASP.NET Core` për platformën `Innovation4Albania Dashboard`.

## Përmbajtja

- API `.NET` për projektet, dashboard-in, OKR, riskun, kalendarin dhe përditësimet
- strukturë e gatshme për deploy në Render
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

Ky repo përfshin edhe `render.yaml`, por mund ta deploy-osh edhe manualisht si `Web Service`.

Shiko:

- `Innovation4Albania.Render.Deploy.md`

## Environment Variables

- `ALLOWED_ORIGINS`
  Vendos URL-në e frontend-it të Render, p.sh.
  `https://innovation4albania-frontend.onrender.com`

- `ASPNETCORE_URLS`
  `http://0.0.0.0:10000`
