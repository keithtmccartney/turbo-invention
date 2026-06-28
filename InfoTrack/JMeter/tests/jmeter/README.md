# InfoTrack JMeter Tests

Location: **`InfoTrack/JMeter/tests/jmeter/`** (alongside `InfoTrack.Tests` for xUnit).

Lightweight [Apache JMeter](https://jmeter.apache.org/) plans for the InfoTrack Solicitor Intelligence API. These are **assessment-grade** smoke and load tests — not an enterprise performance suite — intended to show that the API has been considered from a quality, concurrency, and operational perspective.

## Prerequisites

| Requirement | Notes |
| ----------- | ----- |
| **Apache JMeter 5.6.3** | Installed at `C:\apache-jmeter-5.6.3\` (binary zip is fine) |
| **Java 17+** | Required by JMeter 5.6 |
| **InfoTrack API running** | Default base URL: `http://localhost:5080` |

Start the API from the **InfoTrack** folder (parent of this directory):

```bash
cd InfoTrack
dotnet run --project InfoTrack.Api
```

Swagger UI (sanity check): http://localhost:5080/swagger

---

## Test plans

| File | Purpose | Threads | Loops | Key endpoint(s) |
| ---- | ------- | ------- | ----- | ---------------- |
| **ScrapeApiSmokeTest.jmx** | Quick sanity check after startup | 1 | 1 each | Swagger, locations, insights, discovery, results, scrape |
| **ScrapeApiLoadTest.jmx** | Scrape concurrency / mutex behaviour | 10 | 1 | `POST /api/scrape` |
| **DashboardApiTest.jmx** | Dashboard read performance | 50 | 5 | `GET /api/insights` |
| **InsightsApiTest.jmx** | Analytics compare read load | 20 | 5 | `GET /api/insights/compare` |
| **DiscoveryApiTest.jmx** | Single discovery start (do not hammer) | 1 | 1 | `POST /api/discovery/run` |
| **McpApiTest.jmx** | MCP JSON-RPC + tool listing | 1 | 1 | `POST /api/mcp`, `GET /api/mcp/tools` |
| **ApiAbuseTest.jmx** | Inbound rate-limit / 429 defence | 15 | 1 | `GET /api/insights` (requires low `RateLimiting:ReadPermitLimit`) |

### API route mapping

The Vue dashboard reads **`GET /api/insights`** (there is no `/api/dashboard` route). Async operations use:

- `POST /api/scrape` → **202 Accepted** (or **400** if no active locations, **409** if scrape already running)
- `POST /api/discovery/run` → **202 Accepted** (or **409** if discovery already running)

MCP is at **`/api/mcp`** (not `/mcp/v1`). Authentication uses `Authorization: Bearer {Mcp:ApiKey}`.

---

## Configuration variables

Each test plan defines **User Defined Variables** at the top (Test Plan → User Defined Variables in the GUI):

| Variable | Default | Description |
| -------- | ------- | ----------- |
| `BASE_HOST` | `localhost` | API hostname |
| `BASE_PORT` | `5080` | API port |
| `BASE_PROTOCOL` | `http` | `http` or `https` |
| `MCP_API_KEY` | `dev-mcp-api-key-change-me` | Matches `Mcp:ApiKey` in `InfoTrack.Api/appsettings.json` |
| `THREAD_COUNT` | varies | Virtual users (load tests) |
| `RAMP_UP_SECONDS` | varies | Ramp-up period |
| `LOOP_COUNT` | varies | Iterations per thread |
| `RESPONSE_TIME_MS` | `2000` | Smoke test duration threshold |

Override on the CLI with JMeter properties, e.g.:

```bash
jmeter -n -t ScrapeApiSmokeTest.jmx -JBASE_PORT=5081 -JMCP_API_KEY=your-key -l results.jtl
```

---

## Optional setup — active locations

`POST /api/scrape` returns **400** when no locations are active. For the concurrency test to demonstrate **202 + 409** behaviour, activate locations first:

**Via the UI:** Discovery → Locations → select cities → Save.

**Via curl:**

```bash
curl -X POST http://localhost:5080/api/locations \
  -H "Content-Type: application/json" \
  -d "{\"locations\":[\"London\",\"Cardiff\"]}"
```

`TestData.csv` lists sample location names if you extend the plans with a CSV Data Set Config.

---

## Running tests

### GUI vs CLI — when to use which

JMeter shows this banner on startup (safe to ignore the `StatusConsoleListener` warnings — they are deprecation notices about plugin scanning, not test failures):

```
WARN StatusConsoleListener The use of package scanning to locate plugins is deprecated ...
================================================================================
Don't use GUI mode for load testing!, only for Test creation and Test debugging.
For load testing, use CLI Mode (was NON GUI):
   jmeter -n -t [jmx file] -l [results file] -e -o [Path to web report folder]
================================================================================
```

| Mode | Use for |
| ---- | ------- |
| **GUI** (`jmeter.bat`) | Opening/editing `.jmx` files, smoke tests, debugging failed assertions |
| **CLI** (`jmeter -n …`) | `DashboardApiTest`, `InsightsApiTest`, `ScrapeApiLoadTest` — anything with many threads/loops |

For load plans, **disable View Results Tree** in the GUI before running, or prefer CLI entirely.

---

### GUI mode (Windows / PowerShell)

```powershell
cd C:\apache-jmeter-5.6.3\bin
.\jmeter.bat
```

Or without a console window:

```powershell
cd C:\apache-jmeter-5.6.3\bin
.\jmeterw.cmd
```

1. **File → Open** → browse to `InfoTrack\JMeter\tests\jmeter\` and select a `.jmx` file.
2. Review **User Defined Variables** (click the **Test Plan** root node).
3. Click **Start** (green triangle).
4. Inspect **Summary Report** / **View Results Tree** listeners under the Thread Group.

**Recommended GUI plans:** `ScrapeApiSmokeTest.jmx`, `McpApiTest.jmx`, `DiscoveryApiTest.jmx`

---

### Command line (non-GUI) — recommended for load tests

**Easiest — use the helper script** (creates folders, clears prior report output):

```powershell
cd C:\Users\Keith\source\repos\keithtmccartney\turbo-invention\InfoTrack\JMeter\tests\jmeter
.\run-smoke.ps1

# Other plans
.\run-test.ps1 -Plan ScrapeApiLoadTest
.\run-test.ps1 -Plan DashboardApiTest
.\run-test.ps1 -Plan McpApiTest

# Inbound 429 defence (start API with low read limit first — see below)
.\run-abuse-test.ps1
```

Ensure **InfoTrack.Api is running** on `http://localhost:5080` before executing.

**Inbound rate-limit abuse test** — uses **two terminals**. Environment variables must be set on the **API process**, not the JMeter terminal.

**Terminal 1** (InfoTrack folder) — easiest: use the `AbuseTest` launch profile:

```powershell
cd InfoTrack
dotnet run --project InfoTrack.Api --launch-profile AbuseTest
```

Or set env vars manually in the API terminal before `dotnet run`:

```powershell
cd InfoTrack
$env:RateLimiting__ReadPermitLimit = "5"
$env:RateLimiting__ReadWindowSeconds = "60"
dotnet run --project InfoTrack.Api
```

**Terminal 2**:

```powershell
cd InfoTrack\JMeter\tests\jmeter
.\run-abuse-test.ps1
```

`run-abuse-test.ps1` runs a preflight burst against the API and fails fast if no **429** is observed (avoids a full JMeter run when the API still has the default 600/min limit).

Pass criteria: at least one **429**, no **5xx**, all samples satisfy the 200/429 assertion.

---

**Manual CLI** — set `$Repo` first, create output folders, and remove any previous `-o` target (JMeter creates `report\smoke` but **not** the parent `report` folder):

```powershell
$Repo = "C:\Users\Keith\source\repos\keithtmccartney\turbo-invention\InfoTrack"
$Jmx  = "$Repo\JMeter\tests\jmeter"
$Out  = "$Jmx\results"
$ReportRoot = "$Jmx\report"

New-Item -ItemType Directory -Force -Path $Out, $ReportRoot | Out-Null
Remove-Item -Force "$Out\smoke.jtl" -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force "$ReportRoot\smoke" -ErrorAction SilentlyContinue

cd C:\apache-jmeter-5.6.3\bin
.\jmeter.bat -n -t "$Jmx\ScrapeApiSmokeTest.jmx" -l "$Out\smoke.jtl" -e -o "$ReportRoot\smoke"
```

> **First run failed with `C:\tests\jmeter\...`?** `$Repo` was empty — PowerShell expanded `"$Repo\tests\..."` to `\tests\...` → `C:\tests\...`. Always assign `$Repo` before the command, or use `.\run-smoke.ps1`.

**Scrape concurrency** (10 simultaneous users):

```powershell
.\run-test.ps1 -Plan ScrapeApiLoadTest
```

Or manually (after `$Repo` / folder setup as above):

```powershell
Remove-Item -Recurse -Force "$ReportRoot\scrape-load" -ErrorAction SilentlyContinue
.\jmeter.bat -n -t "$Jmx\ScrapeApiLoadTest.jmx" -l "$Out\scrape-load.jtl" -e -o "$ReportRoot\scrape-load"
```

**Dashboard load** (50 users × 5 loops):

```powershell
.\run-test.ps1 -Plan DashboardApiTest
```

**Insights compare load**:

```powershell
.\run-test.ps1 -Plan InsightsApiTest
```

**Discovery** (single request — do not loop):

```powershell
.\run-test.ps1 -Plan DiscoveryApiTest
```

**MCP**:

```powershell
.\run-test.ps1 -Plan McpApiTest
```

Open the HTML dashboard: `InfoTrack\JMeter\tests\jmeter\report\smoke\index.html` (or the matching subfolder per test).

Exit code **0** means all assertions passed.

**Optional — override variables from CLI:**

```powershell
cd C:\apache-jmeter-5.6.3\bin
.\jmeter.bat -n -t "$Jmx\McpApiTest.jmx" -JMCP_API_KEY=your-key -JBASE_PORT=5080 -l "$Out\mcp.jtl" -e -o "$ReportRoot\mcp"
```

**Optional — increase Java heap for heavier runs** (e.g. DashboardApiTest at 50×5): edit `HEAP` in `C:\apache-jmeter-5.6.3\bin\jmeter.bat`, e.g.:

```bat
set HEAP=-Xms1g -Xmx1g -XX:MaxMetaspaceSize=256m
```

See [JMeter best practices](https://jmeter.apache.org/usermanual/best-practices.html).

---

### HTML report (from existing `.jtl`)

If you already ran without `-e -o`, generate the report separately:

```powershell
cd C:\apache-jmeter-5.6.3\bin
.\jmeter.bat -g "$Out\smoke.jtl" -o "$Report\smoke"
```

---

## Reading results in the GUI

After a GUI run, open these listeners on the test plan tree:

### Summary Report

Shows per-label **# samples**, **average** response time, ** throughput**, and **error %**.

<!-- Screenshot placeholder: docs/jmeter-summary-report.png -->
> **Where to find it:** Listener named **Summary Report** under the Thread Group.  
> Columns to review: `# Samples`, `Average`, `Throughput`, `Error %`.

### Aggregate Report

Same metrics with additional percentile columns (**median**, **90th / 95th / 99th** line).

<!-- Screenshot placeholder: docs/jmeter-aggregate-report.png -->
> **Where to find it:** Listener named **Aggregate Report** (ScrapeApiLoadTest, DashboardApiTest, InsightsApiTest).

### View Results Tree

Request/response detail for debugging failed assertions. Expand a row to see response headers and body.

<!-- Screenshot placeholder: docs/jmeter-view-results-tree.png -->
> **Where to find it:** Listener named **View Results Tree** (enabled in smoke, discovery, MCP, scrape load plans).

---

## Expected behaviour

### ScrapeApiSmokeTest

| Step | Endpoint | Expected status |
| ---- | -------- | --------------- |
| Health proxy | `GET /swagger/v1/swagger.json` | 200 |
| Locations | `GET /api/locations` | 200 |
| Dashboard | `GET /api/insights` | 200 |
| Discovery summary | `GET /api/discovery/summary` | 200 |
| Results | `GET /api/results` | 200 |
| Start scrape | `POST /api/scrape` | **202**, **400**, or **409** (never 5xx) |

All steps must complete within **2 seconds** (queue/acceptance only — not full scrape completion).

### ScrapeApiLoadTest

With **active locations**:

- Exactly **one** `202 Accepted` (scrape queued)
- Remaining requests: **`409 Conflict`** (scrape already in progress)

Without active locations:

- All requests: **`400 Bad Request`**

No **`5xx`** responses in either case.

### DiscoveryApiTest

- **`202 Accepted`** when discovery starts, or **`409`** if a run is already queued/running
- Response time under **5 seconds** (async queue — not full sitemap download)

### McpApiTest

- `POST /api/mcp` with `tools/list` → **200**, body contains `"result"`
- `GET /api/mcp/tools` → **200** with tool definitions
- Wrong/missing API key → **401** (verify manually by clearing `MCP_API_KEY`)

---

## Design notes

- **Intentionally lightweight** — suitable for a technical assessment, not production capacity planning.
- **Self-contained `.jmx` files** — each plan includes its own defaults and comments; no plugins required beyond JMeter core.
- **Groovy assertions** (`JSR223Assertion`) used where multiple HTTP status codes are valid (scrape/discovery concurrency).
- **Polite discovery testing** — single request; do not loop `POST /api/discovery/run` (hits external sitemap + rate limits).

---

## Troubleshooting

| Symptom | Likely cause | Fix |
| ------- | ------------- | --- |
| Connection refused | API not running | `dotnet run --project InfoTrack.Api` |
| MCP 401 | API key mismatch | Set `MCP_API_KEY` to match `appsettings.json` |
| Scrape smoke fails on POST | Unexpected 409 | Wait for in-flight scrape to finish, or restart API |
| All scrape load = 400 | No active locations | Activate locations (see above) |
| Discovery 409 | Prior run still active | Wait or restart API |
| `StatusConsoleListener` warnings on startup | JMeter 5.6 plugin-scan deprecation | Harmless — ignore; does not affect these tests |
| `Cannot write to ... report\smoke ... parent folder is not writable` | `InfoTrack\JMeter\tests\jmeter\report` missing, or `$Repo` unset | Run `.\run-smoke.ps1`, or `New-Item -Force` on `results` + `report`; set `$Repo` before manual CLI |
| `Results file ... is not empty` | Prior `.jtl` from earlier run | Delete `results\*.jtl` or re-run via `.\run-smoke.ps1` (scripts clear it automatically) |
| Out of memory on load test | GUI + View Results Tree, or default heap | Use CLI mode; increase `HEAP` in `jmeter.bat` |

---

## Related documentation

- InfoTrack API & architecture: [`InfoTrack/README.md`](../InfoTrack/README.md)
- xUnit integration tests: `InfoTrack/InfoTrack.Tests/Integration/`
