# InfoTrack JMeter inbound rate-limit abuse test
# Usage: .\run-abuse-test.ps1
#
# IMPORTANT: rate-limit env vars apply to the API process, NOT this script.
# Terminal 1 (InfoTrack folder):
#   cd InfoTrack
#   dotnet run --project InfoTrack.Api --launch-profile AbuseTest
# Terminal 2:
#   cd InfoTrack\JMeter\tests\jmeter
#   .\run-abuse-test.ps1

param(
    [string]$JmeterHome = "C:\apache-jmeter-5.6.3",
    [string]$ApiBaseUrl = "http://localhost:5080",
    [switch]$SkipPreflight
)

$ErrorActionPreference = "Stop"

function Test-InboundRateLimitActive {
    param([string]$BaseUrl)

    $throttled = 0
    $client = [System.Net.Http.HttpClient]::new()

    try {
        foreach ($null in 1..8) {
            $response = $client.GetAsync("$BaseUrl/api/insights").GetAwaiter().GetResult()
            if ([int]$response.StatusCode -eq 429) {
                $throttled++
            }

            $response.Dispose()
        }
    }
    finally {
        $client.Dispose()
    }

    return $throttled -gt 0
}

$Jmx = Join-Path $PSScriptRoot "ApiAbuseTest.jmx"
$Results = Join-Path $PSScriptRoot "results"
$ReportRoot = Join-Path $PSScriptRoot "report"
$Report = Join-Path $ReportRoot "abuse"
$Jtl = Join-Path $Results "abuse.jtl"
$JmeterLog = Join-Path $Results "jmeter.log"
$Jmeter = Join-Path $JmeterHome "bin\jmeter.bat"

if (-not (Test-Path $Jmeter)) {
    Write-Error "JMeter not found at $Jmeter"
}

if (-not $SkipPreflight) {
    Write-Host "Preflight: checking inbound rate limit on $ApiBaseUrl ..."
    if (-not (Test-InboundRateLimitActive -BaseUrl $ApiBaseUrl)) {
        Write-Host ""
        Write-Host "Rate limiting is not active (no HTTP 429 after 8 rapid reads)." -ForegroundColor Red
        Write-Host "Setting `$env:RateLimiting__*` in THIS terminal does not affect a running API." -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Stop the API, then restart from the InfoTrack folder in a separate terminal:" -ForegroundColor Yellow
        Write-Host '  cd InfoTrack' -ForegroundColor Cyan
        Write-Host '  dotnet run --project InfoTrack.Api --launch-profile AbuseTest' -ForegroundColor Cyan
        Write-Host ""
        Write-Host "Or set env vars in the API terminal before dotnet run:" -ForegroundColor Yellow
        Write-Host '  cd InfoTrack' -ForegroundColor Cyan
        Write-Host '  $env:RateLimiting__ReadPermitLimit = "5"' -ForegroundColor Cyan
        Write-Host '  $env:RateLimiting__ReadWindowSeconds = "60"' -ForegroundColor Cyan
        Write-Host '  dotnet run --project InfoTrack.Api' -ForegroundColor Cyan
        exit 1
    }

    Write-Host "Preflight OK — API returned HTTP 429 under burst load." -ForegroundColor Green
    Write-Host ""
}

New-Item -ItemType Directory -Force -Path $Results, $ReportRoot | Out-Null
Remove-Item -Force $Jtl, $JmeterLog -ErrorAction SilentlyContinue
if (Test-Path $Report) {
    Remove-Item -Recurse -Force $Report
}

Write-Host "Plan:   $Jmx"
Write-Host "Results: $Jtl"
Write-Host "Report:  $Report"
Write-Host ""

& $Jmeter -n -t $Jmx -l $Jtl -j $JmeterLog -e -o $Report
if ($LASTEXITCODE -ne 0) {
    Write-Error "JMeter exited with code $LASTEXITCODE"
}

$rows = @(Import-Csv $Jtl)
$failed = @($rows | Where-Object { $_.success -eq "false" })
if ($failed.Count -gt 0) {
    Write-Host "Assertion failures ($($failed.Count)):" -ForegroundColor Red
    foreach ($row in $failed) {
        Write-Host "  $($row.label): HTTP $($row.responseCode)" -ForegroundColor Red
    }
    exit 1
}

$throttled = @($rows | Where-Object { $_.responseCode -eq "429" })
$serverErrors = @($rows | Where-Object { [int]$_.responseCode -ge 500 })

if ($throttled.Count -lt 1) {
    Write-Host "Expected at least one HTTP 429 (inbound rate limit). Got 0." -ForegroundColor Red
    exit 1
}

if ($serverErrors.Count -gt 0) {
    Write-Host "Unexpected 5xx responses: $($serverErrors.Count)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Passed. Throttled $($throttled.Count)/$($rows.Count) requests with HTTP 429."
Write-Host "Open report: $Report\index.html"
exit 0
