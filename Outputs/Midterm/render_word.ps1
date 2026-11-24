$ErrorActionPreference = 'Stop'
$sourcePath = Join-Path $PSScriptRoot 'Bao_cao_giua_ky_KLTN_Tran_Ngoc_Bin_23520150.docx'
$renderDirectory = Join-Path $PSScriptRoot 'render'
[System.IO.Directory]::CreateDirectory($renderDirectory) | Out-Null
$pdfPath = Join-Path $renderDirectory 'report-layout.pdf'
$wordApp = $null
$reportDocument = $null
try {
    $wordApp = New-Object -ComObject Word.Application
    $wordApp.Visible = $false
    $wordApp.DisplayAlerts = 0
    $reportDocument = $wordApp.Documents.Open($sourcePath, $false, $true)
    $reportDocument.Repaginate()
    $reportDocument.ExportAsFixedFormat($pdfPath, 17)
    Write-Output ('Rendered pages: ' + $reportDocument.ComputeStatistics(2))
    Write-Output $pdfPath
} finally {
    if ($null -ne $reportDocument) { $reportDocument.Close(0); [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($reportDocument) }
    if ($null -ne $wordApp) { $wordApp.Quit(); [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($wordApp) }
}
