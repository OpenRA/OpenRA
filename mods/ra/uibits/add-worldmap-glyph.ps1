# Adds 16x16 pixel world-map glyph to glyphs.png / 2x / 3x at editor row.
Add-Type -AssemblyName System.Drawing

# Flat pixel world map inside a frame (O=land, B=border, .=transparent)
$glyph = @(
    '............',
    '............',
    '..BBBBBBBB..',
    '.B........B.',
    '.B.OO....O.B',
    '.B.OO....OO.',
    '.B..OO.OOO.B',
    '.B...OOO...B',
    '.B........B.',
    '..BBBBBBBB..',
    '............',
    '............',
    '............',
    '............',
    '............',
    '............'
)

function Set-WorldMapGlyph($path, $originX, $originY, $scale) {
    $src = [System.Drawing.Bitmap]::FromFile($path)
    $bmp = New-Object System.Drawing.Bitmap $src.Width, $src.Height
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.DrawImage($src, 0, 0)
    $g.Dispose()
    $src.Dispose()

    $white = [System.Drawing.Color]::FromArgb(255, 255, 255, 255)
    $gray = [System.Drawing.Color]::FromArgb(255, 200, 200, 200)

    for ($y = 0; $y -lt 16; $y++) {
        $row = $glyph[$y]
        for ($x = 0; $x -lt 16; $x++) {
            if ($row[$x] -eq 'O') { $color = $white }
            elseif ($row[$x] -eq 'B') { $color = $gray }
            else { continue }
            for ($dy = 0; $dy -lt $scale; $dy++) {
                for ($dx = 0; $dx -lt $scale; $dx++) {
                    $bmp.SetPixel($originX + $x * $scale + $dx, $originY + $y * $scale + $dy, $color)
                }
            }
        }
    }

    $temp = "$path.tmp"
    $bmp.Save($temp, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Move-Item -Force $temp $path
}

$dir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-WorldMapGlyph (Join-Path $dir 'glyphs.png') 135 187 1
Set-WorldMapGlyph (Join-Path $dir 'glyphs-2x.png') 270 374 2
Set-WorldMapGlyph (Join-Path $dir 'glyphs-3x.png') 405 561 3
Write-Host 'World map glyph added at editor row x=135.'
