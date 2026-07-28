#!/usr/bin/env python3
"""Procedural lunar/martian tile-sheet generator for the Space-Age mod.

OpenRA is a 2.5D sprite engine, so "procedural terrain" means generating the tile
SPRITES offline and baking them into a sheet that the tileset templates reference.
This produces a PNG sheet of top-down tiles: regolith variants, crater floor, and
crater-wall (cliff) tiles, using value noise so no external noise lib is needed.

Usage:
    python3 gen_tileset.py --out ../../artwork/lunar_tiles.png --tile 24 --cols 8 \
        --palette moon        # or: mars

Dependencies: numpy, Pillow.
Output: a PNG sheet + a manifest.txt listing tile index -> terrain type, which you
paste into tilesets/lunar.yaml Template blocks.
"""
import argparse
import numpy as np
from PIL import Image

PALETTES = {
    # (low, mid, high) RGB ramp for the surface material
    "moon": ((58, 54, 50), (122, 114, 104), (176, 168, 158)),
    "mars": ((92, 46, 30), (150, 82, 52), (196, 120, 84)),
}


def value_noise(h, w, scale, rng):
    """Cheap smooth value noise in [0,1] via upsampled random grid."""
    gh, gw = max(2, h // scale), max(2, w // scale)
    grid = rng.random((gh, gw))
    img = Image.fromarray((grid * 255).astype(np.uint8)).resize((w, h), Image.BICUBIC)
    a = np.asarray(img, dtype=np.float32) / 255.0
    return (a - a.min()) / (np.ptp(a) + 1e-6)


def ramp(n, palette):
    lo, mid, hi = (np.array(c, np.float32) for c in palette)
    out = np.where(n[..., None] < 0.5,
                   lo + (mid - lo) * (n[..., None] * 2),
                   mid + (hi - mid) * ((n[..., None] - 0.5) * 2))
    return out


def regolith(t, palette, rng):
    n = 0.6 * value_noise(t, t, max(2, t // 4), rng) + 0.4 * value_noise(t, t, max(2, t // 12), rng)
    return ramp(n, palette).astype(np.uint8)


def crater_floor(t, palette, rng):
    base = regolith(t, palette, rng).astype(np.float32)
    yy, xx = np.mgrid[0:t, 0:t]
    r = np.hypot(xx - t / 2, yy - t / 2) / (t / 2)
    shade = np.clip(0.55 + 0.45 * r, 0, 1)[..., None]   # darker centre
    return (base * shade).astype(np.uint8)


def crater_wall(t, palette, rng):
    base = regolith(t, palette, rng).astype(np.float32)
    yy = np.mgrid[0:t, 0:t][0] / t
    rim = (1.0 - 0.5 * yy)[..., None]                    # bright top rim, dark base
    return np.clip(base * (0.6 + 0.6 * rim), 0, 255).astype(np.uint8)


TILE_KINDS = [
    ("Regolith", regolith), ("Regolith", regolith), ("Regolith", regolith),
    ("CraterFloor", crater_floor), ("CraterFloor", crater_floor),
    ("CraterWall", crater_wall), ("CraterWall", crater_wall),
    ("Basalt", regolith),
]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default="lunar_tiles.png")
    ap.add_argument("--tile", type=int, default=24)
    ap.add_argument("--cols", type=int, default=8)
    ap.add_argument("--palette", choices=list(PALETTES), default="moon")
    ap.add_argument("--seed", type=int, default=1969)   # Apollo 11
    args = ap.parse_args()

    rng = np.random.default_rng(args.seed)
    pal = PALETTES[args.palette]
    t, cols = args.tile, args.cols
    rows = (len(TILE_KINDS) + cols - 1) // cols
    sheet = Image.new("RGBA", (cols * t, rows * t), (0, 0, 0, 0))

    manifest = []
    for i, (kind, fn) in enumerate(TILE_KINDS):
        tile = Image.fromarray(fn(t, pal, rng)).convert("RGBA")
        cx, cy = (i % cols) * t, (i // cols) * t
        sheet.paste(tile, (cx, cy))
        manifest.append(f"{i}\t{kind}\t({cx},{cy})")

    sheet.save(args.out)
    with open(args.out.rsplit(".", 1)[0] + "_manifest.txt", "w") as f:
        f.write("index\tterrain\tsheet_xy\n" + "\n".join(manifest) + "\n")
    print(f"wrote {args.out}  ({cols}x{rows} tiles @ {t}px, palette={args.palette})")
    print("manifest ->", args.out.rsplit(".", 1)[0] + "_manifest.txt")


if __name__ == "__main__":
    main()
