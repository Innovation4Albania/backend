# Performance Testing

Ky folder permban testin e ngarkeses `k6` per endpoint-et kryesore te backend-it.
Testi perdor autentikimin real te API-se dhe mat:

- `GET /api/dashboard/summary`
- `GET /api/projects`
- `GET /api/risk-deviations`
- `POST /api/updates` kur `ENABLE_WRITES=true`

## Profili i ngarkeses

Profili default krijon `55` perdorues virtuale leximi:

- rritje per `30s` deri ne `55` VUs
- ngarkese konstante per `2m`
- ulje per `15s`

Me `ENABLE_WRITES=true`, shtohen edhe `5` perdorues virtuale qe dergojne
weekly updates gjate periudhes konstante. Ky eshte skenari i synuar per
testimin e ngarkeses se kombinuar: `60` perdorues aktive.

## Parakushtet

1. Instalo `k6`: <https://grafana.com/docs/k6/latest/set-up/install-k6/>
2. Nise backend-in ne nje ambient testimi ose staging me te dhena provuese.
3. Konfiguro nje perdorues qe ka leje per dashboard, risk dhe updates.

Mos aktivizo shkrimet ndaj production-it: skenari me updates krijon te dhena
reale ne projektin e pare qe i shfaqet perdoruesit.

## Ekzekutimi

Nga root-i i repository-t, ne PowerShell:

```powershell
$env:BASE_URL = "http://localhost:5000/api"
$env:USERNAME = "performance-user"
$env:PASSWORD = "password-i-testit"
$env:ROLE = "drejtor_agjencie"
k6 run --summary-export=performance/results/summary.json performance/k6/dashboard-load.js
```

Per skenarin e plote me lexime dhe weekly updates:

```powershell
$env:ENABLE_WRITES = "true"
$env:READ_VUS = "55"
$env:WRITE_VUS = "5"
$env:STEADY_DURATION = "2m"
k6 run --summary-export=performance/results/summary.json performance/k6/dashboard-load.js
```

Per nje rol qe kerkon ministri, vendos edhe:

```powershell
$env:MINISTRY = "Emri i ministrise"
```

## Ekzekutimi ne GitHub Actions

Workflow-i `.github/workflows/performance-test.yml` nis manualisht nga
`Actions > Backend Performance Test > Run workflow`. Ai perdor action-in
zyrtar `grafana/setup-k6-action@v1` per te instaluar k6 dhe ngarkon raportin
JSON si artifact.

Krijo environment-in GitHub `performance` dhe vendos secrets:

| Secret | Vlera |
| --- | --- |
| `PERFORMANCE_BASE_URL` | URL e API-se staging, duke perfshire `/api` |
| `PERFORMANCE_USERNAME` | Username i perdoruesit te testimit |
| `PERFORMANCE_PASSWORD` | Password i perdoruesit te testimit |
| `PERFORMANCE_ROLE` | P.sh. `drejtor_agjencie` |
| `PERFORMANCE_MINISTRY` | Vetem kur roli kerkon ministri |

Rekomandohet qe environment-i `performance` te kete approval para
ekzekutimit, sidomos kur aktivizohet opsioni i shkrimeve.

## Pragjet

Testi deshton automatikisht kur:

| Matja | Prag |
| --- | --- |
| Kerkesa me gabim | me pak se `1%` |
| Checks funksionale | mbi `99%` sukses |
| Dashboard summary | `p95 < 750ms`, `p99 < 1500ms` |
| Projects | `p95 < 900ms`, `p99 < 1800ms` |
| Risk deviations | `p95 < 900ms`, `p99 < 1800ms` |
| Weekly update | `p95 < 1200ms`, `p99 < 2500ms` |

## Analiza e bottleneck-eve

Krahaso metrikat sipas tag-ut `endpoint` ne output-in e k6 ose ne
`performance/results/summary.json`.

- Nese `projects` dhe `risk_deviations` degradojne bashke, kontrollo skanimin,
  filtrimin dhe renditjen e projekteve te dukshme.
- Nese `weekly_update` degradohet me shpejt se leximet, kontrollo lock-un e
  mutacioneve, rillogaritjen e OKR dhe persistencen e snapshot-it.
- Nese te gjitha endpoint-et ngadalesohen gjate updates, mat contention midis
  leximeve dhe mutacioneve ne store.

Kodi aktual rillogarit OKR gjate mutacioneve, jo gjate cdo `ToResponse()`.
Ky test mban te ndara matjet e leximeve nga matjet e shkrimeve per ta bere
analizen te sakte.
