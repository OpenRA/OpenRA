#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA
{
	public enum MapGridType { Rectangular, RectangularIsometric }

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

		public WVec[][] CellCorners { get; private set; }

		readonly int[][] cellCornerHalfHeights = new int[][]
		{
			// Flat
			new[] { 0, 0, 0, 0 },

			// Slopes (two corners high)
			new[] { 0, 0, 1, 1 },
			new[] { 1, 0, 0, 1 },
			new[] { 1, 1, 0, 0 },
			new[] { 0, 1, 1, 0 },

			// Slopes (one corner high)
			new[] { 0, 0, 0, 1 },
			new[] { 1, 0, 0, 0 },
			new[] { 0, 1, 0, 0 },
			new[] { 0, 0, 1, 0 },

			// Slopes (three corners high)
			new[] { 1, 0, 1, 1 },
			new[] { 1, 1, 0, 1 },
			new[] { 1, 1, 1, 0 },
			new[] { 0, 1, 1, 1 },

			// Slopes (two corners high, one corner double high)
			new[] { 1, 0, 1, 2 },
			new[] { 2, 1, 0, 1 },
			new[] { 1, 2, 1, 0 },
			new[] { 0, 1, 2, 1 },

			// Slopes (two corners high, alternating)
			new[] { 1, 0, 1, 0 },
			new[] { 0, 1, 0, 1 },
			new[] { 1, 0, 1, 0 },
			new[] { 0, 1, 0, 1 }
		};

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

			var makeCorners = Type == MapGridType.RectangularIsometric ?
				(Func<int[], WVec[]>)IsometricCellCorners : RectangularCellCorners;
			CellCorners = cellCornerHalfHeights.Select(makeCorners).ToArray();
			TilesByDistance = CreateTilesByDistance();
		}

		static WVec[] IsometricCellCorners(int[] cornerHeight)
		{
			return new WVec[]
			{
				new WVec(-724, 0, 724 * cornerHeight[0]),
				new WVec(0, -724, 724 * cornerHeight[1]),
				new WVec(724, 0, 724 * cornerHeight[2]),
				new WVec(0, 724, 724 * cornerHeight[3])
			};
		}

		static WVec[] RectangularCellCorners(int[] cornerHeight)
		{
			return new WVec[]
			{
				new WVec(-512, -512, 512 * cornerHeight[0]),
				new WVec(512, -512, 512 * cornerHeight[1]),
				new WVec(512, 512, 512 * cornerHeight[2]),
				new WVec(-512, 512, 512 * cornerHeight[3])
			};
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
