$ErrorActionPreference = 'Stop'
$sl4Build = Get-Content -LiteralPath 'Outputs/SaveLoad/SL4/Build.json' -Raw | ConvertFrom-Json
$sl4Executable = [IO.Path]::GetFullPath($sl4Build.output)
if (-not (Test-Path -LiteralPath $sl4Executable)) { throw 'Build executable missing.' }
$sl4Evidence = Join-Path $PWD 'Docs/Modules/SaveLoad/Evidence/SL4-2026-11-24'
foreach ($sl4Seed in @(0, 1, 1543)) {
    $sl4Output = Join-Path $PWD "Outputs/SaveLoad/SL4/PlayerSeed$sl4Seed"
    New-Item -ItemType Directory -Path $sl4Output -Force | Out-Null
    foreach ($sl4Mode in @('capture', 'load')) {
        $sl4Log = Join-Path $sl4Output "$sl4Mode.log"
        $sl4Args = @('-batchmode', '-screen-fullscreen', '0', '-screen-width', '1280', '-screen-height', '720', '--sl4-smoke', $sl4Mode, ('"' + $sl4Output + '"'), "$sl4Seed", '-logFile', ('"' + $sl4Log + '"'))
        $sl4Process = Start-Process -FilePath $sl4Executable -ArgumentList $sl4Args -WorkingDirectory $PWD -WindowStyle Hidden -PassThru
        if (-not $sl4Process.WaitForExit(55000)) { $sl4Process.Kill(); throw "SL4 $sl4Mode seed$sl4Seed exceeded55s. Log: $sl4Log" }
        $sl4Process.Refresh()
        if ($sl4Process.ExitCode -ne 0) { throw "SL4 $sl4Mode seed$sl4Seed failed with exit$($sl4Process.ExitCode). Log: $sl4Log" }
        $sl4ResultPath = Join-Path $sl4Output "$sl4Mode.json"
        if (-not (Test-Path -LiteralPath $sl4ResultPath)) { throw "Missing smoke result $sl4Mode seed$sl4Seed" }
        $sl4Result = Get-Content -LiteralPath $sl4ResultPath -Raw | ConvertFrom-Json
        if (-not $sl4Result.passed -or $sl4Result.seed -ne $sl4Seed) { throw "SL4 result validation failed $sl4Mode seed$sl4Seed" }
        Copy-Item -LiteralPath $sl4ResultPath -Destination (Join-Path $sl4Evidence "Player-$sl4Seed-$sl4Mode.json")
        Write-Output "SL4 Player seed$sl4Seed $sl4Mode PASS process$($sl4Result.processId)"
    }
}
