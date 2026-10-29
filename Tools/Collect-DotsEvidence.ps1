param(
    [Parameter(Mandatory = $true)][string]$CaptureDirectory,
    [string]$OutputDirectory = 'Docs/Modules/DOTS/Evidence',
    [ValidatePattern('^[a-z0-9-]+$')][string]$Name = 'swarm',
    [string]$PlayerLogPath
)
$ErrorActionPreference = 'Stop'
$capturePath = (Resolve-Path -LiteralPath $CaptureDirectory).ProviderPath
$result = Get-Content -LiteralPath (Join-Path $capturePath 'result.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($result.errors.Count -ne 0) { throw 'Runtime errors were recorded. Inspect result.json before accepting evidence.' }
if ($PlayerLogPath) {
    $errors = @(Select-String -LiteralPath $PlayerLogPath -Pattern 'Exception:|Shader error|Leak Detected|remaining Allocations on the JobTempAlloc')
    if ($errors.Count -ne 0) { throw 'Player log contains an exception, shader error or native allocation leak.' }
}
$csv = Get-ChildItem -LiteralPath $capturePath -Filter '*.csv' | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
if (!$csv) { throw 'No capture CSV found.' }
$lines = @(Get-Content -LiteralPath $csv.FullName -Encoding UTF8)
$rows = @($lines | Where-Object { $_ -and !$_.StartsWith('#') } | ConvertFrom-Csv)
if ($rows.Count -eq 0) { throw 'Capture contains no frames.' }
$culture = [System.Globalization.CultureInfo]::InvariantCulture
function Measure-Series([double[]]$Values) {
    $sorted = @($Values | Sort-Object)
    if ($sorted.Count -eq 0) { throw 'Cannot summarize an empty series.' }
    $sum = ($sorted | Measure-Object -Sum).Sum
    [ordered]@{
        count = $sorted.Count
        minimum = $sorted[0]
        maximum = $sorted[$sorted.Count - 1]
        average = [math]::Round($sum / $sorted.Count, 4)
        median = $sorted[[math]::Min($sorted.Count - 1, [int][math]::Floor($sorted.Count * 0.5))]
        p95 = $sorted[[math]::Min($sorted.Count - 1, [int][math]::Floor($sorted.Count * 0.95))]
        p99 = $sorted[[math]::Min($sorted.Count - 1, [int][math]::Floor($sorted.Count * 0.99))]
    }
}
$frames = @($rows | ForEach-Object { [double]::Parse($_.frame_ms, $culture) })
$ticks = @($rows | Group-Object ecs_tick_id | ForEach-Object { [double]::Parse($_.Group[0].ecs_tick_ms, $culture) })
$enemyCounts = @($rows | ForEach-Object { [double]$_.enemies })
$projectileCounts = @($rows | ForEach-Object { [double]$_.projectiles })
$memory = @($rows | ForEach-Object { [double]$_.unity_allocated_bytes })
$summary = [ordered]@{
    name = $Name
    captureUtc = ($lines | Where-Object { $_.StartsWith('# UTC: ') } | Select-Object -First 1).Substring(7)
    unity = $result.unity
    cpu = $result.cpu
    gpu = $result.gpu
    scenario = $result.scenario
    offscreenRender = $result.offscreenRender
    cameraPosition = $result.cameraPosition
    # Join into a plain string: Windows PowerShell Get-Content strings carry PSDrive metadata,
    # which otherwise makes ConvertTo-Json recursively serialize provider objects.
    configuration = [string]::Join([Environment]::NewLine, @($lines | Where-Object { $_.StartsWith('#') -and !$_.StartsWith('# frame ms') }))
    framesMs = Measure-Series $frames
    uniqueTicksMs = Measure-Series $ticks
    sampledAgentEntities = Measure-Series $enemyCounts
    sampledProjectileEntities = Measure-Series $projectileCounts
    sampledUnityAllocatedBytes = Measure-Series $memory
    gcCounterAvailable = @($rows | Where-Object { [long]$_.gc_bytes -ge 0 }).Count -gt 0
    endOfRun = [ordered]@{ agents = $result.enemies; projectiles = $result.projectiles; hits = $result.hits; submittedInstances = $result.visible; drawSubmissions = $result.drawCalls }
    runtimeErrors = $result.errors
    rawCsv = "$Name.csv"
    screenshot = "$Name.png"
    measurementNotes = @(
        'Short offscreen smoke capture, not a certified interactive FPS benchmark.',
        'Entity counts are sampled in MonoBehaviour.Update after population refill; render/end-of-run snapshots can be lower after collision cleanup.',
        'Agent entity counts include the short death-animation lifetime.',
        'Tick percentiles deduplicate ecs_tick_id; memory is sampled at 1 Hz, not process RSS or GPU VRAM.',
        'Percentiles use sorted samples at floor(N*p), clamped to N-1.'
    )
}
$outputPath = [System.IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null
Copy-Item -LiteralPath $csv.FullName -Destination (Join-Path $outputPath "$Name.csv")
Copy-Item -LiteralPath (Join-Path $capturePath 'screenshot.png') -Destination (Join-Path $outputPath "$Name.png")
[System.IO.File]::WriteAllText((Join-Path $outputPath "$Name.json"), ($summary | ConvertTo-Json -Depth 8), (New-Object System.Text.UTF8Encoding($false)))
$summary | ConvertTo-Json -Depth 8
