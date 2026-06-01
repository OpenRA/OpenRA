# Recolors RA menu chrome from red to purple so local dev builds are easy to spot.
# Run from repo root: powershell -File mods/ra/uibits/apply-dev-purple-theme.ps1
# Restores from *.bak when present (created on first run).

Add-Type -AssemblyName System.Drawing

function Convert-RedToPurple([System.Drawing.Color]$c) {
	if ($c.A -lt 8) { return $c }
	$r = [int]$c.R; $g = [int]$c.G; $b = [int]$c.B
	if ($r -lt 35) { return $c }
	if ($r -le $g + 12 -and $r -le $b + 12) { return $c }
	if ($g -gt $b + 40 -and $r -gt $g) { return $c }

	$newR = [Math]::Min(255, [int]($r * 0.48 + $g * 0.12 + $b * 0.08))
	$newG = [Math]::Min(255, [int]($g * 0.88 + $r * 0.04))
	$newB = [Math]::Min(255, [int]($b * 0.35 + $r * 0.52 + $g * 0.06))
	return [System.Drawing.Color]::FromArgb($c.A, $newR, $newG, $newB)
}

function Recolor-Png([string]$path) {
	$bak = "$path.bak"
	if (Test-Path $bak) {
		Copy-Item $bak $path -Force
	}
	elseif (-not (Test-Path $path)) {
		Write-Warning "Missing $path"
		return
	}
	else {
		Copy-Item $path $bak
	}

	$src = [System.Drawing.Bitmap]::FromFile((Resolve-Path $path))
	$out = New-Object System.Drawing.Bitmap $src.Width, $src.Height
	for ($y = 0; $y -lt $src.Height; $y++) {
		for ($x = 0; $x -lt $src.Width; $x++) {
			$out.SetPixel($x, $y, (Convert-RedToPurple ($src.GetPixel($x, $y))))
		}
	}
	$src.Dispose()
	$out.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
	$out.Dispose()
	Write-Host "Recolored $path"
}

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
foreach ($name in @('dialog.png', 'loadscreen.png', 'loadscreen-2x.png', 'loadscreen-3x.png')) {
	Recolor-Png (Join-Path $here $name)
}

Write-Host 'Done. Window title also uses [Local Dev] in mods/ra/fluent/ra.ftl'
