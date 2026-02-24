# Data sources, licensing, and attribution (geogen)

This generator uses open datasets. When packaging `.oramap` outputs, geogen writes an `Attributions` block into `map.yaml` so downstream users can see sources and versions.

## OpenStreetMap (OSM)
- License: ODbL 1.0 (© OpenStreetMap contributors)
- Usage: roads, waterways, water areas, and buildings.
- Caching: Overpass responses cached by default at `--osm-cache-dir` (default `.cache/osm`). Disable with `--no-osm-cache`.

## ESA WorldCover 10 m
- License: CC BY 4.0
- Usage: urban/built-up mask, forest/wood mask for vegetation tuning.
- CLI flags:
  - `--use-worldcover` to enable.
  - `--worldcover-path` to point at a local GeoTIFF/COG.
  - `--worldcover-year` to record the dataset year in `map.yaml` Attributions.

## JRC Global Surface Water (GSW)
- License: CC BY 4.0
- Usage: optional augmentation of water features using occurrence rasters.
- CLI flags:
  - `--augment-water-gsw` to enable.
  - `--gsw-path` to point at a local occurrence GeoTIFF/COG.
  - `--gsw-min-occurrence` to threshold permanent water (e.g., 75).
  - `--gsw-version` to record the dataset version/year in `map.yaml` Attributions.

## Example Attributions block
The generator populates fields automatically when features are used and when metadata flags are provided:

```yaml
Attributions:
  - Source: OpenStreetMap
    License: ODbL-1.0
    URL: https://www.openstreetmap.org
  - Source: ESA WorldCover
    License: CC-BY-4.0
    Year: 2021
    URL: https://worldcover2020.esa.int/
  - Source: JRC Global Surface Water
    License: CC-BY-4.0
    Version: 2021
    URL: https://global-surface-water.appspot.com
```

Notes:
- If `--use-worldcover`/`--augment-water-gsw` are not used, those entries will be omitted.
- Providing `--worldcover-year` and `--gsw-version` improves reproducibility.
