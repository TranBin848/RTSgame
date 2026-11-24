param(
    [Parameter(Mandatory)][string]$BuildDirectory,
    [Parameter(Mandatory)][string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$e10Build = (Resolve-Path -LiteralPath $BuildDirectory).Path
$e10Exe = Join-Path $e10Build 'RTSgame2026.exe'
$e10Output = [IO.Path]::GetFullPath($OutputDirectory)
if (!(Test-Path -LiteralPath $e10Exe)) { throw 'Release executable missing.' }
if (Test-Path -LiteralPath $e10Output) { throw 'Use a new output directory; preserve previous evidence.' }
& "$PSScriptRoot/RunG5.ps1" -BuildDirectory $e10Build -OutputDirectory (Join-Path $e10Output 'g5')
foreach ($e10Id in @('lifecycle', 'night-stress')) {
    $e10Folder = Join-Path $e10Output $e10Id
    New-Item -ItemType Directory -Path $e10Folder -Force | Out-Null
    $e10Args = @('-logFile', ('"' + (Join-Path $e10Folder 'Player.log') + '"'), '--swarm-output', ('"' + $e10Folder + '"'),
        '-screen-fullscreen', '1', '-screen-width', '1920', '-screen-height', '1080', '-window-mode', 'borderless')
    if ($e10Id -eq 'lifecycle') { $e10Args += '--e10-smoke' }
    else { $e10Args += @('--progression-smoke', '--g5-onscreen', '--e10-night-stress') }
    # This is an explicitly requested onscreen measurement, so the Player must be visible.
    $e10Player = Start-Process -FilePath $e10Exe -ArgumentList $e10Args -WorkingDirectory $e10Build -WindowStyle Normal -PassThru
    Write-Output "$e10Id PID=$($e10Player.Id)"
    if (!$e10Player.WaitForExit(190000)) { throw "$e10Id timed out; inspect Player before retrying." }
    if ($e10Player.ExitCode -ne 0) { throw "$e10Id exited with code $($e10Player.ExitCode)." }
    if ($e10Id -eq 'lifecycle') {
        $e10Result = Get-Content -LiteralPath (Join-Path $e10Folder 'lifecycle-result.json') -Raw | ConvertFrom-Json
        if (!$e10Result.passed -or $e10Result.development) { throw 'Release lifecycle failed.' }
    } else {
        $e10Result = Get-Content -LiteralPath (Join-Path $e10Folder 'result.json') -Raw | ConvertFrom-Json
        if ($e10Result.development -or !$e10Result.onscreen -or $e10Result.offscreenRender -or $e10Result.invalidOnscreenFrames -ne 0 -or
            !$e10Result.hudActive -or !$e10Result.fogActive -or !$e10Result.e10NightStress -or
            $e10Result.peakLocalLights -ne 8 -or $e10Result.peakPulses -ne 32) { throw 'Night stress protocol failed.' }
    }
    Write-Output "$e10Id completed"
}
