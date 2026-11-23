param(
    [Parameter(Mandatory)][string]$BuildDirectory,
    [Parameter(Mandatory)][string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$build = (Resolve-Path -LiteralPath $BuildDirectory).Path
$exe = Join-Path $build 'RTSgame2026.exe'
if (!(Test-Path -LiteralPath $exe)) { throw 'Windows Player executable is missing.' }
$output = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $output) { throw 'Use a new output directory for the run group.' }
$runs = @(
    @{ Id = 'seed1-startup-first'; Flags = @('--g5-startup-only') }
    @{ Id = 'seed1-run1'; Flags = @() }
    @{ Id = 'seed1-run2'; Flags = @() }
    @{ Id = 'seed1-run3'; Flags = @() }
    @{ Id = 'seed1-startup-warm'; Flags = @('--g5-startup-only') }
    @{ Id = 'seed1543-military'; Flags = @('--g5-military-1543') }
    @{ Id = 'gameplay-smoke'; Flags = @() }
)
foreach ($run in $runs) {
    $folder = Join-Path $output $run.Id
    New-Item -ItemType Directory -Path $folder -Force | Out-Null
    $launch = @('-logFile', ('"' + (Join-Path $folder 'Player.log') + '"'))
    if ($run.Id -eq 'gameplay-smoke') {
        $launch += @('--gameplay-smoke', '--gameplay-output', ('"' + $folder + '"'))
        $style = 'Hidden'
    } else {
        $launch += @('--progression-smoke', '--g5-onscreen', '--swarm-output', ('"' + $folder + '"'),
            '-screen-fullscreen', '1', '-screen-width', '1920', '-screen-height', '1080', '-window-mode', 'borderless')
        $launch += $run.Flags
        $style = 'Normal'
    }
    $player = Start-Process -FilePath $exe -ArgumentList $launch -WorkingDirectory $build -WindowStyle $style -PassThru
    Write-Output "$($run.Id) PID=$($player.Id)"
    if (!$player.WaitForExit(190000)) { throw "Timed out waiting for PID $($player.Id) from $($run.Id)." }
    if ($player.ExitCode -ne 0) { throw "$($run.Id) exited with code $($player.ExitCode)." }
    if ($run.Id -ne 'gameplay-smoke') {
        $result = Get-Content -LiteralPath (Join-Path $folder 'result.json') -Raw | ConvertFrom-Json
        if ($result.development -or !$result.onscreen -or $result.offscreenRender -or $result.screenWidth -ne 1920 -or
            $result.screenHeight -ne 1080 -or $result.invalidOnscreenFrames -ne 0 -or !$result.hudActive -or !$result.fogActive) {
            throw "$($run.Id) did not meet the onscreen Release protocol. Check result.json."
        }
    }
    Write-Output "$($run.Id) completed"
}
