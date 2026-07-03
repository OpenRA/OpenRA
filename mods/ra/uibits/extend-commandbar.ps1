# Extends RA command bar: 2px stance border + divider + slot + cap (476px).
# Stitches top/bottom horizontal chrome across the formation divider.
# Run: powershell -File mods/ra/uibits/extend-commandbar.ps1

Add-Type -AssemblyName System.Drawing

$path = Join-Path $PSScriptRoot "sidebar.png"
$sourceUrl = "https://github.com/OpenRA/OpenRA/raw/bleed/mods/ra/uibits/sidebar.png"
$tempPath = Join-Path $PSScriptRoot "sidebar-vanilla.png"

Invoke-WebRequest -Uri $sourceUrl -OutFile $tempPath

$BarHeight = 44
$BorderWidth = 2
$DividerStart = 283
$DividerWidth = 6
$SlotStart = 391
$SlotWidth = 34
$CapStart = 425
$CapWidth = 9
$InsertAt = $CapStart + $BorderWidth
$NewCapStart = $InsertAt + $DividerWidth + $SlotWidth
$NewBarWidth = $NewCapStart + $CapWidth
# Top chrome (y=0-9) and bottom bezel (y=34-35): overwrite old right-cap corner
# pixels at x=425-426 with the command/stance divider pattern (x=281-288).
$BezelRows = 0..9 + @(34, 35)
$BezelRefStart = 281

$src = [System.Drawing.Bitmap]::FromFile((Resolve-Path $tempPath))
$outPath = Join-Path $PSScriptRoot "sidebar-new.png"

for ($y = 0; $y -lt $BarHeight; $y++) {
    $cap = New-Object System.Drawing.Color[] $CapWidth
    for ($i = 0; $i -lt $CapWidth; $i++) {
        $cap[$i] = $src.GetPixel($CapStart + $i, $y)
    }

    for ($i = 0; $i -lt $DividerWidth; $i++) {
        $src.SetPixel($InsertAt + $i, $y, $src.GetPixel($DividerStart + $i, $y))
    }

    for ($i = 0; $i -lt $SlotWidth; $i++) {
        $src.SetPixel($InsertAt + $DividerWidth + $i, $y, $src.GetPixel($SlotStart + $i, $y))
    }

    for ($i = 0; $i -lt $CapWidth; $i++) {
        $src.SetPixel($NewCapStart + $i, $y, $cap[$i])
    }
}

foreach ($y in $BezelRows) {
    for ($i = 0; $i -lt ($BorderWidth + $DividerWidth); $i++) {
        $src.SetPixel($CapStart + $i, $y, $src.GetPixel($BezelRefStart + $i, $y))
    }
}

$src.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
$src.Dispose()
Remove-Item -Force $tempPath
Move-Item -Force $outPath $path
Write-Host "Command bar ${NewBarWidth}px, divider at $InsertAt, bezel rows stitched"
