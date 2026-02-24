# geogen (OpenRA real-world map generator)

Generates OpenRA RA `.oramap` maps from real-world geographic data using MGRS coordinates, OpenStreetMap features, and optional satellite-derived raster datasets.

## Features

- RA TEMPERATE tileset targeting (2048x2048 at 4 m/cell default)
- AOI from MGRS center coordinate with configurable map size (up to 16384x16384)
- OSM overlay: roads (width-aware per highway type), water (areas + waterways with river smoothing + shoreline), vegetation (tree actors with density/spacing controls), buildings (civilian actors with placement strategies)
- Optional datasets: ESA WorldCover 10 m (built-up/forest masks), JRC Global Surface Water (water augmentation)
- map.yaml embeds GeoTransform metadata (UTM zone, meters_per_cell, origin, grid size) and Attributions (dataset licenses/versions)
- Packages `.oramap` zip (map.yaml + map.bin) ready for OpenRA
- Optional auto-install into OpenRA maps directory (cross-platform: Windows, macOS, Linux)
- GeoTransform validation (cell-to-latlon round-trip error checking)
- OSM Overpass response caching for reproducibility

## Prerequisites

**Python 3.10+** (tested with 3.10 and 3.12)

### Install dependencies

```powershell
pip install -r requirements.txt
```

Required packages: `mgrs`, `utm`, `requests`

### Optional: rasterio (for WorldCover/GSW features)

```powershell
pip install rasterio
```

Or via conda:

```powershell
conda install -c conda-forge rasterio
```

Without rasterio, `--use-worldcover` and `--augment-water-gsw` are silently disabled (a warning is printed to stderr).

## Usage

**Important:** Run from the `Map_Generator/` directory (one level above `geogen/`), or run directly from inside `geogen/`.

### From inside `Map_Generator/`:

```powershell
python -m geogen.cli --mgrs "17RMH9336033739" --pretty
```

### From inside `Map_Generator/geogen/`:

```powershell
python -m cli --mgrs "17RMH9336033739" --pretty
```

### PowerShell note

PowerShell does **not** support `\` for line continuation (that's bash). Either put the entire command on one line, or use backtick `` ` `` at the end of each line.

## Common Recipes

### 1) Generate a full overlay map (roads, water, vegetation, buildings)

```powershell
python -m cli --mgrs "17RMH9336033739" --overlay-osm --overlay-osm-buildings --write-oramap "F:\Where\YOU\Downloaded_OpenRA_TAK_Support\GeoMaps\TheKeys_realworld.oramap" --players 8 --place-spawns --road-width-m 8 --waterway-width-m 6 --veg-density 0.15 --max-veg-actors 4000 --veg-min-spacing 2 --veg-patch-size 32 --veg-patch-boost 1.5 --suppress-veg-near-roads 1 --suppress-veg-near-buildings 1 --building-placement-mode aggressive --building-search-radius 3 --pretty
```

### 2) Use optional raster datasets (requires rasterio)

```powershell
python -m cli --mgrs "18STD5154840177" --overlay-osm --overlay-osm-buildings --use-worldcover --worldcover-path ../data/worldcover/ESA_WorldCover_10m_2021.tif --augment-water-gsw --gsw-path ../data/gsw/occurrence_80W_40Nv1_4_2021.tif --gsw-min-occurrence 75 --worldcover-year 2021 --gsw-version 2021 --write-oramap "F:\Where\YOU\Downloaded_OpenRA_TAK_Support\GeoMaps\18STD5154840177_realworld.oramap" --pretty
```

### 3) OSM summary only (no .oramap generation)

```powershell
python -m cli --mgrs "33TWN1234567890" --cells 2048 --meters-per-cell 4 --osm-summary --pretty
```

### 4) Generate and auto-install to OpenRA maps directory

```powershell
python -m cli --mgrs "18STD5154840177" --overlay-osm --overlay-osm-buildings --write-oramap "F:\Where\YOU\Downloaded_OpenRA_TAK_Support\GeoMaps\18STD5154840177_realworld.oramap" --install-openra --install-release 20250330 --pretty
```

## Installing Maps into OpenRA

The `--write-oramap` flag writes the `.oramap` file to the specified path. To play the map in OpenRA, copy the `.oramap` file to your OpenRA maps directory:

| Platform | Maps directory |
|----------|---------------|
| **Windows** | `%APPDATA%\OpenRA\maps\ra\` |
| **macOS** | `~/Library/Application Support/OpenRA/maps/ra/` |
| **Linux** | `~/.config/openra/maps/ra/` |

For release builds, maps go in a subdirectory like `release-20250330/`. For dev builds from source, use `{DEV_VERSION}/`:

```
%APPDATA%\OpenRA\maps\ra\{DEV_VERSION}\
```

The `--install-openra` flag automates this copy. Use `--install-release <tag>` to target a specific release, or `--install-path <dir>` to specify an exact directory.

## CLI Reference

### Core options

| Flag | Default | Description |
|------|---------|-------------|
| `--mgrs` | (required) | MGRS center coordinate (e.g., `18STD5154840177`) |
| `--cells` | 2048 | Map size in cells per side (max: 16384) |
| `--meters-per-cell` | 4.0 | Meters per cell |
| `--rotation-deg` | 0.0 | Rotation relative to UTM north (degrees) |
| `--tileset` | TEMPERAT | Tileset identifier for map.yaml |
| `--pretty` | off | Pretty-print JSON output |

### Map file generation

| Flag | Default | Description |
|------|---------|-------------|
| `--write-oramap PATH` | (none) | Write .oramap zip to PATH |
| `--title` | "RealWorld \<MGRS\>" | Map title in map.yaml |
| `--author` | OpenRA_WoW | Author in map.yaml |
| `--categories` | RealWorld | Comma-separated categories |
| `--players` | 8 | Number of playable spawn slots (0-8) |
| `--place-spawns` | off | Place mpspawn actors for each player |

### Auto-install

| Flag | Default | Description |
|------|---------|-------------|
| `--install-openra` | off | Copy .oramap to OpenRA maps directory |
| `--install-release` | (none) | Target release subdirectory (e.g., `20250330`) |
| `--install-path` | (none) | Explicit target directory (overrides auto-detection) |

### OSM overlay

| Flag | Default | Description |
|------|---------|-------------|
| `--overlay-osm` | off | Enable road/water/vegetation overlay |
| `--overlay-osm-buildings` | off | Enable building footprint overlay |
| `--osm-summary` | off | Fetch and print OSM feature counts |
| `--overpass-url` | overpass-api.de | Overpass API endpoint |
| `--overpass-timeout` | 25 | Overpass query timeout in seconds |
| `--no-roads` | off | Disable road overlay |
| `--no-water` | off | Disable water overlay |
| `--no-vegetation` | off | Disable vegetation overlay |
| `--no-buildings` | off | Disable building overlay |

### Road tuning

| Flag | Default | Description |
|------|---------|-------------|
| `--road-width-m` | 8.0 | Base road drawing width (meters) |

### Water tuning

| Flag | Default | Description |
|------|---------|-------------|
| `--waterway-width-m` | 6.0 | Waterway drawing width (meters) |

### Vegetation tuning

| Flag | Default | Description |
|------|---------|-------------|
| `--veg-density` | 0.15 | Fraction [0..1] of forest cells to place a tree |
| `--max-veg-actors` | 4000 | Maximum tree actors |
| `--veg-min-spacing` | 2 | Min Chebyshev distance between trees (tiles) |
| `--veg-patch-size` | 32 | Patch size for local density calculation |
| `--veg-patch-boost` | 1.5 | Multiplier for high-density forest patches |
| `--suppress-veg-near-roads` | 1 | Suppress trees within N tiles of roads (0 disables) |
| `--suppress-veg-near-buildings` | 1 | Suppress trees within N tiles of buildings (0 disables) |

### Building tuning

| Flag | Default | Description |
|------|---------|-------------|
| `--building-density` | 1.0 | Fraction [0..1] of OSM buildings to place |
| `--max-buildings` | 1200 | Maximum building actors |
| `--building-search-radius` | 2 | Local search radius (tiles) around anchor |
| `--building-placement-mode` | accurate | `accurate` / `fallback` / `aggressive` |
| `--debug-building-audit` | (none) | Write per-building placement CSV for debugging |

### Optional raster datasets (require rasterio)

| Flag | Default | Description |
|------|---------|-------------|
| `--use-worldcover` | off | Enable ESA WorldCover vegetation/built-up mask |
| `--worldcover-path` | (none) | Path to WorldCover GeoTIFF |
| `--worldcover-year` | (none) | Dataset year for metadata (e.g., 2021) |
| `--augment-water-gsw` | off | Augment water from JRC Global Surface Water |
| `--gsw-path` | (none) | Path to GSW raster |
| `--gsw-min-occurrence` | 75.0 | Min occurrence % for water classification (0-100) |
| `--gsw-version` | (none) | Dataset version for metadata |

### Caching

| Flag | Default | Description |
|------|---------|-------------|
| `--osm-cache-dir` | .cache/osm | Directory for Overpass response cache |
| `--no-osm-cache` | off | Disable OSM caching |

### Validation

| Flag | Default | Description |
|------|---------|-------------|
| `--validate-geotransform` | off | Print GeoTransform round-trip validation |
| `--validate-cell` | (none) | Validate a specific cell (e.g., `0,0`) |
| `--validate-latlon` | (none) | Validate a specific coordinate (e.g., `34.59,-77.37`) |

## Output

JSON output includes:
- `input` — echo of input parameters
- `center` — lat/lon and UTM coordinates of MGRS center
- `bounds` — corner coordinates, UTM bbox, extent in meters
- `osm_summary` — feature counts by type (when `--osm-summary`)
- `oramap` — file path, size, overlay stats (when `--write-oramap`)
- `validation` — round-trip error samples (when `--validate-geotransform`)

## Data Sources & Licensing

See [DATA_SOURCES.md](DATA_SOURCES.md) for full details. Generated maps embed attribution metadata in map.yaml.

| Dataset | License | Usage |
|---------|---------|-------|
| OpenStreetMap | ODbL 1.0 | Roads, water, buildings, landuse |
| ESA WorldCover 10 m | CC BY 4.0 | Vegetation/built-up mask |
| JRC Global Surface Water | CC BY 4.0 | Water augmentation |
