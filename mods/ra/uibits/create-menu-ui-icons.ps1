# Builds menu-sized Shockwave logo and 32x32 colorful world-map button icon.
Add-Type -AssemblyName System.Drawing

$dir = Split-Path -Parent $MyInvocation.MyCommand.Path

# --- Shockwave logo (scaled for bottom-right menu chrome, padded to POT texture) ---
$srcPath = Join-Path $dir 'SHOCKWAVELOGO.png'
$logoW = 168
$logoH = 32
$sheetW = 512
$sheetH = 64

$src = [System.Drawing.Image]::FromFile($srcPath)
$sheet = New-Object System.Drawing.Bitmap $sheetW, $sheetH, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($sheet)
$g.Clear([System.Drawing.Color]::FromArgb(0, 0, 0, 0))
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
$g.DrawImage($src, 0, 0, $logoW, $logoH)
$g.Dispose()
$src.Dispose()
$menuPath = Join-Path $dir 'SHOCKWAVELOGO-menu.png'
$sheet.Save($menuPath, [System.Drawing.Imaging.ImageFormat]::Png)
$sheet.Dispose()
Write-Host "Wrote $menuPath (sheet ${sheetW}x${sheetH}, logo ${logoW}x${logoH})"

# --- 32x32 colorful world map (G=green, R=brown, W=polar, .=blue sea) ---
$sea = [System.Drawing.Color]::FromArgb(255, 35, 105, 195)
$green = [System.Drawing.Color]::FromArgb(255, 75, 155, 55)
$brown = [System.Drawing.Color]::FromArgb(255, 145, 100, 45)
$white = [System.Drawing.Color]::FromArgb(255, 230, 245, 255)

$world = @(
    '................................',
    '..............WWWW..............',
    '............WWWWWWWW............',
    '..........WWWWWWWWWWWW..........',
    '........GGGGGGGGGGGGGGGG........',
    '......GGGGGGGGGGGGGGGGGGGG......',
    '....RRRRRRRGGGGGGGGGGGGGGGG.....',
    '...RRRRRRRRRGGGGGGGGGGGGGGGG....',
    '..RRRR....RRGGGGGG....GGGGGGG...',
    '..RRR......GGGGGG......GGGGGG...',
    '.RRR.......GGGGGG.......GGGGG...',
    '.RR........GGGGGG........GGGG...',
    'RR.........GGGGG.........GGG....',
    'R..........GGGGG..........GG....',
    '...........GGGGG................',
    '...........GGGGG................',
    '..........GGGGGG................',
    '.........GGGGGGG................',
    '........GGGGGGGG................',
    '.......GGGGGGGGG................',
    '......GGGGGGGGGG................',
    '.....GGGGGGGGGGG................',
    '....GGGGGGGGGGGG................',
    '...GGGGGGGGGGGGG................',
    '..GGGGGGGGGGGGGG................',
    '.GGGGGGGGGGGGGGG................',
    '..........GGGGGG................',
    '...........GGGGG................',
    '............GGGG................',
    '.............GGG................',
    '..............GG................',
    '................................'
)

$bmp = New-Object System.Drawing.Bitmap 32, 32
for ($y = 0; $y -lt 32; $y++) {
    $row = $world[$y]
    for ($x = 0; $x -lt 32; $x++) {
        $ch = $row[$x]
        $color = switch ($ch) {
            'G' { $green }
            'R' { $brown }
            'W' { $white }
            default { $sea }
        }
        $bmp.SetPixel($x, $y, $color)
    }
}

$iconPath = Join-Path $dir 'worldmap-icon.png'
$bmp.Save($iconPath, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
Write-Host "Wrote $iconPath (32x32)"
