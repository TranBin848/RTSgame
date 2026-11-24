$ErrorActionPreference = 'Stop'
$e9Cli = 'C:/Users/ACER/AppData/Local/npm-cache/_npx/5ced38e42099c3f2/node_modules/unity-mcp-cli/bin/unity-mcp-cli.js'
function Invoke-E9Tool([string]$e9Input) {
    & node.exe $e9Cli run-tool script-execute --input-file $e9Input
    if ($LASTEXITCODE -ne 0) { throw "Unity call failed: $e9Input" }
}
$e9Seeds = @(0,1,1543,137,901)
try {
    foreach ($e9Seed in $e9Seeds) {
        $e9Code = 'AssetDatabase.LoadAssetAtPath<ProceduralMapSettings>("Assets/Game/ProceduralGeneration/Settings/StrategicProceduralMapSettings.asset").Seed=' + $e9Seed + '; return "Temporary matrix seed set";'
        [IO.File]::WriteAllText((Join-Path $PWD '.utmp/e9-matrix-seed.json'), (@{ csharpCode=$e9Code } | ConvertTo-Json))
        Invoke-E9Tool '.utmp/e9-matrix-seed.json'
        $e9Policies = if ($e9Seed -in @(137,901)) { @('Control','Economy') } else { @('Control','Economy','Military','Expansion') }
        foreach ($e9Policy in $e9Policies) {
            Write-Output "E9 START seed=$e9Seed policy=$e9Policy"
            Invoke-E9Tool '.utmp/e9-fresh-scene.json'
            Invoke-E9Tool '.utmp/e3-step.json'
            Invoke-E9Tool ('.utmp/e9-' + $e9Policy + '.json')
            Write-Output "E9 END seed=$e9Seed policy=$e9Policy"
        }
    }
} finally {
    Invoke-E9Tool '.utmp/e3-stop.json'
    Invoke-E9Tool '.utmp/e8-seed1.json'
}
