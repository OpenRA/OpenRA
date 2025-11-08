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
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Support;

namespace OpenRA.Mods.Common.MapGenerator
{
	/// <summary>Combines ramp tiles to fit a target height map.</summary>
	public sealed class RampTiler
	{
		static readonly ImmutableArray<CVec> CellsAroundCorner = [
			new(0, 0), new(-1, 0), new(0, -1), new(-1, -1)
		];
		static readonly ImmutableArray<int2> CornersAroundCell = [
			new(0, 0), new(1, 0), new(0, 1), new(1, 1)
		];

		public enum AdjustmentMode
		{
			/// <summary>Heights will only increase if absolutely necessary.</summary>
			Minimal,

			/// <summary>Heights will be a rounded down median of minimal and maximal.</summary>
			LowerMiddle,

			/// <summary>Heights will be a rounded up median of minimal and maximal.</summary>
			UpperMiddle,

			/// <summary>Heights will only decrease if absolutely necessary.</summary>
			Maximal,
		}

		/// <summary>Height targets and constraints for tiling a map with ramps.</summary>
		public sealed class HeightMap
		{
			/// <summary>Mapping from Matrix to CellLayer space.</summary>
			public readonly Rectangle CellBounds;

			/// <summary>Ideal cell corner heights.</summary>
			public readonly Matrix<byte> Target;

			/// <summary>Minimum permitted cell corner heights.</summary>
			public readonly Matrix<byte> LowerBound;

			/// <summary>Maximum permitted cell corner heights.</summary>
			public readonly Matrix<byte> UpperBound;

			/// <summary>Whether cell corner heights can deviate from Target when constraining or setting heights.</summary>
			public readonly Matrix<bool> Adjustable;

			/// <summary>Mask for cells to tile.</summary>
			public readonly CellLayer<bool> Tileable;

			public HeightMap(Map map)
			{
				CellBounds = CellLayerUtils.CellBounds(map);
				var size = CellBounds.Size.ToInt2() + new int2(1, 1);
				Target = new Matrix<byte>(size);
				LowerBound = new Matrix<byte>(size).Fill(byte.MinValue);
				UpperBound = new Matrix<byte>(size).Fill(byte.MaxValue);
				Adjustable = new Matrix<bool>(size).Fill(true);
				Tileable = new CellLayer<bool>(map);
				Tileable.Clear(true);

				for (var y = 0; y < size.Y; y++)
				{
					for (var x = 0; x < size.X; x++)
					{
						if (!ContainsCorner(new int2(x, y)))
						{
							LowerBound[x, y] = byte.MaxValue;
							UpperBound[x, y] = byte.MinValue;
							Adjustable[x, y] = false;
						}
					}
				}
			}

			/// <summary>Helper to convert from CellLayer to Matrix space.</summary>
			public int2 CPosToXy(CPos cpos)
			{
				return new int2(cpos.X, cpos.Y) - CellBounds.TopLeft;
			}

			/// <summary>Helper to convert from Matrix to CellLayer space.</summary>
			public CPos XyToCPos(int2 xy)
			{
				xy += CellBounds.TopLeft;
				return new CPos(xy.X, xy.Y);
			}

			/// <summary>Return true iff a map cell has a full 4 adjacent cells.</summary>
			public bool IsInternalCell(CPos cpos)
			{
				return DirectionExts.Spread4CVec
					.Select(cvec => cpos + cvec)
					.All(Tileable.Contains);
			}

			/// <summary>Return true iff a cell corner has a full 4 adjacent corners.</summary>
			public bool IsInternalCorner(int2 xy)
			{
				var cpos = XyToCPos(xy);
				return CellsAroundCorner
					.Select(cvec => cpos + cvec)
					.Count(Tileable.Contains) >= 3;
			}

			/// <summary>Return true if a corner touches any cell within the map.</summary>
			public bool ContainsCorner(int2 xy)
			{
				var cpos = XyToCPos(xy);
				return CellsAroundCorner
					.Select(cvec => cpos + cvec)
					.Any(Tileable.Contains);
			}

			public void MarkUntileable(CellLayer<bool> mask)
			{
				foreach (var cpos in mask.CellRegion)
					if (mask[cpos])
						MarkUntileable(cpos);
			}

			public void MarkUntileable(IEnumerable<CPos> cells)
			{
				foreach (var cpos in cells)
					MarkUntileable(cpos);
			}

			/// <summary>
			/// Updates the corner heights of a given cell (each cell has 4 corners) according to what
			/// is currently in the map, and do not include the cell in the ramp tiling result.
			/// </summary>
			public void MarkUntileable(CPos cpos)
			{
				if (!Tileable.Contains(cpos))
					return;

				Tileable[cpos] = false;
				var xy = CPosToXy(cpos);

				foreach (var offset in CornersAroundCell)
				{
					var corner = xy + offset;
					Target[corner] = 0;
					LowerBound[corner] = byte.MaxValue;
					UpperBound[corner] = byte.MinValue;
					Adjustable[corner] = false;
				}
			}

			IEnumerable<(int2 XY, (byte Height, bool First) Prop)> FillSeeds(Matrix<byte> heights)
			{
				for (var y = 0; y < Adjustable.Size.Y; y++)
					for (var x = 0; x < Adjustable.Size.X; x++)
						if (Adjustable[x, y])
							yield return (new int2(x, y), (heights[x, y], true));
			}

			// Given an input matrix of cell corner heights, finds the output height
			// matrix where:
			// - Each output[xy] <= input[xy]
			// - Each output[xy] is no more than 1 height step away from its
			//   (at most) 4 adjacent neighbors.
			// - Each output[xy] is as high as it can possibly be otherwise.
			//
			// A 1D representation of this looks like:
			//   4  ..x.....    ........
			//   3  ....x...    ........
			//   2  .x....xx -> .xx....x
			//   1  x..x....    x..xx.x.
			//   0  .....x..    .....x..
			Matrix<byte> GetLowerHull(Matrix<byte> matrix)
			{
				(byte Lower, bool First)? Fill(int2 xy, (byte Lower, bool First) prop)
				{
					if (!prop.First && (!Adjustable[xy] || prop.Lower >= matrix[xy]))
						return null;

					matrix[xy] = prop.Lower;
					if (prop.Lower == byte.MaxValue)
						return null;

					return ((byte)(prop.Lower + 1), false);
				}

				MatrixUtils.FloodFill(
					matrix.Size,
					FillSeeds(matrix).OrderBy(s => s.Prop.Height),
					Fill,
					DirectionExts.Spread4);
				return matrix;
			}

			// Like GetLowerHull, but the other way around. :)
			Matrix<byte> GetUpperHull(Matrix<byte> matrix)
			{
				(byte Upper, bool First)? Fill(int2 xy, (byte Upper, bool First) prop)
				{
					if (!prop.First && (!Adjustable[xy] || prop.Upper <= matrix[xy]))
						return null;

					matrix[xy] = prop.Upper;
					if (prop.Upper == byte.MinValue)
						return null;

					return ((byte)(prop.Upper - 1), false);
				}

				MatrixUtils.FloodFill(
					matrix.Size,
					FillSeeds(matrix).OrderByDescending(s => s.Prop.Height),
					Fill,
					DirectionExts.Spread4);
				return matrix;
			}

			/// <summary>
			/// Attempt to constrain the Target heights such that all adjustable corners are
			/// no more than 1 height step away from any adjacent corner,
			/// and are within LowerBound and UpperBound.
			/// </summary>
			/// <param name="mode">How to set the height of targets than need to be adjusted.</param>
			/// <returns>
			/// Whether the constraints could be satisfied (and the target heights were updated).
			/// </returns>
			public bool Constrain(AdjustmentMode mode)
			{
				var forcedMaximum = GetLowerHull(UpperBound.Clone());
				var forcedMinimum = GetUpperHull(LowerBound.Clone());
				var constrained = Target.Clone();

				for (var y = 0; y < Target.Size.Y; y++)
				{
					for (var x = 0; x < Target.Size.X; x++)
					{
						if (!Adjustable[x, y])
							continue;

						if (forcedMinimum[x, y] > forcedMaximum[x, y])
							return false;
						else if (constrained[x, y] < forcedMinimum[x, y])
							constrained[x, y] = forcedMinimum[x, y];
						else if (constrained[x, y] > forcedMaximum[x, y])
							constrained[x, y] = forcedMaximum[x, y];
					}
				}

				switch (mode)
				{
					case AdjustmentMode.Minimal:
						constrained = GetLowerHull(constrained);
						break;
					case AdjustmentMode.LowerMiddle:
						constrained = Matrix<byte>.Zip(
							GetLowerHull(constrained.Clone()),
							GetUpperHull(constrained),
							(a, b) => (byte)((a + b) / 2));
						break;
					case AdjustmentMode.UpperMiddle:
						constrained = Matrix<byte>.Zip(
							GetLowerHull(constrained.Clone()),
							GetUpperHull(constrained),
							(a, b) => (byte)((a + b + 1) / 2));
						break;
					case AdjustmentMode.Maximal:
						constrained = GetUpperHull(constrained);
						break;
					default:
						throw new ArgumentException("invalid fitting mode");
				}

				constrained.CopyTo(Target);

				return true;
			}

			/// <summary>
			/// Uniformally adjust the corner heights of masked cells.
			/// </summary>
			/// <param name="adjustment">Height adjustment.</param>
			/// <param name="mask">Cells to apply height change to.</param>
			public void AdjustCellHeights(int adjustment, CellLayer<bool> mask)
			{
				var matrixMask = MatrixUtils.KernelAggregate(
					CellLayerUtils.ToMatrix(mask, false),
					new Matrix<bool>(Target.Size),
					new int2(2, 2),
					new int2(1, 1),
					submatrix => submatrix.Data.Any(v => v));
				for (var y = 0; y < Target.Size.Y; y++)
					for (var x = 0; x < Target.Size.X; x++)
						if (matrixMask[x, y])
							Target[x, y] = (byte)Math.Clamp(Target[x, y] + adjustment, byte.MinValue, byte.MaxValue);
			}

			/// <summary>Set the target height of all masked cells' corners to a target value.</summary>
			public void SetCellHeights(byte height, CellLayer<bool> mask)
			{
				foreach (var cpos in mask.CellRegion)
					if (mask[cpos])
						SetCellHeight(height, cpos);
			}

			/// <summary>Set the target height of a cell's corners to a target value.</summary>
			public void SetCellHeight(byte height, CPos cpos)
			{
				var xy = CPosToXy(cpos);
				foreach (var offset in CornersAroundCell)
					Target[xy + offset] = height;
			}

			/// <summary>
			/// Sets specified corners to a given height and expands outward for radius, without
			/// expanding through unadjustable points.
			/// </summary>
			public void SeedHeights(IEnumerable<(int2 Xy, int Radius, byte Height)> corners)
			{
				var expandable = Adjustable.Clone();
				(int Radius, byte Height)? Filler(int2 xy, (int Radius, byte Height) prop)
				{
					if (!expandable[xy] || prop.Radius == 0)
						return null;

					expandable[xy] = false;
					Target[xy] = prop.Height;
					return (prop.Radius - 1, prop.Height);
				}

				MatrixUtils.FloodFill(
					Target.Size,
					corners.Select(corner => (corner.Xy, (corner.Radius, corner.Height))),
					Filler,
					DirectionExts.Spread4);
			}

			/// <summary>Blur Target heights, but only through adjustable space.</summary>
			public void Soften(int distance)
			{
				// Split the softening into multiple steps if needed to avoid numeric limitations.
				// Make sure the last step isn't too small to improve precision.
				while (distance > 12)
				{
					Soften(8);
					distance -= 8;
				}

				var newNumerator = Target.Map(v => (int)v);
				var newDenominator = new Matrix<int>(Target.Size).Fill(1);

				for (var iteration = 0; iteration < distance; iteration++)
				{
					var oldNumerator = newNumerator;
					var oldDenominator = newDenominator;
					newNumerator = new Matrix<int>(Target.Size);
					newDenominator = new Matrix<int>(Target.Size);

					(int Numerator, int Denominator, bool First)? Filler(int2 xy, (int Numerator, int Denominator, bool First) prop)
					{
						if (Adjustable[xy])
						{
							newNumerator[xy] += prop.Numerator;
							newDenominator[xy] += prop.Denominator;
						}

						if (prop.First)
							return (prop.Numerator, prop.Denominator, false);
						else
							return null;
					}

					MatrixUtils.FloodFill(
						Target.Size,
						Adjustable.Enumerate()
							.Where(v => v.Value)
							.Select(v => (v.Xy, (oldNumerator[v.Xy], oldDenominator[v.Xy], true))),
						Filler,
						DirectionExts.Spread4);
				}

				for (var i = 0; i < Target.Data.Length; i++)
					if (Adjustable[i])
						Target[i] = (byte)((newNumerator[i] + newDenominator[i] / 2) / newDenominator[i]);
			}
		}

		record struct RampProperties
		{
			public MultiBrush[] Brushes;
			public byte Tl;
			public byte Tr;
			public byte Br;
			public byte Bl;

			public readonly byte GetCorner(Riser.Connection connection)
			{
				switch (connection)
				{
					case Riser.Connection.LU:
					case Riser.Connection.UL:
						return Tl;
					case Riser.Connection.UR:
					case Riser.Connection.RU:
						return Tr;
					case Riser.Connection.RD:
					case Riser.Connection.DR:
						return Br;
					case Riser.Connection.DL:
					case Riser.Connection.LD:
						return Bl;
				}

				throw new ArgumentException("invalid connection");
			}
		}

		static CVec ConnectionToAdjacentCorner(Riser.Connection connection)
		{
			switch (connection)
			{
				case Riser.Connection.LU:
					return new CVec(-1, 0);
				case Riser.Connection.UL:
					return new CVec(0, -1);
				case Riser.Connection.UR:
					return new CVec(1, -1);
				case Riser.Connection.RU:
					return new CVec(2, 0);
				case Riser.Connection.RD:
					return new CVec(2, 1);
				case Riser.Connection.DR:
					return new CVec(1, 2);
				case Riser.Connection.DL:
					return new CVec(0, 2);
				case Riser.Connection.LD:
					return new CVec(-1, 1);
			}

			throw new ArgumentException("invalid connection");
		}

		readonly Map map;

		// Contains single-tile brushes with zero height offset.
		readonly RampProperties[] rampProperties;

		// Lookup from a binary-concatenation of corner heights (0, 1, or 2) to ramp types.
		// Only contains mappings for which there are brushes.
		readonly Dictionary<int, List<byte>> rampLookup;

		public RampTiler(Map map, IEnumerable<ushort> rampTemplates)
			: this(
				map,
				rampTemplates
					.Select(t => new MultiBrush().WithTemplate(map, t, CVec.Zero))
					.ToList())
		{ }

		public RampTiler(Map map, IReadOnlyList<MultiBrush> rampBrushes)
		{
			this.map = map;
			var heightStep = map.Grid.TileScale / 2;

			var rampsToBrushes = new Dictionary<byte, List<MultiBrush>>();
			foreach (var brush in rampBrushes)
			{
				var heightsAndRamps = brush.GetHeightsAndRamps().ToList();
				if (heightsAndRamps.Count != 1 || heightsAndRamps[0].Height != 0)
					throw new ArgumentException("brushes that are not single-tile are not supported");

				var ramp = heightsAndRamps[0].Ramp;
				if (!rampsToBrushes.ContainsKey(ramp))
					rampsToBrushes.Add(ramp, []);

				rampsToBrushes[ramp].Add(brush);
			}

			rampLookup = [];
			rampProperties = new RampProperties[map.Grid.Ramps.Length];

			for (byte ramp = 0; ramp < rampProperties.Length; ramp++)
			{
				var cellRamp = map.Grid.Ramps[ramp];
				var tl = cellRamp.Corners[0].Z / heightStep;
				var tr = cellRamp.Corners[1].Z / heightStep;
				var br = cellRamp.Corners[2].Z / heightStep;
				var bl = cellRamp.Corners[3].Z / heightStep;

				rampProperties[ramp] = new RampProperties()
				{
					Brushes = rampsToBrushes.GetValueOrDefault(ramp, []).ToArray(),
					Tl = (byte)tl,
					Tr = (byte)tr,
					Br = (byte)br,
					Bl = (byte)bl,
				};

				if (rampProperties[ramp].Brushes.Length > 0)
				{
					var lookup = tl | (tr << 2) | (br << 4) | (bl << 6);
					if (!rampLookup.ContainsKey(lookup))
						rampLookup.Add(lookup, []);

					rampLookup[lookup].Add(ramp);
				}
			}
		}

		byte GetConnectionHeight(byte height, TerrainTileInfo info, Riser.Connection connection)
		{
			var riser = info.Riser;
			var properties = rampProperties[info.RampType];
			var unclamped =
				riser[connection].HasValue
					? height + riser[connection].Value - info.Height
					: height + properties.GetCorner(connection);
			return (byte)Math.Clamp(unclamped, byte.MinValue, byte.MaxValue);
		}

		/// <summary>
		/// Pull the heights of untileable cells from the map into a heightMap,
		/// providing anchor points for later constraints and tiling.
		/// </summary>
		public void PullHeightMap(HeightMap heightMap)
		{
			foreach (var cpos in heightMap.Tileable.CellRegion)
				PullHeightMap(heightMap, cpos);
		}

		/// <summary>
		/// Pull the heights of an untileable cell from the map into a heightMap,
		/// providing anchor points for later constraints and tiling.
		/// </summary>
		public void PullHeightMap(HeightMap heightMap, CPos cpos)
		{
			if (!heightMap.Tileable.Contains(cpos) || heightMap.Tileable[cpos])
				return;

			var height = map.Height[cpos];
			var info = map.Rules.TerrainInfo.GetTerrainInfo(map.Tiles[cpos]);
			for (var i = 0; i < 8; i++)
			{
				var connection = (Riser.Connection)i;
				var toCVec = ConnectionToAdjacentCorner(connection);
				var toCPos = cpos + toCVec;
				var toXy = heightMap.CPosToXy(toCPos);
				if (!(heightMap.Adjustable.ContainsXY(toXy) && heightMap.Adjustable[toXy]))
					continue;

				if (!heightMap.IsInternalCorner(toXy))
					continue;

				var connectionHeight = GetConnectionHeight(height, info, connection);

				var lower = Math.Clamp(connectionHeight - 1, byte.MinValue, byte.MaxValue);
				var upper = Math.Clamp(connectionHeight + 1, byte.MinValue, byte.MaxValue);
				heightMap.LowerBound[toXy] = (byte)Math.Max(heightMap.LowerBound[toXy], lower);
				heightMap.UpperBound[toXy] = (byte)Math.Min(heightMap.UpperBound[toXy], upper);
			}
		}

		/// <summary>
		/// Generate height and ramp CellLayers (as might be used in a Map) from a heightMap.
		/// </summary>
		/// <param name="heightMap">HeightMap to derive heights and ramps from.</param>
		/// <param name="random">Random source for picking ramps with identical corner heights.</param>
		/// <returns>The height and ramp CellLayers, or (null, null) if no solution is available.</returns>
		public (CellLayer<byte> Heights, CellLayer<byte> Ramps) GenerateRampsAndHeights(
			HeightMap heightMap,
			MersenneTwister random)
		{
			var tlCorners = new CellLayer<byte>(map);
			var trCorners = new CellLayer<byte>(map);
			var brCorners = new CellLayer<byte>(map);
			var blCorners = new CellLayer<byte>(map);

			var heights = CellLayerUtils.Clone(map.Height);

			// Map may not have ramps initialized. Don't clone.
			var ramps = new CellLayer<byte>(map);

			foreach (var cpos in heightMap.Tileable.CellRegion)
			{
				var height = map.Height[cpos];
				var info = map.Rules.TerrainInfo.GetTerrainInfo(map.Tiles[cpos]);
				ramps[cpos] = info.RampType;

				if (heightMap.Tileable[cpos])
				{
					var xy = heightMap.CPosToXy(cpos);
					var tl = xy;
					var tr = xy + new int2(1, 0);
					var br = xy + new int2(1, 1);
					var bl = xy + new int2(0, 1);
					if (heightMap.Adjustable[tl])
						tlCorners[cpos] = heightMap.Target[tl];

					if (heightMap.Adjustable[tr])
						trCorners[cpos] = heightMap.Target[tr];

					if (heightMap.Adjustable[br])
						brCorners[cpos] = heightMap.Target[br];

					if (heightMap.Adjustable[bl])
						blCorners[cpos] = heightMap.Target[bl];
				}
				else
				{
					var r = new CVec(1, 0);
					var d = new CVec(0, 1);
					var l = new CVec(-1, 0);
					var u = new CVec(0, -1);

					if (heightMap.Tileable.Contains(cpos + r) && heightMap.Tileable[cpos + r])
					{
						tlCorners[cpos + r] = GetConnectionHeight(height, info, Riser.Connection.RU);
						blCorners[cpos + r] = GetConnectionHeight(height, info, Riser.Connection.RD);

						if (heightMap.Tileable.Contains(cpos + r + u) && heightMap.Tileable[cpos + r + u])
						{
							blCorners[cpos + r + u] = GetConnectionHeight(height, info, Riser.Connection.RU);
						}

						if (heightMap.Tileable.Contains(cpos + r + d) && heightMap.Tileable[cpos + r + d])
						{
							tlCorners[cpos + r + d] = GetConnectionHeight(height, info, Riser.Connection.RD);
						}
					}

					if (heightMap.Tileable.Contains(cpos + d) && heightMap.Tileable[cpos + d])
					{
						trCorners[cpos + d] = GetConnectionHeight(height, info, Riser.Connection.DR);
						tlCorners[cpos + d] = GetConnectionHeight(height, info, Riser.Connection.DL);

						if (heightMap.Tileable.Contains(cpos + d + r) && heightMap.Tileable[cpos + d + r])
						{
							tlCorners[cpos + d + r] = GetConnectionHeight(height, info, Riser.Connection.DR);
						}

						if (heightMap.Tileable.Contains(cpos + d + l) && heightMap.Tileable[cpos + d + l])
						{
							trCorners[cpos + d + l] = GetConnectionHeight(height, info, Riser.Connection.DL);
						}
					}

					if (heightMap.Tileable.Contains(cpos + l) && heightMap.Tileable[cpos + l])
					{
						brCorners[cpos + l] = GetConnectionHeight(height, info, Riser.Connection.LD);
						trCorners[cpos + l] = GetConnectionHeight(height, info, Riser.Connection.LU);

						if (heightMap.Tileable.Contains(cpos + l + d) && heightMap.Tileable[cpos + l + d])
						{
							trCorners[cpos + l + d] = GetConnectionHeight(height, info, Riser.Connection.LD);
						}

						if (heightMap.Tileable.Contains(cpos + l + u) && heightMap.Tileable[cpos + l + u])
						{
							brCorners[cpos + l + u] = GetConnectionHeight(height, info, Riser.Connection.LU);
						}
					}

					if (heightMap.Tileable.Contains(cpos + u) && heightMap.Tileable[cpos + u])
					{
						blCorners[cpos + u] = GetConnectionHeight(height, info, Riser.Connection.UL);
						brCorners[cpos + u] = GetConnectionHeight(height, info, Riser.Connection.UR);

						if (heightMap.Tileable.Contains(cpos + u + l) && heightMap.Tileable[cpos + u + l])
						{
							brCorners[cpos + u + l] = GetConnectionHeight(height, info, Riser.Connection.UL);
						}

						if (heightMap.Tileable.Contains(cpos + u + r) && heightMap.Tileable[cpos + u + r])
						{
							blCorners[cpos + u + r] = GetConnectionHeight(height, info, Riser.Connection.UR);
						}
					}
				}
			}

			foreach (var cpos in heightMap.Tileable.CellRegion)
			{
				if (!heightMap.Tileable[cpos])
					continue;

				if (!heightMap.IsInternalCell(cpos))
					continue;

				var tl = tlCorners[cpos];
				var tr = trCorners[cpos];
				var br = brCorners[cpos];
				var bl = blCorners[cpos];

				var baseHeight = Math.Min(Math.Min(tl, tr), Math.Min(bl, br));
				tl -= baseHeight;
				tr -= baseHeight;
				br -= baseHeight;
				bl -= baseHeight;
				if (Math.Abs(tl - tr) > 1 ||
					Math.Abs(tr - br) > 1 ||
					Math.Abs(br - bl) > 1 ||
					Math.Abs(bl - tl) > 1)
				{
					return (null, null);
				}

				var lookup = tl | (tr << 2) | (br << 4) | (bl << 6);
				if (!rampLookup.TryGetValue(lookup, out var validRamps))
					return (null, null);

				heights[cpos] = baseHeight;
				ramps[cpos] =
					validRamps.Count == 1
						? validRamps[0]
						: validRamps[random.Next() % validRamps.Count];
			}

			return (heights, ramps);
		}

		/// <summary>Wrapper around CornersToRampsAndHeights and Tile.</summary>
		public MultiBrush TileHeightMap(HeightMap heightMap, MersenneTwister random)
		{
			var (heights, ramps) = GenerateRampsAndHeights(heightMap, random);
			if (heights == null)
				return null;

			return Tile(heights, ramps, heightMap.Tileable, random);
		}

		/// <summary>
		/// Tile a heightmap with pre-computed ramps.
		/// </summary>
		/// <param name="heights">Heights for tiles.</param>
		/// <param name="ramps">Ramps for tiles.</param>
		/// <param name="mask">Cells to include in the output. Can be null to include everything.</param>
		/// <param name="random">Random source for picking brushes.</param>
		/// <returns>A MultiBrush containing the tiled result, or null if tiling is not possible.</returns>
		public MultiBrush Tile(CellLayer<byte> heights, CellLayer<byte> ramps, CellLayer<bool> mask, MersenneTwister random)
		{
			var result = new MultiBrush();
			var mapGridType = map.Grid.Type;
			var masked =
				mask != null
					? mask.CellRegion.Where(cpos => mask[cpos])
					: map.Tiles.CellRegion;
			foreach (var cpos in masked)
			{
				var brushes = rampProperties[ramps[cpos]].Brushes;
				if (brushes.Length == 0)
					return null;

				var brush = MultiBrush.PickAny(brushes, random);
				result.MergeFrom(brush, cpos - CPos.Zero, mapGridType, heights[cpos]);
			}

			return result;
		}
	}
}
