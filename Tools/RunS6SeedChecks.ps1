param([Parameter(Mandatory=$true)][int]$Seed)
$ErrorActionPreference = 'Stop'
$s6Input = Join-Path $env:TEMP 'RTS-S6-SeedChecks.json'
$s6Log = Join-Path $PSScriptRoot '../Docs/Modules/ProceduralGeneration/S6-FinalChecks-2026-11-16.txt'
function Invoke-S6Tool($s6Tool, $s6Arguments) {
    $s6Arguments | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $s6Input -Encoding utf8
    $s6Output = & npx.cmd --yes unity-mcp-cli run-tool $s6Tool --input-file $s6Input 2>&1
    $s6Result = $LASTEXITCODE
    $s6Output | Tee-Object -FilePath $s6Log -Append | Out-Host
    if ($s6Result -ne 0) { throw "Unity MCP $s6Tool failed; stopping seed $Seed." }
}
function Invoke-S6Check($s6File, $s6Class, $s6Method) {
    "seed=$Seed $s6Class.$s6Method" | Tee-Object -FilePath $s6Log -Append | Out-Host
    Invoke-S6Tool 'script-execute' @{csharpCode=[IO.File]::ReadAllText((Join-Path $PSScriptRoot $s6File));className=$s6Class;methodName=$s6Method;timeout=60}
}
"S6 seed=$Seed; paused Play required; temporary asset Seed property only, restore1 after all checks. No SaveAssets." | Tee-Object -FilePath $s6Log -Append | Out-Host
Invoke-S6Tool 'script-execute' @{csharpCode=('var s=AssetDatabase.LoadAssetAtPath<ProceduralMapSettings>("Assets/Game/ProceduralGeneration/Settings/StrategicProceduralMapSettings.asset");s.Seed='+$Seed+';UnityEngine.SceneManagement.SceneManager.LoadScene("ProceduralGame");return s.Seed;')}
Invoke-S6Tool 'script-execute' @{csharpCode='EditorApplication.Step();return 1;'}
Invoke-S6Check 'ValidateD3Units.cs' 'D3UnitValidation' 'CheckBindings'
Invoke-S6Check 'ValidateS6Economy.cs' 'S6EconomyValidation' 'Run'
Invoke-S6Check 'ValidateS6Economy.cs' 'S6EconomyValidation' 'RunConstruction'
Invoke-S6Check 'ValidateS6Economy.cs' 'S6EconomyValidation' 'CheckFogCamera'
Invoke-S6Check 'ValidateS6Acceptance.cs' 'S6AcceptanceValidation' 'CheckInput'
Invoke-S6Check 'ValidateS6Acceptance.cs' 'S6AcceptanceValidation' 'RunDays'
