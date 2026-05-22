# Deploy në Render

Ky projekt ndahet në dy shërbime:

1. `Innovation4Albania.DashboardBackend.Api` si `Web Service`
2. frontend React/Vite si `Static Site`

## 1. Backend në Render

Krijo një `Web Service` me këto vlera:

- Root Directory:
  `Innovation4Albania.DashboardBackend.Api`
- Build Command:
  `dotnet publish -c Release -o out`
- Start Command:
  `dotnet out/Innovation4Albania.DashboardBackend.Api.dll`

### Environment Variables

- `ALLOWED_ORIGINS`
  vendos URL-në e frontend-it në Render, p.sh.:
  `https://innovation4albania-frontend.onrender.com`
  Kjo vlere eshte e detyrueshme ne production/staging. Pa te, backend-i ndalon ne startup per te shmangur fallback-un me localhost.

Nëse Render nuk e vendos portin automatikisht për .NET, shto:

- `ASPNETCORE_URLS`
  `http://0.0.0.0:10000`

ose përdor:

- `PORT`
  `10000`

## 2. Frontend në Render

Krijo një `Static Site` me këto vlera:

- Root Directory:
  dosja e frontend-it
- Build Command:
  `npm install && npm run build`
- Publish Directory:
  `dist`

### Environment Variable

- `VITE_API_BASE_URL`
  vendos URL-në publike të backend-it, p.sh.:
  `https://innovation4albania-backend.onrender.com/api`

## 3. React Router në Static Site

Te Render shto një `Rewrite Rule`:

- Source:
  `/*`
- Destination:
  `/index.html`
- Action:
  `Rewrite`

Kjo është e nevojshme që routet si `/projects/1` të hapen si duhet.

## 4. Renditja e deploy-it

1. Deploy backend-in
2. Merr URL-në e backend-it
3. Vendose te `VITE_API_BASE_URL` në frontend
4. Deploy frontend-in
5. Vendose URL-në e frontend-it te `ALLOWED_ORIGINS` në backend
6. Redeploy backend-in

## 5. Kontrolli pas deploy-it

Backend health:

`https://BACKEND-URL/api/health`

Frontend:

- login duhet të hapet normalisht
- dashboard duhet të marrë të dhënat
- tabs si `Projektet`, `Risk & Devijime`, `Kalendari` dhe `Përditësimet` duhet të funksionojnë pa gabime
