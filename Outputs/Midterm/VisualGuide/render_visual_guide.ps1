$ErrorActionPreference = 'Stop'
$taskSource = Join-Path (Split-Path $PSScriptRoot) 'Giai_thich_Procedural_Generation_Minh_hoa_tile.docx'
$taskQa = Join-Path $PSScriptRoot 'qa'
[System.IO.Directory]::CreateDirectory($taskQa) | Out-Null
$wordApp = $null
$taskDocument = $null
try {
    $wordApp = New-Object -ComObject Word.Application
    $wordApp.Visible = $false
    $wordApp.DisplayAlerts = 0
    $taskDocument = $wordApp.Documents.Open($taskSource, $false, $true)
    $taskDocument.Repaginate()
    $taskDocument.ExportAsFixedFormat((Join-Path $taskQa 'visual-guide.pdf'), 17)
    Write-Output ('Pages: ' + $taskDocument.ComputeStatistics(2))
} finally {
    if ($null -ne $taskDocument) { $taskDocument.Close(0); [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($taskDocument) }
    if ($null -ne $wordApp) { $wordApp.Quit(); [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($wordApp) }
}
