#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA
{
	public enum MapGridType { Rectangular, RectangularIsometric }

	public enum RampSplit { Flat, X, Y }
	public enum RampCornerHeight { Low = 0, Half = 1, Full = 2 }

	public readonly struct CellRamp
	{
		public readonly int CenterHeightOffset;
		public readonly WVec[] Corners;
		public readonly WVec[][] Polygons;
		public readonly WRot Orientation;

		public CellRamp(MapGridType type, WRot orientation, RampCornerHeight tl = RampCornerHeight.Low, RampCornerHeight tr = RampCornerHeight.Low, RampCornerHeight br = RampCornerHeight.Low,  RampCornerHeight bl = RampCornerHeight.Low, RampSplit split = RampSplit.Flat)
		{
			Orientation = orientation;
			if (type == MapGridType.RectangularIsometric)
			{
				Corners = new[]
				{
					new WVec(0, -724, 724 * (int)tl),
					new WVec(724, 0, 724 * (int)tr),
					new WVec(0, 724, 724 * (int)br),
					new WVec(-724, 0, 724 * (int)bl),
				};
			}
			else
			{
				Corners = new[]
				{
					new WVec(-512, -512, 512 * (int)tl),
					new WVec(512, -512, 512 * (int)tr),
					new WVec(512, 512, 512 * (int)br),
					new WVec(-512, 512, 512 * (int)bl)
				};
			}

			if (split == RampSplit.X)
			{
				Polygons = new[]
				{
					new[] { Corners[0], Corners[1], Corners[3] },
					new[] { Corners[1], Corners[2], Corners[3] }
				};
			}
			else if (split == RampSplit.Y)
			{
				Polygons = new[]
				{
					new[] { Corners[0], Corners[1], Corners[2] },
					new[] { Corners[0], Corners[2], Corners[3] }
				};
			}
			else
				Polygons = new[] { Corners };

			// Initial value must be assigned before HeightOffset can be called
			CenterHeightOffset = 0;
			CenterHeightOffset = HeightOffset(0, 0);
		}

		public int HeightOffset(int dX, int dY)
		{
			// Enumerate over the polygons, assuming that they are triangles
			// If the ramp is not split we will take the first three vertices of the corners as a valid triangle
			WVec[] p = null;
			var u = 0;
			var v = 0;
			for (var i = 0; i < Polygons.Length; i++)
			{
				p = Polygons[i];
				u = ((p[1].Y - p[2].Y) * (dX - p[2].X) - (p[1].X - p[2].X) * (dY - p[2].Y)) / 1024;
				v = ((p[0].X - p[2].X) * (dY - p[2].Y) - (p[0].Y - p[2].Y) * (dX - p[2].X)) / 1024;

				// Point is within the triangle if 0 <= u,v <= 1024
				if (u >= 0 && u <= 1024 && v >= 0 && v <= 1024)
					break;
			}

			// Calculate w from u,v and interpolate height
			return (u * p[0].Z + v * p[1].Z + (1024 - u - v) * p[2].Z) / 1024;
		}
	}

	public class MapGrid : IGlobalModData
	{
		public readonly MapGridType Type = MapGridType.Rectangular;
		public readonly Size TileSize = new Size(24, 24);
		public readonly byte MaximumTerrainHeight = 0;
		public readonly SubCell DefaultSubCell = (SubCell)byte.MaxValue;

		public readonly int MaximumTileSearchRange = 50;

		public readonly bool EnableDepthBuffer = false;

		public readonly WVec[] SubCellOffsets =
		{
			new WVec(0, 0, 0),       // full cell - index 0
			new WVec(-299, -256, 0), // top left - index 1
			new WVec(256, -256, 0),  // top right - index 2
			new WVec(0, 0, 0),       // center - index 3
			new WVec(-299, 256, 0),  // bottom left - index 4
			new WVec(256, 256, 0),   // bottom right - index 5
		};

		public CellRamp[] Ramps { get; }

		internal readonly CVec[][] TilesByDistance;

		public MapGrid(MiniYaml yaml)
		{
			FieldLoader.Load(this, yaml);

			// The default subcell index defaults to the middle entry
			var defaultSubCellIndex = (byte)DefaultSubCell;
			if (defaultSubCellIndex == byte.MaxValue)
				DefaultSubCell = (SubCell)(SubCellOffsets.Length / 2);
			else
			{
				var minSubCellOffset = SubCellOffsets.Length > 1 ? 1 : 0;
				if (defaultSubCellIndex < minSubCellOffset || defaultSubCellIndex >= SubCellOffsets.Length)
					throw new InvalidDataException("Subcell default index must be a valid index into the offset triples and must be greater than 0 for mods with subcells");
			}

			// Rotation axes and amounts for the different slope types
			var southEast = new WVec(724, 724, 0);
			var southWest = new WVec(-724, 724, 0);
			var south = new WVec(0, 1024, 0);
			var east = new WVec(1024, 0, 0);

			var forward = new WAngle(64);
			var backward = -forward;
			var halfForward = new WAngle(48);
			var halfBackward = -halfForward;

			// Slope types are hardcoded following the convention from the TS and RA2 map format
			Ramps = new[]
			{
				// Flat
				new CellRamp(Type, WRot.None),

				// Two adjacent corners raised by half a cell
				new CellRamp(Type, new WRot(southEast, backward), tr: RampCornerHeight.Half, br: RampCornerHeight.Half),
				new CellRamp(Type, new WRot(southWest, backward), br: RampCornerHeight.Half, bl: RampCornerHeight.Half),
				new CellRamp(Type, new WRot(southEast, forward), tl: RampCornerHeight.Half, bl: RampCornerHeight.Half),
				new CellRamp(Type, new WRot(southWest, forward), tl: RampCornerHeight.Half, tr: RampCornerHeight.Half),

				// One corner raised by half a cell
				new CellRamp(Type, new WRot(south, halfBackward), br: RampCornerHeight.Half, split: RampSplit.X),
				new CellRamp(Type, new WRot(east, halfForward), bl: RampCornerHeight.Half, split: RampSplit.Y),
				new CellRamp(Type, new WRot(south, halfForward), tl: RampCornerHeight.Half, split: RampSplit.X),
				new CellRamp(Type, new WRot(east, halfBackward), tr: RampCornerHeight.Half, split: RampSplit.Y),

				// Three corners raised by half a cell
				new CellRamp(Type, new WRot(south, halfBackward), tr: RampCornerHeight.Half, br: RampCornerHeight.Half, bl: RampCornerHeight.Half, split: RampSplit.X),
				new CellRamp(Type, new WRot(east, halfForward), tl: RampCornerHeight.Half, br: RampCornerHeight.Half, bl: RampCornerHeight.Half, split: RampSplit.Y),
				new CellRamp(Type, new WRot(south, halfForward), tl: RampCornerHeight.Half, tr: RampCornerHeight.Half, bl: RampCornerHeight.Half, split: RampSplit.X),
				new CellRamp(Type, new WRot(east, halfBackward), tl: RampCornerHeight.Half, tr: RampCornerHeight.Half, br: RampCornerHeight.Half, split: RampSplit.Y),

				// Full tile sloped (mid corners raised by half cell, far corner by full cell)
				new CellRamp(Type, new WRot(south, backward), tr: RampCornerHeight.Half, br: RampCornerHeight.Full, bl: RampCornerHeight.Half),
				new CellRamp(Type, new WRot(east, forward), tl: RampCornerHeight.Half, br: RampCornerHeight.Half, bl: RampCornerHeight.Full),
				new CellRamp(Type, new WRot(south, forward), tl: RampCornerHeight.Full, tr: RampCornerHeight.Half, bl: RampCornerHeight.Half),
				new CellRamp(Type, new WRot(east, backward), tl: RampCornerHeight.Half, tr: RampCornerHeight.Full, br: RampCornerHeight.Half),

				// Two opposite corners raised by half a cell
				new CellRamp(Type, WRot.None, tr: RampCornerHeight.Half, bl: RampCornerHeight.Half, split: RampSplit.Y),
				new CellRamp(Type, WRot.None, tl: RampCornerHeight.Half, br: RampCornerHeight.Half, split: RampSplit.Y),
				new CellRamp(Type, WRot.None, tr: RampCornerHeight.Half, bl: RampCornerHeight.Half, split: RampSplit.X),
				new CellRamp(Type, WRot.None, tl: RampCornerHeight.Half, br: RampCornerHeight.Half, split: RampSplit.X),
			};

			TilesByDistance = CreateTilesByDistance();
		}

		CVec[][] CreateTilesByDistance()
		{
			var ts = new List<CVec>[MaximumTileSearchRange + 1];
			for (var i = 0; i < MaximumTileSearchRange + 1; i++)
				ts[i] = new List<CVec>();

			for (var j = -MaximumTileSearchRange; j <= MaximumTileSearchRange; j++)
				for (var i = -MaximumTileSearchRange; i <= MaximumTileSearchRange; i++)
					if (MaximumTileSearchRange * MaximumTileSearchRange >= i * i + j * j)
						ts[Exts.ISqrt(i * i + j * j, Exts.ISqrtRoundMode.Ceiling)].Add(new CVec(i, j));

			// Sort each integer-distance group by the actual distance
			foreach (var list in ts)
			{
				list.Sort((a, b) =>
				{
					var result = a.LengthSquared.CompareTo(b.LengthSquared);
					if (result != 0)
						return result;

					// If the lengths are equal, use other means to sort them.
					// Try the hash code first because it gives more
					// random-appearing results than X or Y that would always
					// prefer the leftmost/topmost position.
					result = a.GetHashCode().CompareTo(b.GetHashCode());
					if (result != 0)
						return result;

					result = a.X.CompareTo(b.X);
					if (result != 0)
						return result;

					return a.Y.CompareTo(b.Y);
				});
			}

			return ts.Select(list => list.ToArray()).ToArray();
		}

		public WVec OffsetOfSubCell(SubCell subCell)
		{
			if (subCell == SubCell.Invalid || subCell == SubCell.Any)
				return WVec.Zero;

			var index = (int)subCell;
			if (index >= 0 && index < SubCellOffsets.Length)
				return SubCellOffsets[index];

			return WVec.Zero;
		}
	}
}
