# InfoTrack JMeter smoke test runner (CLI)
# Usage: .\run-smoke.ps1
# Optional: .\run-smoke.ps1 -JmeterHome "C:\apache-jmeter-5.6.3"

param(
    [string]$JmeterHome = "C:\apache-jmeter-5.6.3"
)

$ErrorActionPreference = "Stop"

$Jmx    = Join-Path $PSScriptRoot "ScrapeApiSmokeTest.jmx"
$Results = Join-Path $PSScriptRoot "results"
$ReportRoot = Join-Path $PSScriptRoot "report"
$Report = Join-Path $ReportRoot "smoke"
$Jtl    = Join-Path $Results "smoke.jtl"
$JmeterLog = Join-Path $Results "jmeter.log"
$Jmeter = Join-Path $JmeterHome "bin\jmeter.bat"

if (-not (Test-Path $Jmeter)) {
    Write-Error "JMeter not found at $Jmeter. Pass -JmeterHome or install to C:\apache-jmeter-5.6.3"
}

New-Item -ItemType Directory -Force -Path $Results, $ReportRoot | Out-Null

# JMeter requires a non-existent or empty -l results file and a non-existent -o folder
Remove-Item -Force $Jtl, $JmeterLog -ErrorAction SilentlyContinue
if (Test-Path $Report) {
    Remove-Item -Recurse -Force $Report
}

Write-Host "Plan:   $Jmx"
Write-Host "Results: $Jtl"
Write-Host "Report:  $Report"
Write-Host ""

& $Jmeter -n -t $Jmx -l $Jtl -j $JmeterLog -e -o $Report
$exitCode = $LASTEXITCODE

if ($exitCode -ne 0) {
    Write-Host ""
    Write-Error "JMeter exited with code $exitCode"
}

$failed = @(Import-Csv $Jtl | Where-Object { $_.success -eq "false" })
if ($failed.Count -gt 0) {
    Write-Host ""
    Write-Host "Failed samples ($($failed.Count)):" -ForegroundColor Red
    foreach ($row in $failed) {
        $detail = if ($row.failureMessage) { " — $($row.failureMessage)" } else { "" }
        Write-Host "  $($row.label): HTTP $($row.responseCode)$detail" -ForegroundColor Red
    }
    exit 1
}

Write-Host ""
Write-Host "Passed (0 errors). Open report: $Report\index.html"
exit 0
