#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.Common.GeoMapGenerator
{
	/// <summary>
	/// Flat tile grid indexed column-major: [i * Height + j].
	/// </summary>
	public sealed class TileGrid
	{
		public readonly int Width;
		public readonly int Height;
		public readonly ushort[] TileTypes;
		public readonly byte[] TileVariants;

		public TileGrid(int width, int height)
		{
			Width = width;
			Height = height;
			TileTypes = new ushort[width * height];
			TileVariants = new byte[width * height];
		}

		public ushort GetType(int i, int j) => TileTypes[i * Height + j];
		public void SetType(int i, int j, ushort value) => TileTypes[i * Height + j] = value;
		public byte GetVariant(int i, int j) => TileVariants[i * Height + j];
		public void SetVariant(int i, int j, byte value) => TileVariants[i * Height + j] = value;

		public void Fill(ushort tileType)
		{
			Array.Fill(TileTypes, tileType);
		}
	}

	/// <summary>
	/// Result of rasterization containing tile grid and placed actor definitions.
	/// </summary>
	public sealed class RasterizationResult
	{
		public TileGrid Grid;
		public List<ActorPlacement> Actors;
		public Dictionary<string, int> Stats;
	}

	public readonly struct ActorPlacement
	{
		public readonly string Name;
		public readonly string ActorType;
		public readonly int X;
		public readonly int Y;

		public ActorPlacement(string name, string actorType, int x, int y)
		{
			Name = name;
			ActorType = actorType;
			X = x;
			Y = y;
		}
	}

	/// <summary>
	/// Core rasterization engine. Ports Python overlay_osm_to_tiles() and all helper functions.
	/// </summary>
	public static class TileRasterizer
	{
		// Tile template IDs from RA TEMPERATE tileset
		public const ushort Clear = 255;
		public const ushort Water = 1;
		public const ushort Road = 227;
		public const ushort Road2 = 228;
		public const ushort Beach = 6;
		public const byte BeachVariant = 4;

		// River templates
		const ushort RiverVertCenter = 117;    // rv06.tem, 3x2
		const ushort RiverHorizTop = 121;      // rv10.tem, 2x2
		const ushort RiverHorizTopAlt = 122;   // rv11.tem, 2x2

		// Road junction templates (3x3)
		const ushort RoadJunction1 = 206;
		const ushort RoadJunction2 = 207;

		const double RiverStampSkipWaterFrac = 0.45;

		static readonly Dictionary<ushort, (int W, int H)> TemplateSizes = new()
		{
			{ RiverVertCenter, (3, 2) },
			{ RiverHorizTop, (2, 2) },
			{ RiverHorizTopAlt, (2, 2) },
			{ RoadJunction1, (3, 3) },
			{ RoadJunction2, (3, 3) },
		};

		static readonly HashSet<ushort> RiverTemplateIds = new()
		{
			112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 229, 230,
		};

		static readonly HashSet<ushort> RoadBaseIds = new() { Road, Road2 };
		static readonly HashSet<ushort> RoadMultiIds = new() { RoadJunction1, RoadJunction2 };
		static readonly HashSet<ushort> RoadAllIds = new(RoadBaseIds.Concat(RoadMultiIds));

		// Road width by highway type (meters)
		static readonly Dictionary<string, double> RoadWidthByType = new(StringComparer.OrdinalIgnoreCase)
		{
			["motorway"] = 16.0, ["trunk"] = 14.0, ["primary"] = 12.0,
			["secondary"] = 10.0, ["tertiary"] = 9.0, ["unclassified"] = 8.0,
			["residential"] = 8.0, ["living_street"] = 6.0, ["service"] = 6.0,
			["track"] = 5.0, ["pedestrian"] = 6.0, ["footway"] = 4.0,
			["path"] = 4.0, ["cycleway"] = 4.0, ["steps"] = 3.0, ["bus_guideway"] = 6.0,
		};

		// Water width by waterway type (meters)
		static readonly Dictionary<string, double> WaterWidthByType = new(StringComparer.OrdinalIgnoreCase)
		{
			["river"] = 12.0, ["canal"] = 8.0, ["stream"] = 6.0,
		};

		// Building actor footprints
		static readonly Dictionary<(int W, int H), string[]> BuildingActors = new()
		{
			[(1, 1)] = new[] { "LHUS" },
			[(1, 2)] = new[] { "RUSHOUSE" },
			[(2, 1)] = new[] { "V22", "V26", "V30", "V31", "V32", "V33" },
			[(2, 2)] = new[] { "V20", "V21", "V24", "V25" },
		};

		// Tree actor types
		static readonly string[] TreeTypes = { "t01", "t02", "t03", "t05", "t06", "t07", "t08", "t10", "t11", "t12", "t13" };

		/// <summary>
		/// Main entry point. Rasterizes all OSM features onto a tile grid and places actors.
		/// </summary>
		public static RasterizationResult Rasterize(OsmData osm, MapBounds bounds,
			double mpc, int cells, GeoMapOptions options, Action<string, int> onProgress = null)
		{
			var width = cells;
			var height = cells;
			var grid = new TileGrid(width, height);
			var actors = new List<ActorPlacement>();
			var stats = new Dictionary<string, int>
			{
				["osm_nodes"] = osm.NodesById.Count,
				["water_cells"] = 0,
				["road_cells"] = 0,
				["veg_actors"] = 0,
				["building_actors"] = 0,
			};

			var rng = new Random(42); // deterministic seed for reproducibility
			var occupiedCells = new HashSet<(int, int)>();

			// --- Coastline detection and base grid initialization ---
			onProgress?.Invoke("Processing coastlines...", 60);
			var coastlineRings = new List<List<(double X, double Y)>>();
			if (options.IncludeCoastline && options.IncludeWater)
			{
				coastlineRings = AssembleCoastlineRings(osm, bounds, mpc, cells);

				if (coastlineRings.Count > 0)
				{
					if (options.InvertCoastline)
					{
						grid.Fill(Clear);
						foreach (var ring in coastlineRings)
							stats["water_cells"] += FillPolygon(grid, ring, Water);
					}
					else
					{
						grid.Fill(Water);
						foreach (var ring in coastlineRings)
							FillPolygon(grid, ring, Clear);
					}
				}
				else
					grid.Fill(Clear);
			}
			else
				grid.Fill(Clear);

			// River sample points for smoothing
			var riverSamples = new HashSet<(int I, int J, int Orient)>();

			// --- Water areas and waterways ---
			if (options.IncludeWater)
			{
				onProgress?.Invoke("Rasterizing water...", 65);
				RasterizeWaterAreas(osm, grid, bounds, mpc, stats);
				RasterizeWaterways(osm, grid, bounds, mpc, options.WaterwayWidthMeters, stats, riverSamples, width, height);
				ProcessWaterRelations(osm, grid, bounds, mpc, stats);
				ApplyRiverSmoothing(grid, riverSamples, width, height, stats);
				ApplyShorelineSmoothing(grid, width, height, stats);
			}

			// --- Roads ---
			if (options.IncludeRoads)
			{
				onProgress?.Invoke("Rasterizing roads...", 75);
				RasterizeRoads(osm, grid, bounds, mpc, options.RoadWidthMeters, stats, width, height);
				ApplyRoadJunctions(grid, width, height, stats);
			}

			// --- Precompute forest and built-up cells ---
			var forestCells = new HashSet<(int, int)>();
			var builtupCells = new HashSet<(int, int)>();
			ComputeLanduseCells(osm, grid, bounds, mpc, forestCells, builtupCells);

			// Urban patch tiling
			var urbanCells = 0;
			foreach (var (i, j) in builtupCells)
			{
				if (i >= 0 && i < width && j >= 0 && j < height && grid.GetType(i, j) == Clear)
				{
					grid.SetType(i, j, Road);
					grid.SetVariant(i, j, 0);
					urbanCells++;
				}
			}

			if (urbanCells > 0)
				stats["urban_cells"] = urbanCells;

			// Build road cell set for vegetation suppression
			var roadCells = new HashSet<(int, int)>();
			for (var i = 0; i < width; i++)
				for (var j = 0; j < height; j++)
					if (RoadAllIds.Contains(grid.GetType(i, j)))
						roadCells.Add((i, j));

			// --- Buildings ---
			if (options.IncludeBuildings)
			{
				onProgress?.Invoke("Placing buildings...", 80);
				PlaceBuildings(osm, grid, bounds, mpc, options, actors, occupiedCells, stats, rng, width, height);
			}

			// --- Vegetation ---
			if (options.IncludeVegetation)
			{
				onProgress?.Invoke("Placing vegetation...", 85);
				PlaceVegetation(grid, forestCells, builtupCells, roadCells, occupiedCells, options, actors, stats, rng, width, height);
			}

			stats["forest_cells"] = forestCells.Count;
			stats["builtup_cells"] = builtupCells.Count;

			return new RasterizationResult { Grid = grid, Actors = actors, Stats = stats };
		}

		// --- Rasterization primitives ---

		/// <summary>
		/// Fill a polygon on the grid using scanline algorithm. Returns number of cells set.
		/// </summary>
		public static int FillPolygon(TileGrid grid, IReadOnlyList<(double X, double Y)> poly, ushort value)
		{
			if (poly == null || poly.Count < 3)
				return 0;

			var w = grid.Width;
			var h = grid.Height;
			if (w == 0 || h == 0) return 0;

			var ymin = Math.Max(0, (int)poly.Min(p => p.Y));
			var ymax = Math.Min(h - 1, (int)poly.Max(p => p.Y) + 1);
			var n = poly.Count;
			var setCount = 0;

			for (var j = ymin; j <= ymax; j++)
			{
				var cy = j + 0.5;
				var nodeX = new List<double>();
				var k = n - 1;
				for (var idx = 0; idx < n; idx++)
				{
					var yi = poly[idx].Y;
					var yk = poly[k].Y;
					if ((yi > cy) != (yk > cy))
					{
						var ix = poly[idx].X + (cy - yi) / (yk - yi) * (poly[k].X - poly[idx].X);
						nodeX.Add(ix);
					}

					k = idx;
				}

				nodeX.Sort();

				for (var p = 0; p < nodeX.Count - 1; p += 2)
				{
					var iStart = Math.Max(0, (int)Math.Ceiling(nodeX[p] - 0.5));
					var iEnd = Math.Min(w - 1, (int)Math.Floor(nodeX[p + 1] - 0.5));
					for (var i = iStart; i <= iEnd; i++)
					{
						if (grid.GetType(i, j) != value)
						{
							grid.SetType(i, j, value);
							setCount++;
						}
					}
				}
			}

			return setCount;
		}

		/// <summary>
		/// Draw a filled disc of given radius (in cells) on the grid. Returns number of cells set.
		/// </summary>
		public static int DrawDisc(TileGrid grid, double cx, double cy, double r, ushort value)
		{
			var w = grid.Width;
			var h = grid.Height;
			if (r <= 0) return 0;

			var r2 = r * r;
			var xmin = Math.Max(0, (int)(cx - r) - 1);
			var xmax = Math.Min(w - 1, (int)(cx + r) + 1);
			var ymin = Math.Max(0, (int)(cy - r) - 1);
			var ymax = Math.Min(h - 1, (int)(cy + r) + 1);
			var setCount = 0;

			for (var i = xmin; i <= xmax; i++)
			{
				for (var j = ymin; j <= ymax; j++)
				{
					var dx = (i + 0.5) - cx;
					var dy = (j + 0.5) - cy;
					if (dx * dx + dy * dy <= r2 && grid.GetType(i, j) != value)
					{
						grid.SetType(i, j, value);
						setCount++;
					}
				}
			}

			return setCount;
		}

		/// <summary>
		/// Rasterize a thick line by sampling along the segment and drawing discs.
		/// </summary>
		public static int RasterizeLine(TileGrid grid, double x0, double y0, double x1, double y1,
			double radiusCells, ushort value)
		{
			var length = Math.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0));
			if (length == 0)
				return DrawDisc(grid, x0, y0, radiusCells, value);

			var steps = Math.Max(1, (int)(length * 2));
			var count = 0;
			for (var s = 0; s <= steps; s++)
			{
				var t = (double)s / steps;
				var x = x0 + (x1 - x0) * t;
				var y = y0 + (y1 - y0) * t;
				count += DrawDisc(grid, x, y, radiusCells, value);
			}

			return count;
		}

		/// <summary>
		/// Even-odd rule point-in-polygon test.
		/// </summary>
		public static bool PointInPolygon(double px, double py, IReadOnlyList<(double X, double Y)> poly)
		{
			if (poly == null || poly.Count < 3) return false;
			var inside = false;
			var n = poly.Count;
			var (xj, yj) = poly[n - 1];
			for (var i = 0; i < n; i++)
			{
				var (xi, yi) = poly[i];
				if ((yi > py) != (yj > py))
				{
					var slopeX = (xj - xi) * (py - yi) / (yj - yi) + xi;
					if (px < slopeX)
						inside = !inside;
				}

				(xj, yj) = (xi, yi);
			}

			return inside;
		}

		/// <summary>
		/// Douglas-Peucker line simplification.
		/// </summary>
		public static List<(double X, double Y)> SimplifyPolygon(
			IReadOnlyList<(double X, double Y)> poly, double epsilon = 1.0)
		{
			if (poly.Count <= 2)
				return new List<(double, double)>(poly);

			var first = poly[0];
			var last = poly[poly.Count - 1];
			var maxDist = 0.0;
			var maxIdx = 0;
			var dx = last.X - first.X;
			var dy = last.Y - first.Y;
			var lineLenSq = dx * dx + dy * dy;

			for (var i = 1; i < poly.Count - 1; i++)
			{
				var (px, py) = poly[i];
				double dist;
				if (lineLenSq == 0)
				{
					dist = Math.Sqrt((px - first.X) * (px - first.X) + (py - first.Y) * (py - first.Y));
				}
				else
				{
					var t = Math.Max(0.0, Math.Min(1.0,
						((px - first.X) * dx + (py - first.Y) * dy) / lineLenSq));
					var projX = first.X + t * dx;
					var projY = first.Y + t * dy;
					dist = Math.Sqrt((px - projX) * (px - projX) + (py - projY) * (py - projY));
				}

				if (dist > maxDist)
				{
					maxDist = dist;
					maxIdx = i;
				}
			}

			if (maxDist > epsilon)
			{
				var leftSlice = new List<(double, double)>();
				for (var i = 0; i <= maxIdx; i++) leftSlice.Add(poly[i]);
				var rightSlice = new List<(double, double)>();
				for (var i = maxIdx; i < poly.Count; i++) rightSlice.Add(poly[i]);

				var left = SimplifyPolygon(leftSlice, epsilon);
				var right = SimplifyPolygon(rightSlice, epsilon);
				left.RemoveAt(left.Count - 1);
				left.AddRange(right);
				return left;
			}

			return new List<(double, double)> { first, last };
		}

		/// <summary>
		/// Assemble coastline ways into closed land polygon rings.
		/// </summary>
		public static List<List<(double X, double Y)>> AssembleCoastlineRings(
			OsmData osm, MapBounds bounds, double mpc, int cells)
		{
			// Collect coastline ways
			var coastlineWays = new List<OsmWay>();
			foreach (var way in osm.WaysById.Values)
			{
				if (way.Tags.TryGetValue("natural", out var nat) && nat == "coastline")
					coastlineWays.Add(way);
			}

			if (coastlineWays.Count == 0)
				return new List<List<(double, double)>>();

			// Build chains
			var chains = new List<List<long>>();
			foreach (var way in coastlineWays)
			{
				if (way.NodeIds.Length >= 2)
					chains.Add(new List<long>(way.NodeIds));
			}

			// Merge chains sharing endpoints
			var merged = true;
			while (merged)
			{
				merged = false;
				var firstIdx = new Dictionary<long, int>();
				var lastIdx = new Dictionary<long, int>();
				for (var ci = 0; ci < chains.Count; ci++)
				{
					if (chains[ci].Count > 0)
					{
						firstIdx[chains[ci][0]] = ci;
						lastIdx[chains[ci][^1]] = ci;
					}
				}

				for (var ci = 0; ci < chains.Count; ci++)
				{
					if (chains[ci].Count == 0) continue;
					var lastNode = chains[ci][^1];
					if (firstIdx.TryGetValue(lastNode, out var oi) && oi != ci && chains[oi].Count > 0)
					{
						chains[ci].AddRange(chains[oi].Skip(1));
						chains[oi].Clear();
						merged = true;
						break;
					}
				}
			}

			chains.RemoveAll(c => c.Count == 0);

			var rings = new List<List<(double, double)>>();
			foreach (var ch in chains)
			{
				var cellCoords = new List<(double X, double Y)>();
				foreach (var nid in ch)
				{
					if (!osm.NodesById.TryGetValue(nid, out var node)) continue;
					var cell = GeoMath.LatLonToCell(node.Lat, node.Lon, bounds, mpc);
					if (cell.HasValue)
						cellCoords.Add(cell.Value);
				}

				if (cellCoords.Count < 3) continue;

				var isClosed = ch[0] == ch[^1];
				if (!isClosed)
					cellCoords = CloseChainViaBbox(cellCoords, cells, cells);

				if (cellCoords[0] != cellCoords[^1])
					cellCoords.Add(cellCoords[0]);

				// Check orientation: land polygons should be clockwise in screen coords (y-down)
				var signedArea = 0.0;
				for (var i = 0; i < cellCoords.Count - 1; i++)
				{
					var (x0, y0) = cellCoords[i];
					var (x1, y1) = cellCoords[i + 1];
					signedArea += (x1 - x0) * (y1 + y0);
				}

				if (signedArea < 0)
					cellCoords.Reverse();

				var simplified = SimplifyPolygon(cellCoords, 1.0);
				if (simplified.Count >= 3)
					rings.Add(simplified);
			}

			return rings;
		}

		/// <summary>
		/// Close an open coastline chain by walking along the bounding box edges.
		/// </summary>
		static List<(double X, double Y)> CloseChainViaBbox(
			List<(double X, double Y)> chain, int width, int height)
		{
			if (chain.Count < 2) return chain;
			if (chain[0] == chain[^1]) return chain;

			var corners = new[]
			{
				(0.0, 0.0),
				((double)width, 0.0),
				((double)width, (double)height),
				(0.0, (double)height),
			};

			double EdgePosition((double X, double Y) pt)
			{
				var x = Math.Max(0.0, Math.Min(width, pt.X));
				var y = Math.Max(0.0, Math.Min(height, pt.Y));
				var w = (double)width;
				var h = (double)height;
				if (y <= 1.0) return x / w;
				if (x >= w - 1.0) return 1.0 + y / h;
				if (y >= h - 1.0) return 2.0 + (w - x) / w;
				return 3.0 + (h - y) / h;
			}

			var posStart = EdgePosition(chain[^1]);
			var posEnd = EdgePosition(chain[0]);
			var cornerPositions = corners.Select(c => EdgePosition(c)).ToArray();

			List<(double, double)> CollectCornersCw(double pFrom, double pTo)
			{
				var path = new List<(double, double)>();
				for (var i = 0; i < corners.Length; i++)
				{
					var cp = cornerPositions[i];
					if (pFrom <= pTo)
					{
						if (cp > pFrom && cp < pTo)
							path.Add(corners[i]);
					}
					else
					{
						if (cp > pFrom || cp < pTo)
							path.Add(corners[i]);
					}
				}

				path.Sort((a, b) => ((EdgePosition(a) - pFrom + 4.0) % 4.0)
					.CompareTo((EdgePosition(b) - pFrom + 4.0) % 4.0));
				return path;
			}

			double PolyAbsArea(List<(double X, double Y)> poly)
			{
				var a = 0.0;
				for (var i = 0; i < poly.Count - 1; i++)
					a += (poly[i + 1].X - poly[i].X) * (poly[i + 1].Y + poly[i].Y);
				return Math.Abs(a);
			}

			var cwCorners = CollectCornersCw(posStart, posEnd);
			var polyCw = new List<(double, double)>(chain);
			polyCw.AddRange(cwCorners);
			polyCw.Add(chain[0]);

			var ccwCorners = CollectCornersCw(posEnd, posStart);
			ccwCorners.Reverse();
			var polyCcw = new List<(double, double)>(chain);
			polyCcw.AddRange(ccwCorners);
			polyCcw.Add(chain[0]);

			return PolyAbsArea(polyCw) <= PolyAbsArea(polyCcw) ? polyCw : polyCcw;
		}

		// --- Feature rasterization methods ---

		static List<(double X, double Y)> AssembleWayNodes(OsmWay way, OsmData osm, MapBounds bounds, double mpc)
		{
			var coords = new List<(double X, double Y)>();
			foreach (var nid in way.NodeIds)
			{
				if (!osm.NodesById.TryGetValue(nid, out var node)) continue;
				var cell = GeoMath.LatLonToCell(node.Lat, node.Lon, bounds, mpc);
				if (cell.HasValue)
					coords.Add(cell.Value);
			}

			return coords;
		}

		static void RasterizeWaterAreas(OsmData osm, TileGrid grid, MapBounds bounds, double mpc,
			Dictionary<string, int> stats)
		{
			foreach (var way in osm.WaysById.Values)
			{
				var isWater = (way.Tags.TryGetValue("natural", out var nat) && nat == "water")
					|| (way.Tags.TryGetValue("landuse", out var lu) && lu == "reservoir");
				if (!isWater) continue;

				var ring = AssembleWayNodes(way, osm, bounds, mpc);
				if (ring.Count < 3) continue;
				if (ring[0] != ring[^1]) ring.Add(ring[0]);
				stats["water_cells"] += FillPolygon(grid, ring, Water);
			}
		}

		static void RasterizeWaterways(OsmData osm, TileGrid grid, MapBounds bounds, double mpc,
			double defaultWidth, Dictionary<string, int> stats,
			HashSet<(int I, int J, int Orient)> riverSamples, int width, int height)
		{
			foreach (var way in osm.WaysById.Values)
			{
				if (!way.Tags.TryGetValue("waterway", out var wtype)) continue;
				wtype = wtype.ToLowerInvariant();

				double widthM;
				if (way.Tags.TryGetValue("width", out var wtag) && TryParseWidth(wtag, out var wo))
					widthM = wo;
				else if (!WaterWidthByType.TryGetValue(wtype, out widthM))
					widthM = defaultWidth;

				var rCells = Math.Max(0.5, widthM / Math.Max(0.01, mpc) / 2.0);
				var coords = AssembleWayNodes(way, osm, bounds, mpc);

				for (var idx = 0; idx < coords.Count - 1; idx++)
				{
					var (ax, ay) = coords[idx];
					var (bx, by) = coords[idx + 1];
					if (!InBounds(ax, ay, width, height) && !InBounds(bx, by, width, height))
						continue;

					stats["water_cells"] += RasterizeLine(grid, ax, ay, bx, by, rCells, Water);

					// Collect river samples for smoothing
					var dx = bx - ax;
					var dy = by - ay;
					var orient = Math.Abs(dy) >= Math.Abs(dx) ? 0 : 1;
					var segLen = Math.Sqrt(dx * dx + dy * dy);
					var step = Math.Max(1.0, rCells);
					var steps = Math.Max(1, (int)(segLen / step));
					for (var s = 0; s <= steps; s++)
					{
						var t = (double)s / Math.Max(1, steps);
						var x = ax + dx * t;
						var y = ay + dy * t;
						var ii = (int)x;
						var jj = (int)y;
						if (ii >= 0 && ii < width && jj >= 0 && jj < height)
							riverSamples.Add((ii, jj, orient));
					}
				}
			}
		}

		static void ProcessWaterRelations(OsmData osm, TileGrid grid, MapBounds bounds, double mpc,
			Dictionary<string, int> stats)
		{
			foreach (var rel in osm.Relations)
			{
				if (!rel.Tags.TryGetValue("natural", out var nat) || nat != "water")
					continue;

				foreach (var member in rel.Members)
				{
					if (member.Type != "way") continue;
					if (!osm.WaysById.TryGetValue(member.Ref, out var way)) continue;

					var coords = AssembleWayNodes(way, osm, bounds, mpc);
					if (coords.Count < 3) continue;
					if (coords[0] != coords[^1]) coords.Add(coords[0]);

					var tileValue = member.Role == "inner" ? Clear : Water;
					stats["water_cells"] += FillPolygon(grid, coords, tileValue);
				}
			}
		}

		static void ApplyRiverSmoothing(TileGrid grid, HashSet<(int I, int J, int Orient)> riverSamples,
			int width, int height, Dictionary<string, int> stats)
		{
			if (riverSamples.Count == 0) return;
			var stampCount = 0;
			foreach (var (ii, jj, orient) in riverSamples)
			{
				if (grid.GetType(ii, jj) != Water) continue;
				if (LocalWaterFraction(grid, ii, jj, 4, width, height) > RiverStampSkipWaterFrac) continue;

				if (orient == 0)
					stampCount += StampTemplate(grid, ii - 1, jj, RiverVertCenter);
				else
				{
					var templ = ((ii + jj) & 1) == 0 ? RiverHorizTop : RiverHorizTopAlt;
					stampCount += StampTemplate(grid, ii, jj, templ);
				}
			}

			if (stampCount > 0)
			{
				stats["river_stamps"] = stampCount;
				stats["river_samples"] = riverSamples.Count;
			}
		}

		static void ApplyShorelineSmoothing(TileGrid grid, int width, int height, Dictionary<string, int> stats)
		{
			var shoreCount = 0;
			for (var i = 0; i < width; i++)
			{
				for (var j = 0; j < height; j++)
				{
					if (grid.GetType(i, j) != Clear) continue;
					var hasWaterNeighbor = false;
					for (var di = -1; di <= 1 && !hasWaterNeighbor; di++)
					{
						for (var dj = -1; dj <= 1 && !hasWaterNeighbor; dj++)
						{
							if (di == 0 && dj == 0) continue;
							var ni = i + di;
							var nj = j + dj;
							if (ni < 0 || ni >= width || nj < 0 || nj >= height) continue;
							var nType = grid.GetType(ni, nj);
							if (nType == Water || RiverTemplateIds.Contains(nType))
								hasWaterNeighbor = true;
						}
					}

					if (hasWaterNeighbor)
					{
						grid.SetType(i, j, Beach);
						grid.SetVariant(i, j, BeachVariant);
						shoreCount++;
					}
				}
			}

			stats["shore_cells"] = shoreCount;
		}

		static void RasterizeRoads(OsmData osm, TileGrid grid, MapBounds bounds, double mpc,
			double defaultWidth, Dictionary<string, int> stats, int width, int height)
		{
			var roadWays = 0;
			var roadSegments = 0;

			foreach (var way in osm.WaysById.Values)
			{
				if (!way.Tags.TryGetValue("highway", out var htype)) continue;
				htype = htype.ToLowerInvariant();
				roadWays++;

				double widthM;
				if (way.Tags.TryGetValue("width", out var wtag) && TryParseWidth(wtag, out var wo))
					widthM = wo;
				else if (!RoadWidthByType.TryGetValue(htype, out widthM))
					widthM = defaultWidth;

				var rCells = Math.Max(0.5, widthM / Math.Max(0.01, mpc) / 2.0);
				var coords = AssembleWayNodes(way, osm, bounds, mpc);
				if (coords.Count >= 2)
					roadSegments += coords.Count - 1;

				for (var idx = 0; idx < coords.Count - 1; idx++)
				{
					var (ax, ay) = coords[idx];
					var (bx, by) = coords[idx + 1];
					if (!InBounds(ax, ay, width, height) && !InBounds(bx, by, width, height))
						continue;
					stats["road_cells"] += RasterizeLine(grid, ax, ay, bx, by, rCells, Road);
				}
			}

			stats["road_ways"] = roadWays;
			stats["road_segments"] = roadSegments;
		}

		static void ApplyRoadJunctions(TileGrid grid, int width, int height, Dictionary<string, int> stats)
		{
			var junctionStamps = 0;
			var stampedAnchors = new HashSet<(int, int)>();

			for (var i = 1; i < width - 1; i++)
			{
				for (var j = 1; j < height - 1; j++)
				{
					if (!RoadBaseIds.Contains(grid.GetType(i, j))) continue;
					var n = RoadBaseIds.Contains(grid.GetType(i, j - 1)) ? 1 : 0;
					var s = RoadBaseIds.Contains(grid.GetType(i, j + 1)) ? 1 : 0;
					var w = RoadBaseIds.Contains(grid.GetType(i - 1, j)) ? 1 : 0;
					var e = RoadBaseIds.Contains(grid.GetType(i + 1, j)) ? 1 : 0;

					if (n + s + w + e < 3) continue;
					var ai = i - 1;
					var aj = j - 1;
					if (ai < 0 || aj < 0 || ai + 2 >= width || aj + 2 >= height) continue;
					if (stampedAnchors.Contains((ai, aj))) continue;

					var templ = ((i + j) & 1) == 0 ? RoadJunction1 : RoadJunction2;
					if (StampTemplate(grid, ai, aj, templ) > 0)
					{
						junctionStamps++;
						stampedAnchors.Add((ai, aj));
					}
				}
			}

			if (junctionStamps > 0)
				stats["road_junction_stamps"] = junctionStamps;
		}

		static void ComputeLanduseCells(OsmData osm, TileGrid grid, MapBounds bounds, double mpc,
			HashSet<(int, int)> forestCells, HashSet<(int, int)> builtupCells)
		{
			var w = grid.Width;
			var h = grid.Height;

			foreach (var way in osm.WaysById.Values)
			{
				var isForest = (way.Tags.TryGetValue("natural", out var nat) && nat == "wood")
					|| (way.Tags.TryGetValue("landuse", out var lu) && lu == "forest")
					|| (way.Tags.TryGetValue("landcover", out var lc) && lc == "trees");

				var isBuiltup = way.Tags.TryGetValue("landuse", out var lu2)
					&& (lu2.Equals("residential", StringComparison.OrdinalIgnoreCase)
						|| lu2.Equals("industrial", StringComparison.OrdinalIgnoreCase)
						|| lu2.Equals("commercial", StringComparison.OrdinalIgnoreCase));

				if (!isForest && !isBuiltup) continue;

				var ring = AssembleWayNodes(way, osm, bounds, mpc);
				if (ring.Count < 3) continue;
				if (ring[0] != ring[^1]) ring.Add(ring[0]);

				var xs = ring.Select(p => p.X);
				var ys = ring.Select(p => p.Y);
				var xmin = Math.Max(0, (int)xs.Min() - 1);
				var xmax = Math.Min(w - 1, (int)xs.Max() + 1);
				var ymin = Math.Max(0, (int)ys.Min() - 1);
				var ymax = Math.Min(h - 1, (int)ys.Max() + 1);

				for (var i = xmin; i <= xmax; i++)
				{
					for (var j = ymin; j <= ymax; j++)
					{
						if (PointInPolygon(i + 0.5, j + 0.5, ring))
						{
							if (isForest)
								forestCells.Add((i, j));
							else
								builtupCells.Add((i, j));
						}
					}
				}
			}
		}

		static void PlaceBuildings(OsmData osm, TileGrid grid, MapBounds bounds, double mpc,
			GeoMapOptions options, List<ActorPlacement> actors, HashSet<(int, int)> occupiedCells,
			Dictionary<string, int> stats, Random rng, int width, int height)
		{
			var placed = 0;
			var maxBuildings = options.MaxBuildings;

			void PlaceFromBbox(IReadOnlyList<(double X, double Y)> pts)
			{
				if (placed >= maxBuildings) return;
				if (rng.NextDouble() > Math.Max(0.0, Math.Min(1.0, options.BuildingDensity))) return;
				if (pts.Count == 0) return;

				var minX = pts.Min(p => p.X);
				var maxX = pts.Max(p => p.X);
				var minY = pts.Min(p => p.Y);
				var maxY = pts.Max(p => p.Y);

				var bboxW = Math.Max(1, (int)Math.Round(maxX - minX));
				var bboxH = Math.Max(1, (int)Math.Round(maxY - minY));
				var wFit = Math.Max(1, Math.Min(2, bboxW));
				var hFit = Math.Max(1, Math.Min(2, bboxH));

				var ai = (int)Math.Round((minX + maxX) / 2.0 - wFit / 2.0);
				var aj = (int)Math.Round((minY + maxY) / 2.0 - hFit / 2.0);
				ai = Math.Max(0, Math.Min(width - wFit, ai));
				aj = Math.Max(0, Math.Min(height - hFit, aj));

				var localR = options.BuildingSearchRadius;

				// Try primary size, then fallbacks
				var dimsList = new List<(int, int)> { (wFit, hFit) };
				if ((wFit, hFit) == (2, 2)) dimsList.AddRange(new[] { (2, 1), (1, 2), (1, 1) });
				else if (wFit == 2 || hFit == 2) dimsList.Add((1, 1));

				foreach (var (wf, hf) in dimsList)
				{
					if (!BuildingActors.TryGetValue((wf, hf), out var actorChoices)) continue;
					var placedHere = false;
					for (var dj = -localR; dj <= localR && !placedHere; dj++)
					{
						for (var di = -localR; di <= localR && !placedHere; di++)
						{
							var ci = ai + di;
							var cj = aj + dj;
							if (!CanPlace(grid, ci, cj, wf, hf, occupiedCells, width, height)) continue;

							var actorType = actorChoices[rng.Next(actorChoices.Length)];
							actors.Add(new ActorPlacement($"Bld{placed}", actorType, ci, cj));
							for (var ddx = 0; ddx < wf; ddx++)
								for (var ddy = 0; ddy < hf; ddy++)
									occupiedCells.Add((ci + ddx, cj + ddy));
							placed++;
							placedHere = true;
						}
					}

					if (placedHere || placed >= maxBuildings) break;
				}
			}

			// Pass 1: building ways
			foreach (var way in osm.WaysById.Values)
			{
				if (placed >= maxBuildings) break;
				if (!way.Tags.ContainsKey("building")) continue;
				var ring = AssembleWayNodes(way, osm, bounds, mpc);
				if (ring.Count >= 3)
					PlaceFromBbox(ring);
			}

			// Pass 2: building relations
			foreach (var rel in osm.Relations)
			{
				if (placed >= maxBuildings) break;
				if (!rel.Tags.ContainsKey("building")) continue;
				var allPts = new List<(double X, double Y)>();
				foreach (var m in rel.Members)
				{
					if (m.Type != "way" || (m.Role != "outer" && m.Role != "outline" && m.Role != "")) continue;
					if (!osm.WaysById.TryGetValue(m.Ref, out var way)) continue;
					allPts.AddRange(AssembleWayNodes(way, osm, bounds, mpc));
				}

				if (allPts.Count > 0) PlaceFromBbox(allPts);
			}

			stats["building_actors"] = placed;
		}

		static void PlaceVegetation(TileGrid grid, HashSet<(int, int)> forestCells,
			HashSet<(int, int)> builtupCells, HashSet<(int, int)> roadCells,
			HashSet<(int, int)> occupiedCells, GeoMapOptions options,
			List<ActorPlacement> actors, Dictionary<string, int> stats, Random rng,
			int width, int height)
		{
			var target = options.MaxVegActors;
			var baseProb = Math.Max(0.0, Math.Min(1.0, options.VegDensity));
			var spacing = Math.Max(0, options.VegMinSpacing);
			var roadR = Math.Max(0, options.SuppressVegNearRoads);
			var bldR = Math.Max(0, options.SuppressVegNearBuildings);
			var placed = 0;

			// Patch-based local density boost
			var ps = Math.Max(1, options.VegPatchSize);
			var patchCounts = new Dictionary<(int, int), int>();
			var patchTotals = new Dictionary<(int, int), int>();

			foreach (var (i, j) in forestCells)
			{
				var key = (i / ps, j / ps);
				patchCounts[key] = patchCounts.GetValueOrDefault(key) + 1;
			}

			var maxPi = (width + ps - 1) / ps;
			var maxPj = (height + ps - 1) / ps;
			for (var pi = 0; pi < maxPi; pi++)
				for (var pj = 0; pj < maxPj; pj++)
					patchTotals[(pi, pj)] = Math.Max(1, Math.Min(ps, width - pi * ps) * Math.Min(ps, height - pj * ps));

			var densities = patchCounts.Select(kv =>
				(double)kv.Value / patchTotals.GetValueOrDefault(kv.Key, ps * ps)).OrderBy(d => d).ToList();
			var median = densities.Count > 0 ? densities[densities.Count / 2] : 0.0;
			var highPatches = new HashSet<(int, int)>(
				patchCounts.Where(kv =>
					(double)kv.Value / patchTotals.GetValueOrDefault(kv.Key, ps * ps) >= median)
				.Select(kv => kv.Key));

			var vegOccupied = new HashSet<(int, int)>();
			foreach (var (i, j) in forestCells)
			{
				if (placed >= target) break;
				if (grid.GetType(i, j) == Water) continue;
				var tileType = grid.GetType(i, j);
				if (RiverTemplateIds.Contains(tileType) || tileType == Beach || RoadAllIds.Contains(tileType)) continue;
				if (builtupCells.Contains((i, j))) continue;

				// Patch boost
				var prob = baseProb * (highPatches.Contains((i / ps, j / ps)) ? options.VegPatchBoost : 1.0);
				if (prob > 1.0) prob = 1.0;
				if (rng.NextDouble() > prob) continue;

				// Suppress near roads
				if (roadR > 0 && IsNearSet(i, j, roadR, roadCells)) continue;

				// Suppress near buildings
				if (bldR > 0 && IsNearSet(i, j, bldR, occupiedCells)) continue;

				// Spacing constraint
				if (spacing > 0 && IsNearSet(i, j, spacing, vegOccupied)) continue;

				var treeName = TreeTypes[rng.Next(TreeTypes.Length)];
				actors.Add(new ActorPlacement($"Tree{placed}", treeName, i, j));
				vegOccupied.Add((i, j));
				placed++;
			}

			stats["veg_actors"] = placed;
		}

		// --- Helper methods ---

		static int StampTemplate(TileGrid grid, int i0, int j0, ushort templateId)
		{
			if (!TemplateSizes.TryGetValue(templateId, out var size)) return 0;
			var (tw, th) = size;
			var written = 0;
			for (var dy = 0; dy < th; dy++)
			{
				for (var dx = 0; dx < tw; dx++)
				{
					var i = i0 + dx;
					var j = j0 + dy;
					if (i >= 0 && i < grid.Width && j >= 0 && j < grid.Height)
					{
						grid.SetType(i, j, templateId);
						grid.SetVariant(i, j, (byte)(dy * tw + dx));
						written++;
					}
				}
			}

			return written;
		}

		static double LocalWaterFraction(TileGrid grid, int ci, int cj, int radius, int width, int height)
		{
			var total = 0;
			var water = 0;
			for (var dj = -radius; dj <= radius; dj++)
			{
				var j = cj + dj;
				if (j < 0 || j >= height) continue;
				for (var di = -radius; di <= radius; di++)
				{
					var i = ci + di;
					if (i < 0 || i >= width) continue;
					total++;
					if (grid.GetType(i, j) == Water)
						water++;
				}
			}

			return total == 0 ? 0.0 : (double)water / total;
		}

		static bool CanPlace(TileGrid grid, int i0, int j0, int w, int h,
			HashSet<(int, int)> occupied, int gridW, int gridH)
		{
			if (i0 < 0 || j0 < 0 || i0 + w - 1 >= gridW || j0 + h - 1 >= gridH) return false;
			for (var dx = 0; dx < w; dx++)
			{
				for (var dy = 0; dy < h; dy++)
				{
					var ii = i0 + dx;
					var jj = j0 + dy;
					var t = grid.GetType(ii, jj);
					if (t == Water || RiverTemplateIds.Contains(t) || t == Beach) return false;
					if (occupied.Contains((ii, jj))) return false;
				}
			}

			return true;
		}

		static bool IsNearSet(int i, int j, int radius, HashSet<(int, int)> set)
		{
			for (var di = -radius; di <= radius; di++)
				for (var dj = -radius; dj <= radius; dj++)
					if (set.Contains((i + di, j + dj)))
						return true;
			return false;
		}

		static bool InBounds(double x, double y, int width, int height)
		{
			return x >= -1 && y >= -1 && x <= width + 1 && y <= height + 1;
		}

		static bool TryParseWidth(string tag, out double value)
		{
			value = 0;
			if (string.IsNullOrWhiteSpace(tag)) return false;
			var numStr = new string(tag.Where(c => char.IsDigit(c) || c == '.' || c == '-').ToArray());
			return double.TryParse(numStr, System.Globalization.NumberStyles.Float,
				System.Globalization.CultureInfo.InvariantCulture, out value);
		}
	}
}
