param(
    [string]$ResultDirectory = '.utmp/gameplay-smoke',
    [string]$Tests = 'Logs/gameplay-editmode.xml',
    [string]$PlayerLog = 'Logs/gameplay-player.log'
)
$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
Push-Location -LiteralPath $repository
try {
    $resultPath = Join-Path $ResultDirectory 'gameplay-result.json'
    $report = Get-Content -Raw -Encoding UTF8 -LiteralPath $resultPath | ConvertFrom-Json
    if (-not $report.passed -or @($report.errors).Count -gt 0 -or @($report.checks | Where-Object { -not $_.passed }).Count -gt 0) {
        throw 'Gameplay integration report did not pass.'
    }
    [xml]$testRun = Get-Content -Raw -Encoding UTF8 -LiteralPath $Tests
    if ($testRun.'test-run'.result -ne 'Passed' -or [int]$testRun.'test-run'.failed -gt 0) {
        throw 'EditMode test report did not pass.'
    }
    $runtimeErrors = Select-String -LiteralPath $PlayerLog -Pattern 'Exception:|error CS\d+|A Native Collection has not been disposed|Shader error'
    if ($runtimeErrors) { throw 'Player log contains runtime errors or undisposed native collections.' }
    $destination = 'Docs/Modules/DOTSGameplay/Evidence'
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    Copy-Item -LiteralPath $resultPath -Destination (Join-Path $destination 'gameplay-result.json')
    Copy-Item -LiteralPath $Tests -Destination (Join-Path $destination 'editmode-tests.xml')
    Write-Output ("Saved {0} passing EditMode tests and {1} passing integration checks." -f $testRun.'test-run'.passed, @($report.checks).Count)
}
finally { Pop-Location }
