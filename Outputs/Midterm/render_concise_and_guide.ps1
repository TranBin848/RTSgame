$ErrorActionPreference = 'Stop'
$taskFiles = @('Bao_cao_giua_ky_KLTN_Tran_Ngoc_Bin_23520150_Rut_gon', 'Giai_thich_Procedural_Generation_RTSgame2026')
$wordApp = $null
try {
    $wordApp = New-Object -ComObject Word.Application
    $wordApp.Visible = $false
    $wordApp.DisplayAlerts = 0
    foreach ($taskFile in $taskFiles) {
        $taskDocument = $null
        try {
            $taskOutput = Join-Path $PSScriptRoot ('qa_' + $taskFile)
            [System.IO.Directory]::CreateDirectory($taskOutput) | Out-Null
            $taskDocument = $wordApp.Documents.Open((Join-Path $PSScriptRoot ($taskFile + '.docx')), $false, $true)
            $taskDocument.Repaginate()
            $taskDocument.ExportAsFixedFormat((Join-Path $taskOutput ($taskFile + '.pdf')), 17)
            Write-Output ($taskFile + ': ' + $taskDocument.ComputeStatistics(2) + ' pages')
        } finally {
            if ($null -ne $taskDocument) { $taskDocument.Close(0); [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($taskDocument) }
        }
    }
} finally {
    if ($null -ne $wordApp) { $wordApp.Quit(); [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($wordApp) }
}
