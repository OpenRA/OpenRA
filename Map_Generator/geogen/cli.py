#!/usr/bin/env python3
"""
Minimal CLI to compute AOI bounds from an MGRS center for RA TEMPERATE map generation.

Usage:
  python tools/geogen/cli.py --mgrs "33TWN1234567890" --cells 512 --meters-per-cell 4 --pretty
"""
from __future__ import annotations

import argparse
import datetime
import hashlib
import json
import math
import os
import platform
import random
import shutil
import sys
import time
from typing import Dict, Any, List, Tuple, Optional, Set

import mgrs  # type: ignore
import utm   # type: ignore
import requests

try:
    import rasterio  # type: ignore
except ImportError:
    rasterio = None  # Optional: install rasterio for --use-worldcover / --augment-water-gsw
    print("Warning: rasterio not installed. WorldCover and GSW features are disabled.", file=sys.stderr)

# Maximum supported map size (cells per side). 16384x16384 = ~800MB tile data.
MAX_CELLS = 16384

_t0 = time.monotonic()


def _status(msg: str) -> None:
    """Print a timestamped progress message to stderr."""
    elapsed = time.monotonic() - _t0
    print(f"  [{elapsed:6.1f}s] {msg}", file=sys.stderr, flush=True)


def parse_args() -> argparse.Namespace:
    p = argparse.ArgumentParser(description="Compute AOI bounds from MGRS for OpenRA RA maps")
    p.add_argument("--mgrs", required=True, help="MGRS center coordinate (e.g., 33TWN1234567890)")
    p.add_argument("--cells", type=int, default=2048, help=f"Map size in cells per side (default: 2048, max: {MAX_CELLS})")
    p.add_argument("--meters-per-cell", type=float, default=4.0, help="Meters per cell (default: 4.0)")
    p.add_argument("--rotation-deg", type=float, default=0.0, help="Rotation relative to UTM north, degrees (default: 0)")
    # In RA map.yaml, the tileset id is "TEMPERAT" (see OpenRA/mods/ra/tilesets/temperat.yaml General: Id)
    p.add_argument("--tileset", default="TEMPERAT", help="Tileset identifier for map.yaml (default: TEMPERAT)")
    p.add_argument("--pretty", action="store_true", help="Pretty-print JSON output")
    p.add_argument("--osm-summary", action="store_true", help="Fetch OSM highway/water counts within AOI bbox")
    p.add_argument("--overpass-url", default="https://overpass-api.de/api/interpreter", help="Overpass API endpoint")
    # Map file generation
    p.add_argument("--write-oramap", metavar="PATH", help="Write a .oramap zip to PATH (e.g., output/realworld.oramap)")
    p.add_argument("--install-openra", action="store_true", help="Copy the .oramap into the OpenRA RA maps directory after writing")
    p.add_argument("--install-release", default=None, help="OpenRA release tag (e.g., 20250330). If set, install to maps/ra/release-<tag>")
    p.add_argument("--install-path", default=None, help="Explicit directory to copy the .oramap into (overrides automatic path)")
    p.add_argument("--title", default=None, help="Title to embed in map.yaml (default: RealWorld <MGRS>)")
    p.add_argument("--author", default="BK_MainMan", help="Author for map.yaml")
    p.add_argument("--categories", default="RealWorld", help="Comma-separated Categories for map.yaml")
    p.add_argument("--players", type=int, default=8, help="Number of playable Multi players to add (0-8, default: 8)")
    p.add_argument("--place-spawns", action="store_true", help="Place mpspawn actors for each player")
    # OSM overlay options
    p.add_argument("--overlay-osm", action="store_true", help="Overlay OSM features (roads/water/vegetation) into tiles and actors")
    p.add_argument("--road-width-m", type=float, default=8.0, help="Road drawing width in meters for highways (default: 8.0)")
    p.add_argument("--waterway-width-m", type=float, default=6.0, help="Waterway drawing width in meters (default: 6.0)")
    p.add_argument("--veg-density", type=float, default=0.15, help="Fraction [0..1] of forest cells to place a tree actor (default: 0.15)")
    p.add_argument("--max-veg-actors", type=int, default=4000, help="Maximum number of tree actors to place (default: 4000)")
    p.add_argument("--no-roads", action="store_true", help="Disable road overlay")
    p.add_argument("--no-water", action="store_true", help="Disable water overlay")
    p.add_argument("--no-vegetation", action="store_true", help="Disable vegetation overlay")
    p.add_argument("--veg-min-spacing", type=int, default=2,
                   help="Minimum Chebyshev spacing (tiles) between tree actors (default: 2)")
    p.add_argument("--veg-patch-size", type=int, default=32,
                   help="Patch size (cells) for local forest density tuning (default: 32)")
    p.add_argument("--veg-patch-boost", type=float, default=1.5,
                   help="Multiplier for tree probability in high-density patches (default: 1.5)")
    p.add_argument("--suppress-veg-near-roads", type=int, default=1,
                   help="Suppress tree actors within N tiles of roads (default: 1; 0 disables)")
    p.add_argument("--suppress-veg-near-buildings", type=int, default=1,
                   help="Suppress tree actors within N tiles of placed buildings (default: 1; 0 disables)")
    # OSM buildings overlay options (M4)
    p.add_argument("--overlay-osm-buildings", action="store_true", help="Overlay OSM building footprints as actors")
    p.add_argument("--building-density", type=float, default=1.0, help="Fraction [0..1] of OSM buildings to place (default: 1.0)")
    p.add_argument("--max-buildings", type=int, default=1200, help="Maximum number of building actors to place (default: 1200)")
    p.add_argument("--no-buildings", action="store_true", help="Disable building overlay")
    p.add_argument("--no-coastline", action="store_true",
                   help="Disable coastline inversion (default-to-water for coastal areas)")
    p.add_argument("--building-search-radius", type=int, default=2,
                   help="Local search radius (tiles) around anchor for placement (default: 2)")
    p.add_argument("--building-placement-mode", choices=["accurate", "fallback", "aggressive"], default="accurate",
                   help="Placement strategy: accurate (no fallback), fallback (allow size downgrade), aggressive (fallback + larger search radius)")
    p.add_argument("--debug-building-audit", default=None,
                   help="Path to CSV file to write per-building placement audit (optional)")
    # Data source toggles and caching
    p.add_argument("--use-worldcover", action="store_true", help="Use ESA WorldCover landcover for vegetation/built-up mask")
    p.add_argument("--worldcover-path", default=None, help="Path to ESA WorldCover GeoTIFF (10 m) for AOI masking")
    p.add_argument("--augment-water-gsw", action="store_true", help="Augment water using JRC Global Surface Water")
    p.add_argument("--gsw-path", default=None, help="Path to JRC Global Surface Water raster (mask/occurrence)")
    p.add_argument("--gsw-min-occurrence", type=float, default=75.0,
                   help="Minimum occurrence percentage [0-100] considered water when using GSW occurrence rasters. "
                        "If the raster is binary (0/1), any value > 0 is treated as water. Default: 75.0")
    # Dataset metadata
    p.add_argument("--worldcover-year", default=None, help="WorldCover dataset year for metadata (e.g., 2020 or 2021)")
    p.add_argument("--gsw-version", default=None, help="JRC GSW version/year string for metadata (e.g., 2023)")
    p.add_argument("--osm-cache-dir", default=".cache/osm", help="Directory for OSM Overpass cache (default: .cache/osm)")
    p.add_argument("--no-osm-cache", action="store_true", help="Disable OSM Overpass caching")
    # Validation (M3)
    p.add_argument("--validate-geotransform", action="store_true",
                   help="Print GeoTransform validation samples (cell <-> lat/lon round-trip)")
    p.add_argument("--validate-cell", default=None,
                   help="Validate a specific cell 'i,j' (0-based), e.g. --validate-cell 0,0")
    p.add_argument("--validate-latlon", default=None,
                   help="Validate a specific lat,lon, e.g. --validate-latlon 34.59,-77.37")
    p.add_argument("--overpass-timeout", type=int, default=60,
                   help="Overpass API query timeout in seconds (default: 60)")
    args = p.parse_args()
    if args.cells < 1 or args.cells > MAX_CELLS:
        p.error(f"--cells must be between 1 and {MAX_CELLS} (got {args.cells})")
    return args


def mgrs_to_center(mgrs_str: str) -> Dict[str, Any]:
    m = mgrs.MGRS()
    lat, lon = m.toLatLon(mgrs_str)
    e, n, zone_number, zone_letter = utm.from_latlon(lat, lon)
    return {
        "lat": lat,
        "lon": lon,
        "utm": {
            "easting": e,
            "northing": n,
            "zone_number": zone_number,
            "zone_letter": zone_letter,
        },
    }


def compute_bounds(center: Dict[str, Any], cells: int, mpc: float) -> Dict[str, Any]:
    e = center["utm"]["easting"]
    n = center["utm"]["northing"]
    zone_number = center["utm"]["zone_number"]
    zone_letter = center["utm"]["zone_letter"]

    total_m = cells * mpc
    half_m = total_m / 2.0

    min_e = e - half_m
    max_e = e + half_m
    min_n = n - half_m
    max_n = n + half_m

    # Corners in lat/lon
    nw_lat, nw_lon = utm.to_latlon(min_e, max_n, zone_number, zone_letter)
    ne_lat, ne_lon = utm.to_latlon(max_e, max_n, zone_number, zone_letter)
    se_lat, se_lon = utm.to_latlon(max_e, min_n, zone_number, zone_letter)
    sw_lat, sw_lon = utm.to_latlon(min_e, min_n, zone_number, zone_letter)

    return {
        "extent_m": {"width": total_m, "height": total_m},
        "bbox_utm": {"min_e": min_e, "max_e": max_e, "min_n": min_n, "max_n": max_n},
        "corners": {
            "NW": {"lat": nw_lat, "lon": nw_lon},
            "NE": {"lat": ne_lat, "lon": ne_lon},
            "SE": {"lat": se_lat, "lon": se_lon},
            "SW": {"lat": sw_lat, "lon": sw_lon},
        },
    }


# --- OpenRA RA .oramap generation ---

def _le_u16(v: int) -> bytes:
    return v.to_bytes(2, "little", signed=False)


def _le_u32(v: int) -> bytes:
    return v.to_bytes(4, "little", signed=False)


def build_map_bin(width: int, height: int, *, default_template_id: int = 255, default_variant: int = 0,
                  include_heights: bool = False) -> bytes:
    """
    Construct map.bin following OpenRA Map.cs SaveBinaryData() with TileFormat=2.

    Layout:
      - byte: TileFormat (2)
      - ushort: width, ushort: height
      - uint: tilesOffset (17), uint: heightsOffset, uint: resourcesOffset
      - tiles: for i in 0..w-1, for j in 0..h-1 -> ushort(type), byte(index)  [3*w*h bytes]
      - heights (optional): byte per cell [w*h bytes]
      - resources: for i.., j.. -> byte(type), byte(density) [2*w*h bytes]

    For RA (temperate), MaximumTerrainHeight == 0, so heightsOffset=0 and resourcesOffset=3*w*h+17.
    """
    w, h = width, height
    tile_format = 2  # Map.TileFormat
    header_len = 17
    tiles_offset = header_len
    heights_offset = 0 if not include_heights else (3 * w * h + header_len)
    resources_offset = (4 if include_heights else 3) * w * h + header_len

    out = bytearray()
    out.append(tile_format)
    out += _le_u16(w)
    out += _le_u16(h)
    out += _le_u32(tiles_offset)
    out += _le_u32(heights_offset)
    out += _le_u32(resources_offset)

    # Tiles: column-major (i outer, j inner)
    for i in range(w):
        for j in range(h):
            out += _le_u16(default_template_id)
            out.append(default_variant & 0xFF)

    # Heights (skipped for RA)
    if include_heights:
        out += bytes([0]) * (w * h)

    # Resources: zero type/density everywhere
    out += bytes([0, 0]) * (w * h)

    return bytes(out)


def build_map_bin_from_grid(tiles_type: List[List[int]], tiles_variant: List[List[int]], *, include_heights: bool = False) -> bytes:
    """
    Build map.bin from explicit per-cell tile type and variant grids.

    tiles_type and tiles_variant must be sized [width][height] in column-major indexing.
    """
    if not tiles_type or not tiles_type[0]:
        return b""
    w = len(tiles_type)
    h = len(tiles_type[0])
    # Validate shapes
    for col in tiles_type:
        if len(col) != h:
            raise ValueError("tiles_type columns have inconsistent heights")
    for col in tiles_variant:
        if len(col) != h:
            raise ValueError("tiles_variant columns have inconsistent heights")

    tile_format = 2
    header_len = 17
    tiles_offset = header_len
    heights_offset = 0 if not include_heights else (3 * w * h + header_len)
    resources_offset = (4 if include_heights else 3) * w * h + header_len

    out = bytearray()
    out.append(tile_format)
    out += _le_u16(w)
    out += _le_u16(h)
    out += _le_u32(tiles_offset)
    out += _le_u32(heights_offset)
    out += _le_u32(resources_offset)

    for i in range(w):
        for j in range(h):
            out += _le_u16(int(tiles_type[i][j]))
            out.append(int(tiles_variant[i][j]) & 0xFF)

    if include_heights:
        out += bytes([0]) * (w * h)

    out += bytes([0, 0]) * (w * h)
    return bytes(out)


def build_players_block(num_players: int) -> str:
    num_players = max(0, min(8, int(num_players)))
    lines = []
    lines.append("Players:")
    lines.append("\tPlayerReference@Neutral:")
    lines.append("\t\tName: Neutral")
    lines.append("\t\tOwnsWorld: True")
    lines.append("\t\tNonCombatant: True")
    lines.append("\t\tFaction: allies")
    for p in range(num_players):
        lines.append(f"\tPlayerReference@Multi{p}:")
        lines.append(f"\t\tName: Multi{p}")
        lines.append("\t\tPlayable: True")
        lines.append("\t\tAllowBots: True")
        lines.append("\t\tLockFaction: False")
        lines.append("\t\tFaction: soviet")
    return "\n".join(lines)


def build_spawn_actors(num_players: int, width: int, height: int) -> str:
    if num_players <= 0:
        return ""
    # Place spawn points in a ring at ~70% of the half-width from center
    cx, cy = width // 2, height // 2
    r = max(8, int(min(cx, cy) * 0.7))
    coords = []
    for k in range(num_players):
        ang = 2 * math.pi * (k / max(1, num_players))
        x = int(cx + r * math.cos(ang))
        y = int(cy + r * math.sin(ang))
        coords.append((x, y))
    lines = []
    for idx, (x, y) in enumerate(coords):
        lines.append(f"\tSpawn{idx}: mpspawn")
        lines.append(f"\t\tLocation: {x},{y}")
        lines.append("\t\tOwner: Neutral")
    return "\n".join(lines)


def build_metadata_block(metadata: Dict[str, Any]) -> str:
    lines: List[str] = []
    lines.append("Metadata:")
    gt = metadata.get("GeoTransform", {})
    if gt:
        lines.append("\tGeoTransform:")
        uzn = gt.get("utm_zone_number")
        uzl = gt.get("utm_zone_letter")
        if uzn is not None and uzl is not None:
            lines.append(f"\t\tUTMZone: {uzn}{uzl}")
        if "meters_per_cell" in gt:
            lines.append(f"\t\tMetersPerCell: {gt['meters_per_cell']}")
        if "rotation_deg" in gt:
            lines.append(f"\t\tRotationDeg: {gt['rotation_deg']}")
        origin = gt.get("origin", {})
        if origin:
            lines.append("\t\tOrigin:")
            if "corner" in origin:
                lines.append(f"\t\t\tCorner: {origin['corner']}")
            if "lat" in origin:
                lines.append(f"\t\t\tLat: {origin['lat']}")
            if "lon" in origin:
                lines.append(f"\t\t\tLon: {origin['lon']}")
            if "utm_e" in origin:
                lines.append(f"\t\t\tUTM_E: {origin['utm_e']}")
            if "utm_n" in origin:
                lines.append(f"\t\t\tUTM_N: {origin['utm_n']}")
        grid = gt.get("grid", {})
        if grid:
            lines.append("\t\tGrid:")
            if "width" in grid:
                lines.append(f"\t\t\tWidth: {grid['width']}")
            if "height" in grid:
                lines.append(f"\t\t\tHeight: {grid['height']}")
    atts: List[Dict[str, Any]] = metadata.get("Attributions", [])  # type: ignore
    if atts:
        lines.append("\tAttributions:")
        for a in atts:
            lines.append("\t\t- Name: " + str(a.get("Name", "")))
            if a.get("License") is not None:
                lines.append("\t\t  License: " + str(a.get("License")))
            if a.get("URL") is not None:
                lines.append("\t\t  URL: " + str(a.get("URL")))
            if a.get("Source") is not None:
                lines.append("\t\t  Source: " + str(a.get("Source")))
            if a.get("Notes") is not None:
                lines.append("\t\t  Notes: " + str(a.get("Notes")))
            # Write any additional keys that may be present, such as DatasetYear/DatasetVersion
            known = {"Name", "License", "URL", "Source", "Notes"}
            for k in sorted(a.keys()):
                if k in known:
                    continue
                v = a.get(k)
                if v is None:
                    continue
                lines.append(f"\t\t  {k}: {v}")
    return "\n".join(lines)


def build_map_yaml(title: str, author: str, tileset: str, width: int, height: int,
                   categories: str, num_players: int, place_spawns: bool,
                   extra_actors: Optional[List[str]] = None,
                   metadata: Optional[Dict[str, Any]] = None) -> str:
    cats = ", ".join([c.strip() for c in categories.split(",") if c.strip()]) or "RealWorld"
    yaml_parts = []
    yaml_parts.append("MapFormat: 12")
    yaml_parts.append("")
    yaml_parts.append("RequiresMod: ra")
    yaml_parts.append("")
    yaml_parts.append(f"Title: {title}")
    yaml_parts.append("")
    yaml_parts.append(f"Author: {author}")
    yaml_parts.append("")
    yaml_parts.append(f"Tileset: {tileset}")
    yaml_parts.append("")
    yaml_parts.append(f"MapSize: {width},{height}")
    yaml_parts.append("")
    yaml_parts.append(f"Bounds: 0,0,{width},{height}")
    yaml_parts.append("")
    yaml_parts.append("Visibility: Lobby")
    yaml_parts.append("")
    yaml_parts.append(f"Categories: {cats}")
    yaml_parts.append("")
    if metadata:
        yaml_parts.append(build_metadata_block(metadata))
        yaml_parts.append("")
    yaml_parts.append(build_players_block(num_players))
    yaml_parts.append("")
    yaml_parts.append("Actors:")
    if place_spawns:
        spawns = build_spawn_actors(num_players, width, height)
        if spawns:
            yaml_parts.append(spawns)
    # Append extra actors like trees
    if extra_actors:
        for a in extra_actors:
            yaml_parts.append(a)
    yaml_parts.append("")
    return "\n".join(yaml_parts)


# --- OSM overlay to tiles / actors ---

CLEAR_TEMPLATE_ID = 255  # `Templates: Template@255` in temperat.yaml
WATER_TEMPLATE_ID = 1    # `Template@1` (w1.tem) 1x1 Water
ROAD_TEMPLATE_ID = 227   # `Template@227` (d44.tem) 1x1 Road
BEACH_TEMPLATE_ID = 6    # `Template@6` (sh04.tem) 3x3 Beach/Water mix; we will use a Beach variant

# River templates (multi-tile) used for MVP smoothing.
# See `OpenRA/mods/ra/tilesets/temperat.yaml` for definitions.
RIVER_TEMPLATE_VERT_CENTER = 117   # rv06.tem, Size 3x2, center column is River (indices 1 and 4)
RIVER_TEMPLATE_HORIZ_TOP = 121     # rv10.tem, Size 2x2, top row River (indices 0 and 1), plus 3 River
RIVER_TEMPLATE_HORIZ_TOP_ALT = 122 # rv11.tem, Size 2x2, top row River (indices 0 and 1), plus 2 River

# Map template ids to their (width, height) sizes for stamping
TEMPLATE_SIZES: Dict[int, Tuple[int, int]] = {
    RIVER_TEMPLATE_VERT_CENTER: (3, 2),
    RIVER_TEMPLATE_HORIZ_TOP: (2, 2),
    RIVER_TEMPLATE_HORIZ_TOP_ALT: (2, 2),
    # Road intersections (3x3) used to improve junction visuals
    206: (3, 3),  # d34.tem
    207: (3, 3),  # d35.tem
}

# All River template IDs in TEMPERATE tileset (used to detect water-like neighbors)
RIVER_TEMPLATE_IDS: Set[int] = {
    112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124,
    229, 230,
}

# Skip stamping rivers in areas that are predominantly open water (e.g., bays/ocean).
# Evaluate on a local window around the sample point.
RIVER_STAMP_SKIP_WATER_FRAC = 0.45  # if local water fraction exceeds this, skip stamp

# Road helpers
# Some additional 1x1 base road tile exists (Template@228). Treat both as roadlike for adjacency tests.
ROAD_BASE_TEMPLATE_IDS: Set[int] = {227, 228}
ROAD_MULTI_TEMPLATE_IDS: Set[int] = {206, 207}
ROAD_ALL_TEMPLATE_IDS: Set[int] = set().union(ROAD_BASE_TEMPLATE_IDS, ROAD_MULTI_TEMPLATE_IDS)


def _latlon_to_cell(lat: float, lon: float, *, center: Dict[str, Any], bounds: Dict[str, Any], mpc: float) -> Optional[Tuple[float, float]]:
    """
    Convert lat/lon to fractional cell coordinates (x, y) where x in [0,w), y in [0,h).
    Origin is at (min_e, min_n). Returns None if UTM zone mismatch.
    """
    e, n, zn, zl = utm.from_latlon(lat, lon)
    czn = center["utm"]["zone_number"]
    czl = center["utm"]["zone_letter"]
    # Require same UTM zone for simplicity
    if zn != czn or zl != czl:
        return None
    min_e = bounds["bbox_utm"]["min_e"]
    max_n = bounds["bbox_utm"]["max_n"]
    x = (e - min_e) / mpc
    # Y increases downward in map grid; set y=0 at north edge
    y = (max_n - n) / mpc
    return (x, y)


def _cell_to_latlon(i: int, j: int, *, center: Dict[str, Any], bounds: Dict[str, Any], mpc: float) -> Tuple[float, float]:
    """Convert grid cell index (column-major) to lat/lon of the cell center."""
    min_e = bounds["bbox_utm"]["min_e"]
    max_n = bounds["bbox_utm"]["max_n"]
    e = float(min_e + (i + 0.5) * mpc)
    n = float(max_n - (j + 0.5) * mpc)
    zn = center["utm"]["zone_number"]
    zl = center["utm"]["zone_letter"]
    lat, lon = utm.to_latlon(e, n, zn, zl)
    return (lat, lon)


def _draw_disc(grid: List[List[int]], cx: float, cy: float, radius_cells: float, value: int) -> int:
    """Draw a filled disc of given radius (in cells) on column-major grid. Returns number of cells set."""
    w = len(grid)
    h = len(grid[0]) if w else 0
    r = max(0.0, radius_cells)
    r2 = r * r
    xmin = max(0, int(cx - r) - 1)
    xmax = min(w - 1, int(cx + r) + 1)
    ymin = max(0, int(cy - r) - 1)
    ymax = min(h - 1, int(cy + r) + 1)
    set_count = 0
    for i in range(xmin, xmax + 1):
        for j in range(ymin, ymax + 1):
            dx = (i + 0.5) - cx
            dy = (j + 0.5) - cy
            if dx * dx + dy * dy <= r2:
                if 0 <= i < w and 0 <= j < h:
                    if grid[i][j] != value:
                        grid[i][j] = value
                        set_count += 1
    return set_count


def _rasterize_line(grid: List[List[int]], x0: float, y0: float, x1: float, y1: float, radius_cells: float, value: int) -> int:
    """Rasterize a thick line by sampling along the segment and drawing discs. Returns cells touched (approx)."""
    length = math.hypot(x1 - x0, y1 - y0)
    if length == 0:
        return _draw_disc(grid, x0, y0, radius_cells, value)
    # Sample roughly every 0.5 cell
    steps = max(1, int(length * 2))
    count = 0
    for s in range(steps + 1):
        t = s / steps
        x = x0 + (x1 - x0) * t
        y = y0 + (y1 - y0) * t
        count += _draw_disc(grid, x, y, radius_cells, value)
    return count


def _point_in_poly(px: float, py: float, poly: List[Tuple[float, float]]) -> bool:
    """Even-odd rule point-in-polygon test on a list of (x,y) vertices."""
    inside = False
    n = len(poly)
    if n < 3:
        return False
    xj, yj = poly[-1]
    for i in range(n):
        xi, yi = poly[i]
        # Standard ray-casting: skip horizontal edges to avoid division by zero
        if (yi > py) != (yj > py):
            slope_x = (xj - xi) * (py - yi) / (yj - yi) + xi
            if px < slope_x:
                inside = not inside
        xj, yj = xi, yi
    return inside


def _fill_polygon(grid: List[List[int]], poly: List[Tuple[float, float]], value: int) -> int:
    """Fill a polygon on the grid using scanline algorithm. Returns number of cells set."""
    if not poly or len(poly) < 3:
        return 0
    w = len(grid)
    h = len(grid[0]) if w else 0
    if w == 0 or h == 0:
        return 0
    ys = [p[1] for p in poly]
    ymin = max(0, int(min(ys)))
    ymax = min(h - 1, int(max(ys)) + 1)
    n = len(poly)
    set_count = 0
    for j in range(ymin, ymax + 1):
        cy = j + 0.5
        # Find all x-intersections of polygon edges with this scanline (even-odd rule)
        node_x: List[float] = []
        k = n - 1
        for idx in range(n):
            yi = poly[idx][1]
            yk = poly[k][1]
            if (yi > cy) != (yk > cy):
                ix = poly[idx][0] + (cy - yi) / (yk - yi) * (poly[k][0] - poly[idx][0])
                node_x.append(ix)
            k = idx
        node_x.sort()
        # Fill between pairs of intersections
        for p in range(0, len(node_x) - 1, 2):
            i_start = max(0, math.ceil(node_x[p] - 0.5))
            i_end = min(w - 1, int(math.floor(node_x[p + 1] - 0.5)))
            for i in range(i_start, i_end + 1):
                if grid[i][j] != value:
                    grid[i][j] = value
                    set_count += 1
    return set_count


# Stamp a multi-tile Template ID at (i0, j0) top-left. Returns number of cells written.
def _stamp_template(tiles_type: List[List[int]], tiles_variant: List[List[int]],
                    i0: int, j0: int, template_id: int) -> int:
    size = TEMPLATE_SIZES.get(template_id)
    if not size:
        return 0
    tw, th = size
    w = len(tiles_type)
    h = len(tiles_type[0]) if w else 0
    written = 0
    for dy in range(th):
        for dx in range(tw):
            i = i0 + dx
            j = j0 + dy
            if 0 <= i < w and 0 <= j < h:
                tiles_type[i][j] = template_id
                tiles_variant[i][j] = dy * tw + dx
                written += 1
    return written


def _local_water_fraction(tiles_type: List[List[int]], ci: int, cj: int, radius: int = 4) -> float:
    """Compute fraction of WATER_TEMPLATE_ID cells in an (2r+1)x(2r+1) window centered at (ci,cj)."""
    w = len(tiles_type)
    h = len(tiles_type[0]) if w else 0
    if w == 0 or h == 0:
        return 0.0
    total = 0
    water = 0
    for dj in range(-radius, radius + 1):
        j = cj + dj
        if j < 0 or j >= h:
            continue
        for di in range(-radius, radius + 1):
            i = ci + di
            if i < 0 or i >= w:
                continue
            total += 1
            if tiles_type[i][j] == WATER_TEMPLATE_ID:
                water += 1
    if total == 0:
        return 0.0
    return float(water) / float(total)


def _assemble_way_nodes(way: Dict[str, Any], nodes_by_id: Dict[int, Tuple[float, float]], *, center: Dict[str, Any], bounds: Dict[str, Any], mpc: float) -> List[Tuple[float, float]]:
    coords: List[Tuple[float, float]] = []
    for nid in way.get("nodes", []):
        npos = nodes_by_id.get(int(nid))
        if not npos:
            continue
        cell = _latlon_to_cell(npos[0], npos[1], center=center, bounds=bounds, mpc=mpc)
        if cell is not None:
            coords.append(cell)
    return coords


def _simplify_polygon(poly: List[Tuple[float, float]], epsilon: float = 1.0) -> List[Tuple[float, float]]:
    """Douglas-Peucker line simplification. Reduces vertex count while preserving shape.
    epsilon is in cell units — vertices within epsilon of the simplified line are dropped.
    """
    if len(poly) <= 2:
        return list(poly)

    # Find the point with the greatest distance from the line (first, last)
    first = poly[0]
    last = poly[-1]
    max_dist = 0.0
    max_idx = 0
    dx = last[0] - first[0]
    dy = last[1] - first[1]
    line_len_sq = dx * dx + dy * dy

    for i in range(1, len(poly) - 1):
        px, py = poly[i]
        if line_len_sq == 0:
            dist = math.hypot(px - first[0], py - first[1])
        else:
            t = max(0.0, min(1.0, ((px - first[0]) * dx + (py - first[1]) * dy) / line_len_sq))
            proj_x = first[0] + t * dx
            proj_y = first[1] + t * dy
            dist = math.hypot(px - proj_x, py - proj_y)
        if dist > max_dist:
            max_dist = dist
            max_idx = i

    if max_dist > epsilon:
        left = _simplify_polygon(poly[:max_idx + 1], epsilon)
        right = _simplify_polygon(poly[max_idx:], epsilon)
        return left[:-1] + right
    else:
        return [first, last]


def _close_chain_via_bbox(chain: List[Tuple[float, float]], width: int, height: int) -> List[Tuple[float, float]]:
    """Close an open coastline chain by walking along the bounding box edges.

    Tries both clockwise and counterclockwise closure around the bbox and
    picks the one that produces the smaller polygon (= the land polygon,
    not the ocean polygon).
    """
    if not chain or len(chain) < 2:
        return chain
    if chain[0] == chain[-1]:
        return chain  # Already closed

    # Bbox corners clockwise: TL, TR, BR, BL
    corners = [
        (0.0, 0.0),                    # TL
        (float(width), 0.0),           # TR
        (float(width), float(height)), # BR
        (0.0, float(height)),          # BL
    ]

    def _edge_position(pt: Tuple[float, float]) -> float:
        """Map a point on the bbox perimeter to a scalar 0..4 going clockwise from TL."""
        x, y = pt[0], pt[1]
        w, h = float(width), float(height)
        x = max(0.0, min(w, x))
        y = max(0.0, min(h, y))
        if y <= 1.0:
            return x / w
        if x >= w - 1.0:
            return 1.0 + y / h
        if y >= h - 1.0:
            return 2.0 + (w - x) / w
        return 3.0 + (h - y) / h

    pos_start = _edge_position(chain[-1])
    pos_end = _edge_position(chain[0])
    corner_positions = [_edge_position(c) for c in corners]

    def _collect_corners_cw(p_from: float, p_to: float) -> List[Tuple[float, float]]:
        """Collect bbox corners going clockwise from p_from to p_to."""
        path: List[Tuple[float, float]] = []
        for i, cp in enumerate(corner_positions):
            if p_from <= p_to:
                if p_from < cp < p_to:
                    path.append(corners[i])
            else:
                if cp > p_from or cp < p_to:
                    path.append(corners[i])
        path.sort(key=lambda p: (_edge_position(p) - p_from) % 4.0)
        return path

    def _poly_abs_area(poly: List[Tuple[float, float]]) -> float:
        a = 0.0
        for i in range(len(poly) - 1):
            a += (poly[i + 1][0] - poly[i][0]) * (poly[i + 1][1] + poly[i][1])
        return abs(a)

    # Clockwise closure: walk CW from chain end to chain start
    cw_corners = _collect_corners_cw(pos_start, pos_end)
    poly_cw = list(chain) + cw_corners + [chain[0]]

    # Counterclockwise closure: walk CW from chain start to chain end (= CCW from end to start)
    ccw_corners = _collect_corners_cw(pos_end, pos_start)
    poly_ccw = list(chain) + list(reversed(ccw_corners)) + [chain[0]]

    # Pick the smaller polygon — the land polygon, not the ocean
    return poly_cw if _poly_abs_area(poly_cw) <= _poly_abs_area(poly_ccw) else poly_ccw


def _assemble_coastline_rings(
    osm_data: Dict[str, Any],
    nodes_by_id: Dict[int, Tuple[float, float]],
    ways_by_id: Dict[int, Dict[str, Any]],
    *,
    center: Dict[str, Any],
    bounds: Dict[str, Any],
    mpc: float,
    cells: int,
    stats: Dict[str, int],
) -> List[List[Tuple[float, float]]]:
    """Collect natural=coastline ways, chain them into closed land polygon rings.

    OSM coastlines have land on the left, water on the right. We assemble
    connected chains, close open ones via bbox edge walking, convert to cell
    coordinates, and simplify.

    Returns list of closed land polygon rings in cell coordinates.
    """
    # Collect coastline ways
    coastline_ways: List[Dict[str, Any]] = []
    for el in osm_data.get("elements", []):
        if el.get("type") != "way":
            continue
        tags = el.get("tags", {}) or {}
        if tags.get("natural") == "coastline":
            coastline_ways.append(el)

    stats["coast_coastline_ways"] = len(coastline_ways)
    if not coastline_ways:
        return []

    # Build chains: each way is a sequence of node IDs
    # Chain by matching last node of one way to first node of another
    chains: List[List[int]] = []
    for way in coastline_ways:
        node_ids = [int(nid) for nid in way.get("nodes", [])]
        if len(node_ids) >= 2:
            chains.append(node_ids)

    # Iteratively merge chains that share endpoints
    merged = True
    while merged:
        merged = False
        # Build index: first_node -> chain index, last_node -> chain index
        first_idx: Dict[int, int] = {}
        last_idx: Dict[int, int] = {}
        for ci, ch in enumerate(chains):
            if ch:
                first_idx[ch[0]] = ci
                last_idx[ch[-1]] = ci
        for ci, ch in enumerate(chains):
            if not ch:
                continue
            last_node = ch[-1]
            # Can we append another chain whose first node matches our last?
            if last_node in first_idx:
                oi = first_idx[last_node]
                if oi != ci and chains[oi]:
                    # Merge: extend ci with oi (skip duplicate junction node)
                    chains[ci] = ch + chains[oi][1:]
                    chains[oi] = []
                    merged = True
                    break

    # Remove empty chains
    chains = [ch for ch in chains if ch]

    total_verts_before = 0
    total_verts_after = 0
    open_chains = 0
    rings: List[List[Tuple[float, float]]] = []

    for ch in chains:
        # Convert node IDs to cell coordinates
        cell_coords: List[Tuple[float, float]] = []
        for nid in ch:
            npos = nodes_by_id.get(nid)
            if not npos:
                continue
            cell = _latlon_to_cell(npos[0], npos[1], center=center, bounds=bounds, mpc=mpc)
            if cell is not None:
                cell_coords.append(cell)

        if len(cell_coords) < 3:
            continue

        total_verts_before += len(cell_coords)

        # Check if chain is closed
        is_closed = (ch[0] == ch[-1])
        if not is_closed:
            open_chains += 1
            # Close via bbox edge walk
            cell_coords = _close_chain_via_bbox(cell_coords, cells, cells)

        # Ensure closure
        if cell_coords[0] != cell_coords[-1]:
            cell_coords.append(cell_coords[0])

        # Check orientation: OSM coastlines have land on left.
        # For our cell coordinate system (x right, y down), land polygons
        # should have negative signed area (clockwise winding).
        # If positive (CCW), reverse to get land polygon.
        signed_area = 0.0
        for i in range(len(cell_coords) - 1):
            x0, y0 = cell_coords[i]
            x1, y1 = cell_coords[i + 1]
            signed_area += (x1 - x0) * (y1 + y0)
        # signed_area > 0 means clockwise in screen coords (y-down) = land on left = correct
        # signed_area < 0 means CCW = water polygon, reverse it
        if signed_area < 0:
            cell_coords.reverse()

        # Simplify to reduce vertex count (epsilon = 1.0 cell)
        simplified = _simplify_polygon(cell_coords, epsilon=1.0)
        total_verts_after += len(simplified)

        if len(simplified) >= 3:
            rings.append(simplified)

    stats["coast_assembled_rings"] = len(rings)
    stats["coast_open_chains"] = open_chains
    stats["coast_vertices_before_simplify"] = total_verts_before
    stats["coast_vertices_after_simplify"] = total_verts_after

    return rings


def _process_water_relations(
    osm_data: Dict[str, Any],
    nodes_by_id: Dict[int, Tuple[float, float]],
    ways_by_id: Dict[int, Dict[str, Any]],
    tiles_type: List[List[int]],
    *,
    center: Dict[str, Any],
    bounds: Dict[str, Any],
    mpc: float,
    stats: Dict[str, int],
) -> int:
    """Process OSM relation elements with natural=water as multipolygons.

    Fills outer rings with WATER, inner rings (islands) with CLEAR.
    Returns total number of cells modified.
    """
    total_cells = 0
    for el in osm_data.get("elements", []):
        if el.get("type") != "relation":
            continue
        tags = el.get("tags", {}) or {}
        if tags.get("natural") != "water":
            continue
        members = el.get("members", []) or []
        for m in members:
            if m.get("type") != "way":
                continue
            role = m.get("role", "outer")
            wid = m.get("ref")
            if wid is None:
                continue
            w_el = ways_by_id.get(int(wid))
            if not w_el:
                continue
            coords = _assemble_way_nodes(w_el, nodes_by_id, center=center, bounds=bounds, mpc=mpc)
            if len(coords) < 3:
                continue
            # Close ring if needed
            if coords[0] != coords[-1]:
                coords.append(coords[0])
            if role == "inner":
                # Inner ring = island within water = CLEAR
                total_cells += _fill_polygon(tiles_type, coords, CLEAR_TEMPLATE_ID)
            else:
                # Outer ring = water body
                total_cells += _fill_polygon(tiles_type, coords, WATER_TEMPLATE_ID)
    stats["water_relation_cells"] = total_cells
    return total_cells


def overlay_osm_to_tiles(center: Dict[str, Any], bounds: Dict[str, Any], mpc: float, cells: int, osm_data: Dict[str, Any],
                         *, include_roads: bool, include_water: bool, include_vegetation: bool, include_buildings: bool,
                         include_coastline: bool = True,
                         road_width_m: float, waterway_width_m: float, veg_density: float, max_veg_actors: int,
                         veg_min_spacing: int, veg_patch_size: int, veg_patch_boost: float,
                         suppress_veg_near_roads: int, suppress_veg_near_buildings: int,
                         building_density: float, max_buildings: int, building_search_radius: int,
                         building_placement_mode: str, building_audit_path: Optional[str] = None,
                         worldcover_masks: Optional[Dict[str, Set[Tuple[int, int]]]] = None,
                         gsw_water_mask: Optional[Set[Tuple[int, int]]] = None) -> Tuple[List[List[int]], List[List[int]], List[str], Dict[str, int]]:
    """
    Convert OSM features into tile grids and actor YAML lines.

    Returns (tiles_type, tiles_variant, extra_actor_lines, stats)
    """
    width = height = int(cells)
    tiles_variant = [[0 for _ in range(height)] for _ in range(width)]

    # Collect nodes
    nodes_by_id: Dict[int, Tuple[float, float]] = {}
    for el in osm_data.get("elements", []):
        if el.get("type") == "node" and "lat" in el and "lon" in el:
            nodes_by_id[int(el["id"])] = (float(el["lat"]), float(el["lon"]))

    # Collect ways for relation assembly / lookup
    ways_by_id: Dict[int, Dict[str, Any]] = {}
    for el in osm_data.get("elements", []):
        if el.get("type") == "way" and "nodes" in el:
            ways_by_id[int(el["id"])] = el

    _status("Indexing OSM nodes and ways...")
    # --- Coastline inversion: detect coastlines and optionally default grid to WATER ---
    coastline_land_rings: List[List[Tuple[float, float]]] = []
    coastline_land_cells = 0
    if include_coastline and include_water:
        _status("Assembling coastline rings...")
        coastline_stats: Dict[str, int] = {}
        coastline_land_rings = _assemble_coastline_rings(
            osm_data, nodes_by_id, ways_by_id,
            center=center, bounds=bounds, mpc=mpc, cells=cells,
            stats=coastline_stats,
        )
        if coastline_land_rings:
            _status(f"Coastline found ({len(coastline_land_rings)} land rings) -- defaulting grid to WATER")
            # Coastlines found: init grid as WATER, then carve land
            tiles_type = [[WATER_TEMPLATE_ID for _ in range(height)] for _ in range(width)]
            for ri, ring in enumerate(coastline_land_rings):
                _status(f"  Filling land ring {ri + 1}/{len(coastline_land_rings)} ({len(ring)} vertices)...")
                coastline_land_cells += _fill_polygon(tiles_type, ring, CLEAR_TEMPLATE_ID)
            _status(f"  Land carved: {coastline_land_cells} cells")
            coastline_stats["coastline_land_cells"] = coastline_land_cells
        else:
            _status("No coastlines found (inland area) -- defaulting grid to CLEAR")
            # No coastlines (inland area): default to CLEAR as before
            tiles_type = [[CLEAR_TEMPLATE_ID for _ in range(height)] for _ in range(width)]
    else:
        # Coastline processing disabled or water disabled: default to CLEAR
        tiles_type = [[CLEAR_TEMPLATE_ID for _ in range(height)] for _ in range(width)]
        coastline_stats = {}

    # Collect sample points for river smoothing along waterways.
    # Tuple is (i, j, orient) where orient: 0=vertical, 1=horizontal.
    river_samples_set: Set[Tuple[int, int, int]] = set()

    # Helper to clamp
    def _clamp_cell(pt: Tuple[float, float]) -> Optional[Tuple[float, float]]:
        x, y = pt
        if x < -1 or y < -1 or x > width + 1 or y > height + 1:
            return None
        return (x, y)

    # Rasterize water first (areas + waterways)
    stats = {"water_cells": 0, "road_cells": 0, "veg_actors": 0, "building_actors": 0}
    # Merge coastline stats
    stats.update(coastline_stats)
    # Debug counters
    stats["osm_nodes"] = len(nodes_by_id)
    if worldcover_masks is not None:
        stats["worldcover_used"] = 1
    if gsw_water_mask is not None:
        stats["gsw_used"] = 1
    if include_water:
        _status("Rasterizing water areas and waterways...")
        # Width mapping for waterways (meters)
        water_width_by_type: Dict[str, float] = {
            "river": max(waterway_width_m, 12.0),
            "canal": max(waterway_width_m, 8.0),
            "stream": min(waterway_width_m, max(3.0, waterway_width_m)),
        }
        # Areas
        for el in osm_data.get("elements", []):
            if el.get("type") != "way":
                continue
            tags = el.get("tags", {}) or {}
            if not tags:
                continue
            is_water_area = tags.get("natural") == "water" or tags.get("landuse") == "reservoir"
            if is_water_area:
                ring = _assemble_way_nodes(el, nodes_by_id, center=center, bounds=bounds, mpc=mpc)
                if len(ring) >= 3:
                    # Close if needed
                    if ring[0] != ring[-1]:
                        ring = ring + [ring[0]]
                    stats["water_cells"] += _fill_polygon(tiles_type, [p for p in ring], WATER_TEMPLATE_ID)

        # Waterways (lines)
        for el in osm_data.get("elements", []):
            if el.get("type") != "way":
                continue
            tags = el.get("tags", {}) or {}
            if not tags or not ("waterway" in tags):
                continue
            wtype = str(tags.get("waterway", "")).lower()
            # Parse explicit width if available
            width_override = None
            wtag = tags.get("width")
            if isinstance(wtag, str):
                try:
                    width_override = float("".join([c for c in wtag if (c.isdigit() or c in ".-")]))
                except (ValueError, TypeError):
                    width_override = None
            width_m = width_override if width_override is not None else water_width_by_type.get(wtype, waterway_width_m)
            r_cells = max(0.5, width_m / max(0.01, mpc) / 2.0)
            coords = _assemble_way_nodes(el, nodes_by_id, center=center, bounds=bounds, mpc=mpc)
            for a, b in zip(coords, coords[1:]):
                pa = _clamp_cell(a)
                pb = _clamp_cell(b)
                if not pa or not pb:
                    continue
                stats["water_cells"] += _rasterize_line(tiles_type, pa[0], pa[1], pb[0], pb[1], r_cells, WATER_TEMPLATE_ID)
                # Collect sparse sample points for river smoothing stamps along segment
                dx = pb[0] - pa[0]
                dy = pb[1] - pa[1]
                orient = 0 if abs(dy) >= abs(dx) else 1
                seg_len = math.hypot(dx, dy)
                # Step roughly every (max(1, r_cells)) cells to avoid over-stamping
                step = max(1.0, float(r_cells))
                steps = max(1, int(seg_len / step))
                for s in range(steps + 1):
                    t = s / max(1, steps)
                    x = pa[0] + (pb[0] - pa[0]) * t
                    y = pa[1] + (pb[1] - pa[1]) * t
                    ii = int(x)
                    jj = int(y)
                    if 0 <= ii < width and 0 <= jj < height:
                        river_samples_set.add((ii, jj, orient))

    # Augment water from GSW mask (if provided)
    if include_water and gsw_water_mask:
        added = 0
        for (i, j) in gsw_water_mask:
            if 0 <= i < width and 0 <= j < height:
                if tiles_type[i][j] != WATER_TEMPLATE_ID:
                    tiles_type[i][j] = WATER_TEMPLATE_ID
                    added += 1
        stats["water_cells"] += added

    # Process water multipolygon relations (large bays, ocean inlets, etc.)
    if include_water:
        _process_water_relations(
            osm_data, nodes_by_id, ways_by_id, tiles_type,
            center=center, bounds=bounds, mpc=mpc, stats=stats,
        )

    # River smoothing pass: stamp river templates along sampled waterway points
    if include_water and river_samples_set:
        river_stamp_count = 0
        for (ii, jj, orient) in list(river_samples_set):
            # Only consider points rasterized as WATER (from polygons/lines)
            if tiles_type[ii][jj] != WATER_TEMPLATE_ID:
                continue
            # Skip stamping in predominantly open water regions (estuaries/sea)
            if _local_water_fraction(tiles_type, ii, jj, radius=4) > RIVER_STAMP_SKIP_WATER_FRAC:
                continue
            if orient == 0:
                # Vertical: center column river, 3x2 template anchored one cell left of center
                river_stamp_count += _stamp_template(tiles_type, tiles_variant, ii - 1, jj, RIVER_TEMPLATE_VERT_CENTER)
            else:
                # Horizontal: alternate two 2x2 templates with rivers on the top row
                templ = RIVER_TEMPLATE_HORIZ_TOP if ((ii + jj) & 1) == 0 else RIVER_TEMPLATE_HORIZ_TOP_ALT
                river_stamp_count += _stamp_template(tiles_type, tiles_variant, ii, jj, templ)
        if river_stamp_count:
            stats["river_stamps"] = river_stamp_count
            stats["river_samples"] = len(river_samples_set)

    _status("Applying shoreline smoothing...")
    # Simple shoreline pass: mark land cells adjacent to water as Beach tiles.
    # This is an MVP smoothing step. We use Template@6 (sh04.tem) with a Beach variant index.
    if include_water:
        shore_count = 0
        # Choose a generic Beach variant from Template 6.
        BEACH_VARIANT = 4  # indices 0..5 are Beach per tileset; 4 chosen as a neutral-looking tile
        for i in range(width):
            for j in range(height):
                # Only convert previously Clear cells to Beach if they touch water
                if tiles_type[i][j] != CLEAR_TEMPLATE_ID:
                    continue
                has_water_neighbor = False
                # 8-neighborhood to soften diagonals too
                for di in (-1, 0, 1):
                    if has_water_neighbor:
                        break
                    for dj in (-1, 0, 1):
                        if di == 0 and dj == 0:
                            continue
                        ni = i + di
                        nj = j + dj
                        if 0 <= ni < width and 0 <= nj < height:
                            neighbor_id = tiles_type[ni][nj]
                            if neighbor_id == WATER_TEMPLATE_ID or neighbor_id in RIVER_TEMPLATE_IDS:
                                has_water_neighbor = True
                                break
                if has_water_neighbor:
                    tiles_type[i][j] = BEACH_TEMPLATE_ID
                    tiles_variant[i][j] = BEACH_VARIANT
                    shore_count += 1
        stats["shore_cells"] = shore_count

    # Rasterize roads second
    if include_roads:
        _status("Rasterizing roads...")
        # Width mapping for highway types (meters)
        road_width_by_type: Dict[str, float] = {
            "motorway": max(road_width_m, 16.0),
            "trunk": max(road_width_m, 14.0),
            "primary": max(road_width_m, 12.0),
            "secondary": max(road_width_m, 10.0),
            "tertiary": max(road_width_m, 9.0),
            "unclassified": max(road_width_m, 8.0),
            "residential": max(road_width_m, 8.0),
            "living_street": max(road_width_m, 6.0),
            "service": max(road_width_m, 6.0),
            "track": max(road_width_m, 5.0),
            "pedestrian": max(road_width_m, 6.0),
            "footway": max(3.0, min(road_width_m, 4.0)),
            "path": max(3.0, min(road_width_m, 4.0)),
            "cycleway": max(3.0, min(road_width_m, 4.0)),
            "steps": max(2.0, min(road_width_m, 3.0)),
            "bus_guideway": max(road_width_m, 6.0),
        }
        r_cells = max(0.5, road_width_m / max(0.01, mpc) / 2.0)
        road_ways = 0
        road_segments = 0
        for el in osm_data.get("elements", []):
            if el.get("type") != "way":
                continue
            tags = el.get("tags", {}) or {}
            if not tags or not ("highway" in tags):
                continue
            road_ways += 1
            htype = str(tags.get("highway", "")).lower()
            # Parse explicit width if present
            width_override = None
            wtag = tags.get("width")
            if isinstance(wtag, str):
                try:
                    width_override = float("".join([c for c in wtag if (c.isdigit() or c in ".-")]))
                except (ValueError, TypeError):
                    width_override = None
            width_m = width_override if width_override is not None else road_width_by_type.get(htype, road_width_m)
            r_cells = max(0.5, width_m / max(0.01, mpc) / 2.0)
            coords = _assemble_way_nodes(el, nodes_by_id, center=center, bounds=bounds, mpc=mpc)
            if len(coords) >= 2:
                road_segments += (len(coords) - 1)
            for a, b in zip(coords, coords[1:]):
                pa = _clamp_cell(a)
                pb = _clamp_cell(b)
                if not pa or not pb:
                    continue
                stats["road_cells"] += _rasterize_line(tiles_type, pa[0], pa[1], pb[0], pb[1], r_cells, ROAD_TEMPLATE_ID)
        stats["road_ways"] = road_ways
        stats["road_segments"] = road_segments

    # Road intersection stamping (3x3) based on 4-neighbor road adjacency
    # Use Templates 206/207 (both 3x3 road junctions) and alternate for variation.
    if include_roads:
        junction_stamps = 0
        stamped_anchors: Set[Tuple[int, int]] = set()
        for i in range(1, width - 1):
            for j in range(1, height - 1):
                if tiles_type[i][j] not in ROAD_BASE_TEMPLATE_IDS:
                    continue
                n = tiles_type[i][j - 1] in ROAD_BASE_TEMPLATE_IDS
                s = tiles_type[i][j + 1] in ROAD_BASE_TEMPLATE_IDS
                w_ = tiles_type[i - 1][j] in ROAD_BASE_TEMPLATE_IDS
                e = tiles_type[i + 1][j] in ROAD_BASE_TEMPLATE_IDS
                if int(n) + int(s) + int(w_) + int(e) >= 3:
                    ai = i - 1
                    aj = j - 1
                    if ai < 0 or aj < 0 or ai + 2 >= width or aj + 2 >= height:
                        continue
                    if (ai, aj) in stamped_anchors:
                        continue
                    templ = 206 if ((i + j) & 1) == 0 else 207
                    wrote = _stamp_template(tiles_type, tiles_variant, ai, aj, templ)
                    if wrote > 0:
                        junction_stamps += 1
                        stamped_anchors.add((ai, aj))
        if junction_stamps:
            stats["road_junction_stamps"] = junction_stamps

    # Precompute forest and built-up cell sets from OSM polygons (and WorldCover if provided)
    forest_cells: Set[Tuple[int, int]] = set()
    builtup_cells: Set[Tuple[int, int]] = set()
    for el in osm_data.get("elements", []):
        if el.get("type") != "way":
            continue
        tags = el.get("tags", {}) or {}
        if not tags:
            continue
        is_forest = (
            tags.get("natural") == "wood" or
            tags.get("landuse") == "forest" or
            tags.get("landcover") == "trees"
        )
        is_builtup = str(tags.get("landuse", "").lower()) in {"residential", "industrial", "commercial"}
        if is_forest or is_builtup:
            ring = _assemble_way_nodes(el, nodes_by_id, center=center, bounds=bounds, mpc=mpc)
            if len(ring) >= 3:
                if ring[0] != ring[-1]:
                    ring = ring + [ring[0]]
                w = len(tiles_type)
                h = len(tiles_type[0]) if w else 0
                xs = [p[0] for p in ring]
                ys = [p[1] for p in ring]
                xmin = max(0, int(min(xs)) - 1)
                xmax = min(w - 1, int(max(xs)) + 1)
                ymin = max(0, int(min(ys)) - 1)
                ymax = min(h - 1, int(max(ys)) + 1)
                for i in range(xmin, xmax + 1):
                    for j in range(ymin, ymax + 1):
                        cx = i + 0.5
                        cy = j + 0.5
                        if _point_in_poly(cx, cy, ring):
                            if is_forest:
                                forest_cells.add((i, j))
                            elif is_builtup:
                                builtup_cells.add((i, j))

    if worldcover_masks:
        wc_built = worldcover_masks.get("builtup_cells", set())
        wc_forest_pref = worldcover_masks.get("forest_pref_cells", set())
        builtup_cells |= wc_built
        forest_cells |= wc_forest_pref

    # Simple urban patch tiling: convert built-up CLEAR cells to asphalt (1x1 road tile)
    urban_cells = 0
    for (i, j) in builtup_cells:
        if 0 <= i < width and 0 <= j < height:
            if tiles_type[i][j] == CLEAR_TEMPLATE_ID:
                tiles_type[i][j] = ROAD_TEMPLATE_ID
                tiles_variant[i][j] = 0
                urban_cells += 1
    if urban_cells:
        stats["urban_cells"] = urban_cells

    # Vegetation and Building actors over remaining cells (use precomputed sets)
    extra_actors: List[str] = []
    # Cells occupied by multi-tile actors (e.g., buildings); shared across stages
    occupied_cells: Set[Tuple[int, int]] = set()

    # Build a set of road cells for adjacency checks (after urban patching)
    road_cells: Set[Tuple[int, int]] = set()
    for i in range(width):
        for j in range(height):
            if tiles_type[i][j] in ROAD_ALL_TEMPLATE_IDS:
                road_cells.add((i, j))

    # OSM Buildings -> RA civilian building actors
    if include_buildings:
        _status("Placing buildings...")
        placed_b = 0
        total_osm_b = 0
        audit_rows: List[str] = []

        # Preferred actor lists by (w,h) footprint in tiles
        by_dims: Dict[Tuple[int, int], List[str]] = {
            (1, 1): ["LHUS"],
            (1, 2): ["RUSHOUSE"],
            (2, 1): ["V22", "V26", "V30", "V31", "V32", "V33"],
            (2, 2): ["V20", "V21", "V24", "V25"],
        }

        def _can_place(i0: int, j0: int, w_: int, h_: int) -> bool:
            if i0 < 0 or j0 < 0 or (i0 + w_ - 1) >= width or (j0 + h_ - 1) >= height:
                return False
            for dx in range(w_):
                for dy in range(h_):
                    ii = i0 + dx
                    jj = j0 + dy
                    if tiles_type[ii][jj] == WATER_TEMPLATE_ID:
                        return False
                    if tiles_type[ii][jj] in RIVER_TEMPLATE_IDS:
                        return False
                    if tiles_type[ii][jj] == BEACH_TEMPLATE_ID:
                        return False
                    if (ii, jj) in occupied_cells:
                        return False
            return True

        def _dims_with_fallbacks(w_fit: int, h_fit: int, mode: str) -> List[Tuple[int, int]]:
            seq: List[Tuple[int, int]] = [(w_fit, h_fit)]
            if mode in ("fallback", "aggressive"):
                # Try smaller/easier footprints
                opts = []
                if (w_fit, h_fit) == (2, 2):
                    opts = [(2, 1), (1, 2), (1, 1)]
                elif (w_fit, h_fit) in [(2, 1), (1, 2)]:
                    opts = [(1, 1)]
                for d in opts:
                    if d not in seq:
                        seq.append(d)
            return seq

        def _place_from_bbox(id_type: str, osm_id: int, tags: Dict[str, Any], xs: List[float], ys: List[float]) -> None:
            nonlocal placed_b, total_osm_b
            reason = ""
            placed_here = False
            if placed_b >= int(max_buildings):
                return
            total_osm_b += 1
            if random.random() > max(0.0, min(1.0, float(building_density))):
                audit_rows.append(f"{id_type},{osm_id},0,density_skip,0,0,0,0")
                return
            if not xs or not ys:
                audit_rows.append(f"{id_type},{osm_id},0,no_coords,0,0,0,0")
                return
            min_x, max_x = min(xs), max(xs)
            min_y, max_y = min(ys), max(ys)
            bbox_w = max(1, int(round(max_x - min_x)))
            bbox_h = max(1, int(round(max_y - min_y)))
            w_fit = max(1, min(2, bbox_w))
            h_fit = max(1, min(2, bbox_h))
            # Centered anchor
            ai = int(round((min_x + max_x) / 2.0 - w_fit / 2.0))
            aj = int(round((min_y + max_y) / 2.0 - h_fit / 2.0))
            ai = max(0, min(width - w_fit, ai))
            aj = max(0, min(height - h_fit, aj))

            mode = str(building_placement_mode or "accurate")
            base_r = int(max(0, building_search_radius))
            local_r = base_r * (2 if mode == "aggressive" else 1)
            dims_list = _dims_with_fallbacks(w_fit, h_fit, mode)

            chosen_actor = None
            chosen_dims: Tuple[int, int] = (w_fit, h_fit)
            for (wf, hf) in dims_list:
                actor_choices = by_dims.get((wf, hf))
                if not actor_choices:
                    continue
                for dj in range(-local_r, local_r + 1):
                    if placed_here:
                        break
                    for di in range(-local_r, local_r + 1):
                        ci = ai + di
                        cj = aj + dj
                        if _can_place(ci, cj, wf, hf):
                            chosen_actor = random.choice(actor_choices)
                            # Emit actor
                            extra_actors.append(f"\tBld{placed_b}: {chosen_actor}\n\t\tLocation: {ci},{cj}\n\t\tOwner: Neutral")
                            for dx in range(wf):
                                for dy in range(hf):
                                    occupied_cells.add((ci + dx, cj + dy))
                            placed_b += 1
                            placed_here = True
                            chosen_dims = (wf, hf)
                            break
                if placed_b >= int(max_buildings) or placed_here:
                    break

            if placed_here:
                audit_rows.append(f"{id_type},{osm_id},1,ok,{chosen_dims[0]},{chosen_dims[1]},{bbox_w},{bbox_h}")
            else:
                audit_rows.append(f"{id_type},{osm_id},0,search_fail,{w_fit},{h_fit},{bbox_w},{bbox_h}")

        # Pass 1: building ways
        for el in osm_data.get("elements", []):
            if placed_b >= int(max_buildings):
                break
            if el.get("type") != "way":
                continue
            tags = el.get("tags", {}) or {}
            if "building" not in tags:
                continue
            ring = _assemble_way_nodes(el, nodes_by_id, center=center, bounds=bounds, mpc=mpc)
            if len(ring) >= 3:
                xs = [p[0] for p in ring]
                ys = [p[1] for p in ring]
                _place_from_bbox("way", int(el.get("id", -1)), tags, xs, ys)

        # Pass 2: building relations (multipolygons)
        for el in osm_data.get("elements", []):
            if placed_b >= int(max_buildings):
                break
            if el.get("type") != "relation":
                continue
            tags = el.get("tags", {}) or {}
            if "building" not in tags:
                continue
            members = el.get("members", []) or []
            xs_all: List[float] = []
            ys_all: List[float] = []
            for m in members:
                if m.get("type") != "way" or m.get("role") not in ("outer", None, "outline"):
                    continue
                wid = m.get("ref")
                if wid is None:
                    continue
                w_el = ways_by_id.get(int(wid))
                if not w_el:
                    continue
                coords = _assemble_way_nodes(w_el, nodes_by_id, center=center, bounds=bounds, mpc=mpc)
                if not coords:
                    continue
                xs_all.extend([p[0] for p in coords])
                ys_all.extend([p[1] for p in coords])
            if xs_all and ys_all:
                _place_from_bbox("relation", int(el.get("id", -1)), tags, xs_all, ys_all)

        stats["building_actors"] = placed_b
        stats["osm_buildings"] = total_osm_b

        # Optional: write audit CSV
        if building_audit_path and audit_rows:
            try:
                dirn = os.path.dirname(building_audit_path)
                if dirn:
                    os.makedirs(dirn, exist_ok=True)
                with open(building_audit_path, "w", encoding="utf-8") as f:
                    f.write("id_type,osm_id,placed,reason,w_fit,h_fit,bbox_w,bbox_h\n")
                    for row in audit_rows:
                        f.write(row + "\n")
            except Exception as e:
                print(f"Warning: failed to write building audit CSV: {e}", file=sys.stderr)
    if include_vegetation:
        _status("Placing vegetation...")
        tree_types = ["t01", "t02", "t03", "t05", "t06", "t07", "t08", "t10", "t11", "t12", "t13"]
        target = int(max_veg_actors)
        base_prob = max(0.0, min(1.0, veg_density))
        spacing = max(0, int(veg_min_spacing))
        road_r = max(0, int(suppress_veg_near_roads))
        bld_r = max(0, int(suppress_veg_near_buildings))
        placed = 0
        skipped_water = skipped_built = 0
        skipped_road_adj = skipped_bld_adj = skipped_spacing = 0
        skipped_tiletype = 0

        # Patch-based local density boost
        ps = max(1, int(veg_patch_size))
        patch_counts: Dict[Tuple[int, int], int] = {}
        patch_totals: Dict[Tuple[int, int], int] = {}
        for (i, j) in forest_cells:
            pi, pj = i // ps, j // ps
            patch_counts[(pi, pj)] = patch_counts.get((pi, pj), 0) + 1
        # Compute totals per patch considering edges
        max_pi = (width + ps - 1) // ps
        max_pj = (height + ps - 1) // ps
        for pi in range(max_pi):
            for pj in range(max_pj):
                w_rem = min(ps, width - pi * ps)
                h_rem = min(ps, height - pj * ps)
                patch_totals[(pi, pj)] = max(1, w_rem * h_rem)
        densities: List[float] = []
        for key, cnt in patch_counts.items():
            densities.append(cnt / float(patch_totals.get(key, ps * ps)))
        densities.sort()
        median = densities[len(densities) // 2] if densities else 0.0
        high_patches: Set[Tuple[int, int]] = set(k for k, cnt in patch_counts.items()
                                                if (cnt / float(patch_totals.get(k, ps * ps))) >= median)
        if high_patches:
            stats["veg_patches_high"] = len(high_patches)

        # Track placed tree cells for spacing constraint
        veg_occupied: Set[Tuple[int, int]] = set()
        for (i, j) in forest_cells:
            if placed >= target:
                break
            # Skip unsuitable terrain
            if tiles_type[i][j] == WATER_TEMPLATE_ID:
                skipped_water += 1
                continue
            if tiles_type[i][j] in RIVER_TEMPLATE_IDS or tiles_type[i][j] == BEACH_TEMPLATE_ID or tiles_type[i][j] in ROAD_ALL_TEMPLATE_IDS:
                skipped_tiletype += 1
                continue
            if (i, j) in builtup_cells:
                skipped_built += 1
                continue

            # Local probability with patch boost
            pi, pj = i // ps, j // ps
            prob = base_prob * (float(veg_patch_boost) if (pi, pj) in high_patches else 1.0)
            if prob > 1.0:
                prob = 1.0
            if random.random() > prob:
                continue

            # Suppress near roads
            suppressed = False
            if road_r > 0:
                for di in range(-road_r, road_r + 1):
                    if suppressed:
                        break
                    for dj in range(-road_r, road_r + 1):
                        if (i + di, j + dj) in road_cells:
                            skipped_road_adj += 1
                            suppressed = True
                            break
                if suppressed:
                    continue

            # Suppress near buildings
            if bld_r > 0:
                suppressed = False
                for di in range(-bld_r, bld_r + 1):
                    if suppressed:
                        break
                    for dj in range(-bld_r, bld_r + 1):
                        if (i + di, j + dj) in occupied_cells:
                            skipped_bld_adj += 1
                            suppressed = True
                            break
                if suppressed:
                    continue

            # Spacing constraint (Chebyshev)
            too_close = False
            if spacing > 0:
                for di in range(-spacing, spacing + 1):
                    if too_close:
                        break
                    for dj in range(-spacing, spacing + 1):
                        if (i + di, j + dj) in veg_occupied:
                            too_close = True
                            break
                if too_close:
                    skipped_spacing += 1
                    continue

            # Place tree
            tname = random.choice(tree_types)
            extra_actors.append(f"\tTree{placed}: {tname}\n\t\tLocation: {i},{j}\n\t\tOwner: Neutral")
            veg_occupied.add((i, j))
            placed += 1
        stats["veg_actors"] = placed
        if skipped_water:
            stats["veg_skipped_water"] = skipped_water
        if skipped_built:
            stats["veg_skipped_builtup"] = skipped_built
        if skipped_tiletype:
            stats["veg_skipped_tiletype"] = skipped_tiletype
        if skipped_road_adj:
            stats["veg_skipped_road_adj"] = skipped_road_adj
        if skipped_bld_adj:
            stats["veg_skipped_building_adj"] = skipped_bld_adj
        if skipped_spacing:
            stats["veg_skipped_spacing"] = skipped_spacing
    # Report masks in stats for visibility regardless of include_vegetation
    stats["forest_cells"] = len(forest_cells)
    stats["builtup_cells"] = len(builtup_cells)

    return tiles_type, tiles_variant, extra_actors, stats


def build_worldcover_masks(worldcover_path: str, *, center: Dict[str, Any], bounds: Dict[str, Any], mpc: float, cells: int) -> Optional[Dict[str, Set[Tuple[int, int]]]]:
    """Load ESA WorldCover GeoTIFF and produce built-up and forest-preference cell sets for the AOI grid.
    Returns None if rasterio is unavailable or file cannot be read.
    """
    if rasterio is None or not worldcover_path or not os.path.exists(worldcover_path):
        return None
    try:
        built: Set[Tuple[int, int]] = set()
        forest_pref: Set[Tuple[int, int]] = set()
        with rasterio.open(worldcover_path) as ds:
            width = height = int(cells)
            # Sample per cell center
            # Build in chunks to limit memory
            chunk = 8192
            coords: List[Tuple[float, float]] = []
            ij_list: List[Tuple[int, int]] = []
            def flush_sample():
                nonlocal coords, ij_list
                if not coords:
                    return
                sample_coords = coords
                try:
                    ds_crs = getattr(ds, "crs", None)
                    if ds_crs and str(ds_crs).upper() not in ("EPSG:4326", "WGS84"):
                        from rasterio.warp import transform as rio_transform  # type: ignore
                        lons, lats = zip(*coords)
                        xs, ys = rio_transform("EPSG:4326", ds_crs, list(lons), list(lats))
                        sample_coords = list(zip(xs, ys))
                except Exception as e:
                    print(f"Warning: WorldCover CRS reprojection failed, using raw coords: {e}", file=sys.stderr)
                    sample_coords = coords
                for v, (ii, jj) in zip(ds.sample(sample_coords), ij_list):
                    try:
                        val = int(v[0])
                    except (ValueError, TypeError, IndexError):
                        continue
                    if val == 50:  # Built-up
                        built.add((ii, jj))
                    elif val in (10, 20):  # Tree cover / Shrubland
                        forest_pref.add((ii, jj))
                coords = []
                ij_list = []
            for i in range(width):
                for j in range(height):
                    lat, lon = _cell_to_latlon(i, j, center=center, bounds=bounds, mpc=mpc)
                    coords.append((lon, lat))
                    ij_list.append((i, j))
                    if len(coords) >= chunk:
                        flush_sample()
            flush_sample()
        return {"builtup_cells": built, "forest_pref_cells": forest_pref}
    except Exception as e:
        print(f"Warning: failed to read WorldCover raster '{worldcover_path}': {e}", file=sys.stderr)
        return None


def build_gsw_water_mask(gsw_path: str, *, center: Dict[str, Any], bounds: Dict[str, Any], mpc: float, cells: int,
                         min_occurrence: Optional[float] = None) -> Optional[Set[Tuple[int, int]]]:
    """Load JRC GSW raster and return set of AOI grid cells considered water.

    If the raster is an occurrence product (values 0..100), cells with value >= min_occurrence (default 75) are water.
    If the raster is a binary mask (values 0/1), any value > 0 is water.
    Returns None if rasterio is unavailable or file cannot be read.
    """
    if rasterio is None or not gsw_path or not os.path.exists(gsw_path):
        return None
    try:
        water: Set[Tuple[int, int]] = set()
        with rasterio.open(gsw_path) as ds:
            width = height = int(cells)
            # Detect binary mask (0/1) vs. occurrence (0-100) via band statistics
            is_binary = False
            try:
                band_stats = ds.statistics(1)
                is_binary = band_stats.max <= 1.0
            except Exception:
                # Fallback: check dtype — uint8 with nodata=255 is likely occurrence
                dt = str(ds.dtypes[0]).lower()
                is_binary = dt in ("uint8",) and ds.nodata is None

            thr = 0.0 if min_occurrence is None else float(min_occurrence)
            chunk = 8192
            coords: List[Tuple[float, float]] = []
            ij_list: List[Tuple[int, int]] = []
            def flush_sample():
                nonlocal coords, ij_list
                if not coords:
                    return
                sample_coords = coords
                try:
                    ds_crs = getattr(ds, "crs", None)
                    if ds_crs and str(ds_crs).upper() not in ("EPSG:4326", "WGS84"):
                        from rasterio.warp import transform as rio_transform  # type: ignore
                        lons, lats = zip(*coords)
                        xs, ys = rio_transform("EPSG:4326", ds_crs, list(lons), list(lats))
                        sample_coords = list(zip(xs, ys))
                except Exception as e:
                    print(f"Warning: GSW CRS reprojection failed, using raw coords: {e}", file=sys.stderr)
                    sample_coords = coords
                for v, (ii, jj) in zip(ds.sample(sample_coords), ij_list):
                    try:
                        val = float(v[0])
                    except (ValueError, TypeError, IndexError):
                        continue
                    if is_binary:
                        if val > 0.0:
                            water.add((ii, jj))
                    else:
                        if val >= thr:
                            water.add((ii, jj))
                coords = []
                ij_list = []
            for i in range(width):
                for j in range(height):
                    lat, lon = _cell_to_latlon(i, j, center=center, bounds=bounds, mpc=mpc)
                    coords.append((lon, lat))
                    ij_list.append((i, j))
                    if len(coords) >= chunk:
                        flush_sample()
            flush_sample()
        return water
    except Exception as e:
        print(f"Warning: failed to read GSW raster '{gsw_path}': {e}", file=sys.stderr)
        return None


def write_oramap_zip(dst_path: str, map_yaml: str, map_bin: bytes) -> None:
    import zipfile
    os.makedirs(os.path.dirname(dst_path) or ".", exist_ok=True)
    with zipfile.ZipFile(dst_path, "w", compression=zipfile.ZIP_DEFLATED) as z:
        z.writestr("map.yaml", map_yaml)
        z.writestr("map.bin", map_bin)


def _openra_maps_base() -> str:
    """Return the platform-appropriate OpenRA RA maps directory."""
    home = os.path.expanduser("~")
    system = platform.system()
    if system == "Windows":
        appdata = os.environ.get("APPDATA", os.path.join(home, "AppData", "Roaming"))
        return os.path.join(appdata, "OpenRA", "maps", "ra")
    elif system == "Darwin":
        return os.path.join(home, "Library", "Application Support", "OpenRA", "maps", "ra")
    else:
        # Linux / other Unix
        xdg = os.environ.get("XDG_CONFIG_HOME", os.path.join(home, ".config"))
        return os.path.join(xdg, "openra", "maps", "ra")


def install_oramap_to_openra(src_oramap: str, *, release_tag: Optional[str] = None, explicit_dir: Optional[str] = None) -> str:
    """Copy the given .oramap into the user's OpenRA RA maps directory.
    Detects platform automatically (Windows/macOS/Linux).
    If explicit_dir is given, copy there instead.
    Returns the destination directory used.
    """
    base = _openra_maps_base()
    target_dir = explicit_dir if explicit_dir else base
    if not explicit_dir and release_tag:
        target_dir = os.path.join(base, f"release-{release_tag}")
    elif not explicit_dir and not release_tag:
        # Try to prefer a release-* subdir if one exists (pick most recently modified)
        try:
            entries = [d for d in os.listdir(base) if d.startswith("release-") and os.path.isdir(os.path.join(base, d))]
            if entries:
                entries.sort(key=lambda d: os.path.getmtime(os.path.join(base, d)), reverse=True)
                target_dir = os.path.join(base, entries[0])
        except FileNotFoundError:
            target_dir = base
    os.makedirs(target_dir, exist_ok=True)
    dst_path = os.path.join(target_dir, os.path.basename(src_oramap))
    shutil.copy2(src_oramap, dst_path)
    return target_dir


def bbox_from_corners(corners: Dict[str, Dict[str, float]]) -> Dict[str, float]:
    lats = [corners[k]["lat"] for k in ("NW", "NE", "SE", "SW")]
    lons = [corners[k]["lon"] for k in ("NW", "NE", "SE", "SW")]
    return {
        "south": min(lats),
        "west": min(lons),
        "north": max(lats),
        "east": max(lons),
    }


def build_overpass_query(bbox: Dict[str, float], *, timeout: int = 25) -> str:
    s, w, n, e = bbox["south"], bbox["west"], bbox["north"], bbox["east"]
    # Highways and water features within bbox
    return (
        f"[out:json][timeout:{timeout}];"
        f"("  # initial selection
        f"way['highway']({s},{w},{n},{e});"
        f"way['waterway']({s},{w},{n},{e});"
        f"way['natural'='water']({s},{w},{n},{e});"
        f"way['landuse'='reservoir']({s},{w},{n},{e});"
        f"way['natural'='coastline']({s},{w},{n},{e});"
        f"way['building']({s},{w},{n},{e});"
        f"relation['building']({s},{w},{n},{e});"
        f"way['natural'='wood']({s},{w},{n},{e});"
        f"way['landuse'='forest']({s},{w},{n},{e});"
        f"way['landcover'='trees']({s},{w},{n},{e});"
        f"way['landuse'='residential']({s},{w},{n},{e});"
        f"way['landuse'='industrial']({s},{w},{n},{e});"
        f"way['landuse'='commercial']({s},{w},{n},{e});"
        f");"
        f"(._;>;>;);"  # keep ways and relations; bring member ways and their nodes
        f"out body qt;"
    )


def fetch_osm(bbox: Dict[str, float], overpass_url: str, cache_dir: Optional[str] = None,
              timeout: int = 25) -> Dict[str, Any]:
    q = build_overpass_query(bbox, timeout=timeout)
    cache_path: Optional[str] = None
    if cache_dir:
        try:
            os.makedirs(cache_dir, exist_ok=True)
            # Include a cache schema version and the full query so changes to the query
            # (e.g., adding recursion to include nodes) do not reuse stale cached files.
            cache_version = "v3"
            key = (
                f"{cache_version}|{bbox['south']:.6f},{bbox['west']:.6f},"
                f"{bbox['north']:.6f},{bbox['east']:.6f}|{q}"
            )
            h = hashlib.sha256(key.encode("utf-8")).hexdigest()
            cache_path = os.path.join(cache_dir, f"{h}.json")
            if os.path.exists(cache_path):
                with open(cache_path, "r", encoding="utf-8") as f:
                    return json.load(f)
        except Exception as e:
            print(f"Warning: OSM cache read failed: {e}", file=sys.stderr)
            cache_path = None
    # HTTP timeout is query timeout + 20s headroom for network/server processing
    r = requests.post(overpass_url, data={"data": q}, timeout=timeout + 20)
    r.raise_for_status()
    data = r.json()
    if cache_path:
        try:
            with open(cache_path, "w", encoding="utf-8") as f:
                json.dump(data, f)
        except Exception as e:
            print(f"Warning: OSM cache write failed: {e}", file=sys.stderr)
    return data


def summarize_osm(data: Dict[str, Any]) -> Dict[str, Any]:
    roads: Dict[str, int] = {}
    waterways: Dict[str, int] = {}
    waters: Dict[str, int] = {"natural_water": 0, "reservoir": 0}
    buildings: Dict[str, int] = {"total": 0}
    landuse: Dict[str, int] = {}
    natural: Dict[str, int] = {}
    landcover: Dict[str, int] = {}
    for el in data.get("elements", []):
        tags = el.get("tags", {})
        if not tags:
            continue
        if "highway" in tags:
            key = str(tags["highway"]).lower()
            roads[key] = roads.get(key, 0) + 1
        if "waterway" in tags:
            key = str(tags["waterway"]).lower()
            waterways[key] = waterways.get(key, 0) + 1
        if tags.get("natural") == "water":
            waters["natural_water"] = waters.get("natural_water", 0) + 1
        if tags.get("landuse") == "reservoir":
            waters["reservoir"] = waters.get("reservoir", 0) + 1
        if "building" in tags:
            buildings["total"] += 1
            b = str(tags["building"]).lower()
            buildings[b] = buildings.get(b, 0) + 1
        if "landuse" in tags:
            lu = str(tags["landuse"]).lower()
            landuse[lu] = landuse.get(lu, 0) + 1
        if "natural" in tags:
            nat = str(tags["natural"]).lower()
            natural[nat] = natural.get(nat, 0) + 1
        if "landcover" in tags:
            lc = str(tags["landcover"]).lower()
            landcover[lc] = landcover.get(lc, 0) + 1
    return {
        "roads_by_type": roads,
        "waterways_by_type": waterways,
        "water_areas": waters,
        "buildings_by_type": buildings,
        "landuse_by_type": landuse,
        "natural_by_type": natural,
        "landcover_by_type": landcover,
    }


def _validate_geotransform(
    *,
    center: Dict[str, Any],
    bounds: Dict[str, Any],
    mpc: float,
    cells: int,
    extra_cell: Optional[Tuple[int, int]] = None,
    extra_latlon: Optional[Tuple[float, float]] = None,
) -> Dict[str, Any]:
    """Produce round-trip validation samples for GeoTransform.

    Returns a dict with two lists:
      - cell_to_latlon_roundtrip: [{cell, lat, lon, rt_error_m}]
      - latlon_to_cell_roundtrip: [{lat, lon, cell_f, cell_snap, rt_error_m}]
    """

    w = int(cells)
    h = int(cells)
 
    def _cell_roundtrip(i: int, j: int) -> Dict[str, Any]:
        lat, lon = _cell_to_latlon(i, j, center=center, bounds=bounds, mpc=mpc)
        cell = _latlon_to_cell(lat, lon, center=center, bounds=bounds, mpc=mpc)
        err_m = None
        if cell is not None:
            fx, fy = float(cell[0]), float(cell[1])
            dx_cells = fx - (i + 0.5)
            dy_cells = fy - (j + 0.5)
            err_m = float(math.hypot(dx_cells * mpc, dy_cells * mpc))
        return {
            "cell": [int(i), int(j)],
            "lat": float(lat),
            "lon": float(lon),
            "rt_error_m": err_m,
        }
 
    def _latlon_roundtrip(lat: float, lon: float) -> Dict[str, Any]:
        cell = _latlon_to_cell(lat, lon, center=center, bounds=bounds, mpc=mpc)
        if cell is None:
            return {"lat": float(lat), "lon": float(lon), "cell_f": None, "cell_snap": None, "rt_error_m": None}
        fx, fy = float(cell[0]), float(cell[1])
        i_snap = max(0, min(w - 1, int(fx)))
        j_snap = max(0, min(h - 1, int(fy)))
        lat2, lon2 = _cell_to_latlon(i_snap, j_snap, center=center, bounds=bounds, mpc=mpc)
        # Compute planar UTM error in meters between original and round-tripped lat/lon
        e1, n1, _, _ = utm.from_latlon(lat, lon)
        e2, n2, _, _ = utm.from_latlon(lat2, lon2)
        err_m = float(math.hypot(e2 - e1, n2 - n1))
        return {
            "lat": float(lat),
            "lon": float(lon),
            "cell_f": [fx, fy],
            "cell_snap": [i_snap, j_snap],
            "rt_error_m": err_m,
        }
 
    cell_samples: List[Tuple[int, int]] = [
        (0, 0),
        (w - 1, 0),
        (w - 1, h - 1),
        (0, h - 1),
        (w // 2, h // 2),
    ]
    if extra_cell is not None:
        ci, cj = extra_cell
        if 0 <= ci < w and 0 <= cj < h:
            cell_samples.append((ci, cj))
 
    latlon_samples: List[Tuple[float, float]] = [
        (bounds["corners"]["NW"]["lat"], bounds["corners"]["NW"]["lon"]),
        (bounds["corners"]["NE"]["lat"], bounds["corners"]["NE"]["lon"]),
        (bounds["corners"]["SE"]["lat"], bounds["corners"]["SE"]["lon"]),
        (bounds["corners"]["SW"]["lat"], bounds["corners"]["SW"]["lon"]),
        (center["lat"], center["lon"]),
    ]
    if extra_latlon is not None:
        latlon_samples.append((float(extra_latlon[0]), float(extra_latlon[1])))
 
    return {
        "cell_to_latlon_roundtrip": [
            _cell_roundtrip(i, j) for (i, j) in cell_samples
        ],
        "latlon_to_cell_roundtrip": [
            _latlon_roundtrip(lat, lon) for (lat, lon) in latlon_samples
        ],
    }


def main() -> None:
    args = parse_args()

    try:
        center = mgrs_to_center(args.mgrs)
    except Exception as e:
        err = {
            "error": "Invalid MGRS input",
            "mgrs": args.mgrs,
            "hint": "Format: <zone><band><100km grid 2 letters><easting><northing> with equal digits (e.g., 11SMT1234512345).",
            "exception": str(e),
        }
        print(json.dumps(err, indent=2) if args.pretty else json.dumps(err))
        sys.exit(2)
    bounds = compute_bounds(center, args.cells, args.meters_per_cell)

    out = {
        "input": {
            "mgrs": args.mgrs,
            "cells": args.cells,
            "meters_per_cell": args.meters_per_cell,
            "rotation_deg": args.rotation_deg,
            "tileset": args.tileset,
        },
        "center": center,
        "bounds": bounds,
    }

    if args.osm_summary:
        bbox = bbox_from_corners(bounds["corners"])
        try:
            osm_data = fetch_osm(bbox, args.overpass_url, None if args.no_osm_cache else args.osm_cache_dir,
                                 timeout=args.overpass_timeout)
            out["osm_summary"] = {
                "bbox": bbox,
                "counts": summarize_osm(osm_data),
            }
        except Exception as e:
            out["osm_summary"] = {
                "bbox": bbox,
                "error": str(e),
            }

    # Optional: write a minimal .oramap for OpenRA RA
    if args.write_oramap:
        width = int(args.cells)
        height = int(args.cells)
        extra_actor_lines: List[str] = []
        overlay_stats: Optional[Dict[str, int]] = None
        # RA uses flat terrain (heights layer omitted) by default.
        if args.overlay_osm or args.overlay_osm_buildings:
            bbox = bbox_from_corners(bounds["corners"])
            try:
                _status("Fetching OSM data from Overpass API...")
                osm_data = fetch_osm(bbox, args.overpass_url, None if args.no_osm_cache else args.osm_cache_dir,
                                     timeout=args.overpass_timeout)
                _status(f"OSM data received ({len(osm_data.get('elements', []))} elements)")
                # Optional dataset ingestions
                wc_masks = None
                gsw_mask = None
                if args.use_worldcover and args.worldcover_path:
                    wc_masks = build_worldcover_masks(args.worldcover_path, center=center, bounds=bounds, mpc=float(args.meters_per_cell), cells=width)
                if args.augment_water_gsw and args.gsw_path:
                    gsw_mask = build_gsw_water_mask(
                        args.gsw_path,
                        center=center,
                        bounds=bounds,
                        mpc=float(args.meters_per_cell),
                        cells=width,
                        min_occurrence=float(args.gsw_min_occurrence),
                    )
                _status("Processing OSM overlay...")
                tiles_type, tiles_variant, extra_actor_lines, overlay_stats = overlay_osm_to_tiles(
                    center=center,
                    bounds=bounds,
                    mpc=float(args.meters_per_cell),
                    cells=width,
                    osm_data=osm_data,
                    include_roads=(not bool(args.no_roads)) and bool(args.overlay_osm),
                    include_water=(not bool(args.no_water)) and bool(args.overlay_osm),
                    include_vegetation=(not bool(args.no_vegetation)) and bool(args.overlay_osm),
                    include_buildings=(not bool(args.no_buildings)) and bool(args.overlay_osm or args.overlay_osm_buildings),
                    include_coastline=(not bool(args.no_coastline)),
                    road_width_m=float(args.road_width_m),
                    waterway_width_m=float(args.waterway_width_m),
                    veg_density=float(args.veg_density),
                    max_veg_actors=int(args.max_veg_actors),
                    veg_min_spacing=int(args.veg_min_spacing),
                    veg_patch_size=int(args.veg_patch_size),
                    veg_patch_boost=float(args.veg_patch_boost),
                    suppress_veg_near_roads=int(args.suppress_veg_near_roads),
                    suppress_veg_near_buildings=int(args.suppress_veg_near_buildings),
                    building_density=float(args.building_density),
                    max_buildings=int(args.max_buildings),
                    building_search_radius=int(args.building_search_radius),
                    building_placement_mode=str(args.building_placement_mode),
                    building_audit_path=args.debug_building_audit,
                    worldcover_masks=wc_masks,
                    gsw_water_mask=gsw_mask,
                )
                _status("Building map.bin...")
                bin_bytes = build_map_bin_from_grid(tiles_type, tiles_variant, include_heights=False)
                _status("Overlay complete")
            except Exception as e:
                # Fallback to flat map if overlay fails
                bin_bytes = build_map_bin(width, height, default_template_id=255, default_variant=0, include_heights=False)
                overlay_stats = {"error": 1}  # mark error
                extra_actor_lines = []
                out["overlay_error"] = str(e)
        else:
            bin_bytes = build_map_bin(width, height, default_template_id=255, default_variant=0, include_heights=False)

        map_title = args.title or f"RealWorld {args.mgrs}"
        # Build metadata/attributions
        attributions: List[Dict[str, Any]] = []
        if args.overlay_osm or args.osm_summary or args.overlay_osm_buildings:
            attributions.append({
                "Name": "OpenStreetMap contributors",
                "License": "ODbL 1.0",
                "URL": "https://www.openstreetmap.org/copyright",
                "Source": f"Overpass API: {args.overpass_url}",
            })
        if args.use_worldcover:
            current_year = str(datetime.datetime.now().year)
            wc_attr = {
                "Name": "ESA WorldCover (10 m)",
                "License": "CC BY 4.0",
                "URL": "https://worldcover2020.esa.int/",
                "Notes": "Used for vegetation/built-up mask",
            }
            wc_attr["DatasetYear"] = args.worldcover_year or current_year
            attributions.append(wc_attr)
        if args.augment_water_gsw:
            current_year = str(datetime.datetime.now().year)
            gsw_attr = {
                "Name": "JRC Global Surface Water",
                "License": "CC BY 4.0",
                "URL": "https://global-surface-water.appspot.com/",
                "Notes": "Augments permanent water",
            }
            gsw_attr["DatasetVersion"] = args.gsw_version or current_year
            try:
                gsw_attr["MinOccurrencePct"] = float(args.gsw_min_occurrence)
            except (ValueError, TypeError):
                pass
            attributions.append(gsw_attr)
        metadata: Dict[str, Any] = {
            "GeoTransform": {
                "utm_zone_number": center["utm"]["zone_number"],
                "utm_zone_letter": center["utm"]["zone_letter"],
                "meters_per_cell": args.meters_per_cell,
                "rotation_deg": args.rotation_deg,
                "origin": {
                    "corner": "NW",
                    "lat": bounds["corners"]["NW"]["lat"],
                    "lon": bounds["corners"]["NW"]["lon"],
                    "utm_e": bounds["bbox_utm"]["min_e"],
                    "utm_n": bounds["bbox_utm"]["max_n"],
                },
                "grid": {"width": width, "height": height},
            },
            "Attributions": attributions,
        }
        yaml_text = build_map_yaml(
            title=map_title,
            author=args.author,
            tileset=args.tileset,
            width=width,
            height=height,
            categories=args.categories,
            num_players=args.players,
            place_spawns=bool(args.place_spawns),
            extra_actors=extra_actor_lines or None,
            metadata=metadata,
        )
        try:
            _status(f"Writing .oramap to {args.write_oramap}...")
            write_oramap_zip(args.write_oramap, yaml_text, bin_bytes)
            _status("Done!")
            oramap_info: Dict[str, Any] = {"path": args.write_oramap, "bytes_map_bin": len(bin_bytes), "notes": "map.yaml + map.bin written"}
            if overlay_stats:
                oramap_info["overlay_stats"] = overlay_stats
            out["oramap"] = oramap_info
            # Optional install into OpenRA maps directory for immediate visibility in-game
            if args.install_openra:
                try:
                    used_dir = install_oramap_to_openra(
                        args.write_oramap,
                        release_tag=(str(args.install_release) if args.install_release else None),
                        explicit_dir=(str(args.install_path) if args.install_path else None),
                    )
                    oramap_info["installed_to"] = used_dir
                except Exception as _e:
                    oramap_info["install_error"] = str(_e)
        except Exception as e:
            out["oramap_error"] = str(e)

    # Optional: GeoTransform validation (M3)
    if (
        args.validate_geotransform
        or (args.validate_cell is not None)
        or (args.validate_latlon is not None)
    ):
        extra_cell = None
        if args.validate_cell:
            try:
                parts = [p.strip() for p in str(args.validate_cell).split(",")]
                if len(parts) == 2:
                    extra_cell = (int(parts[0]), int(parts[1]))
            except (ValueError, TypeError):
                extra_cell = None
        extra_latlon = None
        if args.validate_latlon:
            try:
                parts = [p.strip() for p in str(args.validate_latlon).split(",")]
                if len(parts) == 2:
                    extra_latlon = (float(parts[0]), float(parts[1]))
            except (ValueError, TypeError):
                extra_latlon = None
        out["validation"] = _validate_geotransform(
            center=center,
            bounds=bounds,
            mpc=float(args.meters_per_cell),
            cells=int(args.cells),
            extra_cell=extra_cell,
            extra_latlon=extra_latlon,
        )

    if args.pretty:
        print(json.dumps(out, indent=2))
    else:
        print(json.dumps(out, separators=(",", ":")))


if __name__ == "__main__":
    main()
