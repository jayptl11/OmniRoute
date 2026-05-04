param([string]$ExcelPath, [string]$OutPath)

$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
$excel.DisplayAlerts = $false
$wb = $excel.Workbooks.Open($ExcelPath)
$ws = $wb.Sheets.Item("QLST-QLV (KCT)")
$lastRow = $ws.UsedRange.Rows.Count

$stores = @()
for ($r = 3; $r -le $lastRow; $r++) {
    $code = $ws.Cells.Item($r, 2).Text
    if ($code -eq "") { continue }
    $stores += [PSCustomObject]@{
        Code    = $code
        Name    = $ws.Cells.Item($r, 6).Text
        Address = $ws.Cells.Item($r, 7).Text
        Region  = $ws.Cells.Item($r, 5).Text
    }
}

$wb.Close($false)
$excel.Quit()
[System.Runtime.InteropServices.Marshal]::ReleaseComObject($excel) | Out-Null

Write-Host "Stores read: $($stores.Count)"

$sb = [System.Text.StringBuilder]::new()
[void]$sb.AppendLine("-- Insert stores from THONG TIN QLST-QLV 31.01.2026")
[void]$sb.AppendLine("-- Total: $($stores.Count) stores")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("BEGIN TRANSACTION;")
[void]$sb.AppendLine("")

foreach ($s in $stores) {
    $id  = [System.Guid]::NewGuid().ToString().ToUpper()
    $sc  = $s.Code.Replace("'", "''")
    $sn  = $s.Name.Replace("'", "''")
    $ad  = $s.Address.Replace("'", "''")
    $rg  = $s.Region.Replace("'", "''")
    $adv = if ($ad -eq "") { "NULL" } else { "N'$ad'" }
    $rgv = if ($rg -eq "") { "NULL" } else { "N'$rg'" }

    [void]$sb.AppendLine("IF NOT EXISTS (SELECT 1 FROM [Stores] WHERE [StoreCode] = N'$sc')")
    [void]$sb.AppendLine("    INSERT INTO [Stores] ([Id],[StoreCode],[StoreName],[Address],[Region],[ManagerId],[MaxCapacity],[IsActive],[CreatedAt])")
    [void]$sb.AppendLine("    VALUES ('$id', N'$sc', N'$sn', $adv, $rgv, NULL, 20, 1, GETUTCDATE());")
    [void]$sb.AppendLine("")
}

[void]$sb.AppendLine("COMMIT TRANSACTION;")

[System.IO.File]::WriteAllText($OutPath, $sb.ToString(), [System.Text.Encoding]::UTF8)
Write-Host "Done: $OutPath"
