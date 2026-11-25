$ErrorActionPreference = 'Stop'
$frEvidence = Join-Path $PWD 'Docs/Modules/ProceduralGeneration/Evidence/FourFrontiers'
$frBuild = Get-Content -LiteralPath (Join-Path $frEvidence 'Build.json') -Raw | ConvertFrom-Json
$frExecutable = [IO.Path]::GetFullPath($frBuild.output)
if (-not (Test-Path -LiteralPath $frExecutable)) { throw 'FourFrontiers executable missing.' }
$frRuns = @()
foreach ($frSeed in @(0, 1, 1543)) {
    foreach ($frMode in @('baseline', 'gameplay')) { $frRuns += @{ seed = $frSeed; mode = $frMode; flag = '--four-frontiers-smoke' } }
    foreach ($frMode in @('capture', 'load')) { $frRuns += @{ seed = $frSeed; mode = $frMode; flag = '--sl4-smoke' } }
}
foreach ($frMode in @('fallback', 'failure')) { $frRuns += @{ seed = 1543; mode = $frMode; flag = '--four-frontiers-smoke' } }
foreach ($frRun in $frRuns) {
    $frCategory = if ($frRun.flag -eq '--sl4-smoke') { 'SL4' } else { $frRun.mode }
    $frOutput = Join-Path $PWD ("Outputs/FourFrontiers/{0}-{1}" -f $frCategory, $frRun.seed)
    New-Item -ItemType Directory -Path $frOutput -Force | Out-Null
    $frLog = Join-Path $frOutput ($frRun.mode + '.log')
    $frArgs = @('-batchmode', '-screen-fullscreen', '0', '-screen-width', '1280', '-screen-height', '720', $frRun.flag, $frRun.mode, ('"' + $frOutput + '"'), "$($frRun.seed)", '-logFile', ('"' + $frLog + '"'))
    $frProcess = Start-Process -FilePath $frExecutable -ArgumentList $frArgs -WorkingDirectory $PWD -WindowStyle Hidden -PassThru
    if (-not $frProcess.WaitForExit(55000)) { $frProcess.Kill(); throw "Timeout $($frRun.mode) seed$($frRun.seed). Log: $frLog" }
    $frProcess.Refresh()
    if ($frProcess.ExitCode -ne 0) { throw "Player failed $($frRun.mode) seed$($frRun.seed). Log: $frLog" }
    $frResultName = if ($frRun.flag -eq '--sl4-smoke') { $frRun.mode + '.json' } else { 'result.json' }
    $frResult = Get-Content -LiteralPath (Join-Path $frOutput $frResultName) -Raw | ConvertFrom-Json
    if (-not $frResult.passed) { throw "False result $($frRun.mode) seed$($frRun.seed)" }
    Copy-Item -LiteralPath (Join-Path $frOutput $frResultName) -Destination (Join-Path $frEvidence ("Player-{0}-{1}.json" -f $frRun.seed, $frRun.mode))
    if ($frRun.flag -ne '--sl4-smoke') { Copy-Item -LiteralPath (Join-Path $frOutput 'first.json') -Destination (Join-Path $frEvidence ("Player-{0}-{1}-first.json" -f $frRun.seed, $frRun.mode)) }
    Write-Output "Player $($frRun.mode) seed$($frRun.seed) PASS"
}
