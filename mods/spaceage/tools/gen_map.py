#!/usr/bin/env python3
"""Procedural lunar crater-field MAP layout generator.

Emits a terrain-type grid: regolith plains stamped with impassable crater rings,
scattered ice deposits (the harvestable resource), and flat "buildable" pads for
domes. It writes:

  * <name>.grid.txt    — human-readable terrain grid (one char per cell)
  * <name>.layout.json — machine-readable {terrain, resources, spawns} for import

It intentionally does NOT write the binary .oramap directly: that container (a zip
of map.yaml + a versioned tile/resource binary) is engine-version specific and is
safest to produce with `OpenRA.Utility <mod> --import-map` or the in-game editor.
Feed this layout into a small importer, or hand-place using it as a blueprint.

Usage:
    python3 gen_map.py --w 96 --h 96 --craters 40 --ice 10 --spawns 4 --name luna01
"""
import argparse
import json
import numpy as np

# terrain codes
REGOLITH, CRATER_FLOOR, CRATER_WALL, ICE, PAD = ".", "_", "#", "*", "="
CHAR_TO_TYPE = {
    REGOLITH: "Regolith", CRATER_FLOOR: "CraterFloor", CRATER_WALL: "CraterWall",
    ICE: "IceDeposit", PAD: "Regolith",
}


def stamp_crater(grid, cy, cx, r):
    h, w = grid.shape
    yy, xx = np.mgrid[0:h, 0:w]
    d = np.hypot(xx - cx, yy - cy)
    grid[(d <= r) & (d >= r - 1.5)] = CRATER_WALL     # rim ring (impassable)
    grid[d < r - 1.5] = CRATER_FLOOR                  # depressed floor


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--w", type=int, default=96)
    ap.add_argument("--h", type=int, default=96)
    ap.add_argument("--craters", type=int, default=40)
    ap.add_argument("--ice", type=int, default=10)
    ap.add_argument("--spawns", type=int, default=4)
    ap.add_argument("--name", default="luna01")
    ap.add_argument("--seed", type=int, default=1969)
    args = ap.parse_args()

    rng = np.random.default_rng(args.seed)
    grid = np.full((args.h, args.w), REGOLITH, dtype="<U1")

    for _ in range(args.craters):
        r = int(rng.integers(3, 9))
        cy = int(rng.integers(r + 1, args.h - r - 1))
        cx = int(rng.integers(r + 1, args.w - r - 1))
        stamp_crater(grid, cy, cx, r)

    # ice deposits on open regolith only
    resources = []
    placed = 0
    while placed < args.ice:
        y, x = int(rng.integers(0, args.h)), int(rng.integers(0, args.w))
        if grid[y, x] == REGOLITH:
            # a small blob
            for dy in range(-1, 2):
                for dx in range(-1, 2):
                    yy, xx = y + dy, x + dx
                    if 0 <= yy < args.h and 0 <= xx < args.w and grid[yy, xx] == REGOLITH:
                        grid[yy, xx] = ICE
                        resources.append([int(xx), int(yy)])
            placed += 1

    # spawn pads spread around the edges on flat regolith
    spawns = []
    margin = 6
    corners = [(margin, margin), (args.h - margin, args.w - margin),
               (margin, args.w - margin), (args.h - margin, margin)]
    for i in range(min(args.spawns, len(corners))):
        cy, cx = corners[i]
        for dy in range(-2, 3):
            for dx in range(-2, 3):
                yy, xx = cy + dy, cx + dx
                if 0 <= yy < args.h and 0 <= xx < args.w:
                    grid[yy, xx] = PAD
        spawns.append([int(cx), int(cy)])

    grid_txt = "\n".join("".join(row) for row in grid)
    with open(f"{args.name}.grid.txt", "w") as f:
        f.write(grid_txt + "\n")

    layout = {
        "name": args.name, "width": args.w, "height": args.h,
        "legend": CHAR_TO_TYPE,
        "spawns": spawns, "ice": resources,
    }
    with open(f"{args.name}.layout.json", "w") as f:
        json.dump(layout, f, indent=1)

    # quick stats
    unique, counts = np.unique(grid, return_counts=True)
    stats = {CHAR_TO_TYPE.get(u, u): int(c) for u, c in zip(unique, counts)}
    print(f"wrote {args.name}.grid.txt and {args.name}.layout.json  ({args.w}x{args.h})")
    print("terrain cell counts:", stats)
    print(f"spawns={len(spawns)}  ice cells={len(resources)}")


if __name__ == "__main__":
    main()
