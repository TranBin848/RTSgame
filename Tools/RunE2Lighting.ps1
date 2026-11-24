param(
    [Parameter(Mandatory)][string]$BuildDirectory,
    [Parameter(Mandatory)][string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$e2Build = (Resolve-Path -LiteralPath $BuildDirectory).Path
$e2Exe = Join-Path $e2Build 'RTSgame2026.exe'
$e2Output = [IO.Path]::GetFullPath($OutputDirectory)
if (!(Test-Path -LiteralPath $e2Exe)) { throw 'Player executable missing.' }
if (Test-Path -LiteralPath $e2Output) { throw 'Use a new output directory; previous evidence is kept.' }
foreach ($e2Id in @('baseline1', 'candidate1', 'baseline2', 'candidate2')) {
    $e2Folder = Join-Path $e2Output $e2Id
    New-Item -ItemType Directory -Path $e2Folder -Force | Out-Null
    $e2Args = @('-logFile', ('"' + (Join-Path $e2Folder 'Player.log') + '"'),
        '--progression-smoke', '--g5-onscreen', '--swarm-output', ('"' + $e2Folder + '"'),
        '-screen-fullscreen', '1', '-screen-width', '1920', '-screen-height', '1080', '-window-mode', 'borderless')
    if ($e2Id.StartsWith('baseline')) { $e2Args += '--e2-lighting-off' }
    # Visible foreground Player is required by the E2/G5 onscreen measurement contract.
    $e2Player = Start-Process -FilePath $e2Exe -ArgumentList $e2Args -WorkingDirectory $e2Build -WindowStyle Normal -PassThru
    Write-Output "$e2Id started PID=$($e2Player.Id)"
    if (!$e2Player.WaitForExit(190000)) { throw "$e2Id timed out; inspect Player before retrying." }
    if ($e2Player.ExitCode -ne 0) { throw "$e2Id exit code $($e2Player.ExitCode)" }
    $e2Result = Get-Content -LiteralPath (Join-Path $e2Folder 'result.json') -Raw | ConvertFrom-Json
    if ($e2Result.errors.Count -ne 0 -or $e2Result.development -or !$e2Result.onscreen -or $e2Result.offscreenRender -or
        $e2Result.screenWidth -ne 1920 -or $e2Result.screenHeight -ne 1080 -or $e2Result.invalidOnscreenFrames -ne 0 -or
        !$e2Result.hudActive -or !$e2Result.fogActive -or $e2Result.seed -ne 1 -or $e2Result.waveSeed -ne 8001) {
        throw "$e2Id did not meet the measurement contract; preserve its result."
    }
    if ($e2Id.StartsWith('baseline')) {
        if ($e2Result.e2LightingEnabled -or $e2Result.peakLocalLights -ne 0 -or $e2Result.peakGlowGroups -ne 0) { throw 'Baseline still had E2 lighting.' }
    } elseif (!$e2Result.e2LightingEnabled -or $e2Result.peakLocalLights -lt 1 -or $e2Result.peakLocalLights -gt 8) { throw 'Candidate did not exercise the light budget.' }
    Write-Output "$e2Id completed; peak lights=$($e2Result.peakLocalLights), glow groups=$($e2Result.peakGlowGroups)"
}
