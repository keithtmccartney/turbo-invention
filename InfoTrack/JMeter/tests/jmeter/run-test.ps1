# InfoTrack JMeter generic CLI runner
# Usage: .\run-test.ps1 -Plan ScrapeApiLoadTest
#        .\run-test.ps1 -Plan DashboardApiTest -JmeterHome "C:\apache-jmeter-5.6.3"

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet(
        "ScrapeApiSmokeTest",
        "ScrapeApiLoadTest",
        "DashboardApiTest",
        "InsightsApiTest",
        "DiscoveryApiTest",
        "McpApiTest")]
    [string]$Plan,

    [string]$JmeterHome = "C:\apache-jmeter-5.6.3"
)

$ErrorActionPreference = "Stop"

$slugByPlan = @{
    ScrapeApiSmokeTest = "smoke"
    ScrapeApiLoadTest  = "scrape-load"
    DashboardApiTest   = "dashboard"
    InsightsApiTest    = "insights"
    DiscoveryApiTest   = "discovery"
    McpApiTest         = "mcp"
}

$slug = $slugByPlan[$Plan]
$Jmx = Join-Path $PSScriptRoot "$Plan.jmx"
$Results = Join-Path $PSScriptRoot "results"
$ReportRoot = Join-Path $PSScriptRoot "report"
$Report = Join-Path $ReportRoot $slug
$Jtl = Join-Path $Results "$slug.jtl"
$JmeterLog = Join-Path $Results "jmeter.log"
$Jmeter = Join-Path $JmeterHome "bin\jmeter.bat"

if (-not (Test-Path $Jmx)) {
    Write-Error "Plan not found: $Jmx"
}

if (-not (Test-Path $Jmeter)) {
    Write-Error "JMeter not found at $Jmeter"
}

New-Item -ItemType Directory -Force -Path $Results, $ReportRoot | Out-Null
Remove-Item -Force $Jtl, $JmeterLog -ErrorAction SilentlyContinue
if (Test-Path $Report) {
    Remove-Item -Recurse -Force $Report
}

Write-Host "Running $Plan ..."
Write-Host "  JTL:    $Jtl"
Write-Host "  Report: $Report"
Write-Host ""

& $Jmeter -n -t $Jmx -l $Jtl -j $JmeterLog -e -o $Report
$exitCode = $LASTEXITCODE

if ($exitCode -ne 0) {
    Write-Error "JMeter exited with code $exitCode"
}

$failed = @(Import-Csv $Jtl | Where-Object { $_.success -eq "false" })
if ($failed.Count -gt 0) {
    Write-Host "Failed samples ($($failed.Count)):" -ForegroundColor Red
    foreach ($row in $failed) {
        $detail = if ($row.failureMessage) { " — $($row.failureMessage)" } else { "" }
        Write-Host "  $($row.label): HTTP $($row.responseCode)$detail" -ForegroundColor Red
    }
    exit 1
}

Write-Host "Passed (0 errors). Open: $Report\index.html"
exit 0
