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
using OpenRA;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Orders
{
	public static class FormationLayout
	{
		/// <summary>
		/// Returns local-space cell offsets on a uniform grid centered on the anchor.
		/// X = east, Y = north. Spacing is the center-to-center distance between adjacent slots.
		/// </summary>
		public static CVec[] GetOffsets(FormationType formation, int count, int spacing)
		{
			if (count <= 0)
				return [];

			if (count == 1)
				return [CVec.Zero];

			var offsets = formation switch
			{
				FormationType.Default => Circle(count, spacing),
				FormationType.Square => Square(count, spacing),
				FormationType.Circle => Circle(count, spacing),
				FormationType.LineHorizontal => LineHorizontal(count, spacing),
				FormationType.LineVertical => LineVertical(count, spacing),
				FormationType.Pyramid => PyramidVertical(count, spacing, inverted: false),
				FormationType.PyramidInverted => PyramidVertical(count, spacing, inverted: true),
				FormationType.PyramidRight => PyramidHorizontal(count, spacing, pointingRight: true),
				FormationType.PyramidLeft => PyramidHorizontal(count, spacing, pointingRight: false),
				FormationType.VFormation => VShapeVertical(count, spacing, inverted: false),
				FormationType.VInverted => VShapeVertical(count, spacing, inverted: true),
				FormationType.VLeft => VShapeHorizontal(count, spacing, pointingLeft: false),
				FormationType.VRight => VShapeHorizontal(count, spacing, pointingLeft: true),
				_ => Circle(count, spacing),
			};

			if (formation == FormationType.Square || UsesPointAnchor(formation))
				return offsets;

			return CenterOnGrid(offsets, spacing);
		}

		public static bool UsesPointAnchor(FormationType formation)
		{
			return formation switch
			{
				FormationType.Pyramid or FormationType.PyramidInverted or FormationType.PyramidRight or FormationType.PyramidLeft => true,
				FormationType.VFormation or FormationType.VInverted or FormationType.VLeft or FormationType.VRight => true,
				_ => false,
			};
		}

		static CVec[] CenterOnGrid(CVec[] offsets, int spacing)
		{
			if (offsets.Length == 0)
				return offsets;

			var minX = offsets.Min(o => o.X);
			var maxX = offsets.Max(o => o.X);
			var minY = offsets.Min(o => o.Y);
			var maxY = offsets.Max(o => o.Y);

			var cx = GridCenter(minX, maxX, spacing);
			var cy = GridCenter(minY, maxY, spacing);
			return offsets.Select(o => new CVec(o.X - cx, o.Y - cy)).ToArray();
		}

		static int GridCenter(int min, int max, int spacing)
		{
			if (spacing <= 0)
				return (min + max) / 2;

			var center = (min + max) / 2;
			var remainder = center % spacing;
			if (remainder == 0)
				return center;

			var down = center - remainder;
			var up = down + spacing;
			return Math.Abs(center - down) <= Math.Abs(up - center) ? down : up;
		}

		public static WAngle AdjustFacing(FormationType formation, WAngle movementFacing)
		{
			return movementFacing;
		}

		static CVec[] Square(int count, int spacing)
		{
			var cols = BestColumnCount(count);
			var rows = (count + cols - 1) / cols;
			var offsets = new List<CVec>(count);

			var startX = GridStart(cols, spacing);
			var startY = GridStart(rows, spacing);

			for (var row = 0; row < rows && offsets.Count < count; row++)
			{
				var unitsThisRow = Math.Min(cols, count - offsets.Count);
				var rowStartX = startX;

				// Keep the last partial row centered within the full grid width.
				if (unitsThisRow < cols)
					rowStartX += (cols - unitsThisRow) * spacing / 2;

				for (var col = 0; col < unitsThisRow; col++)
					offsets.Add(new CVec(rowStartX + col * spacing, startY + row * spacing));
			}

			return offsets.ToArray();
		}

		static int BestColumnCount(int count)
		{
			var side = (int)Math.Round(Math.Sqrt(count));
			if (side * side == count)
				return side;

			var bestCols = 1;
			var bestScore = int.MaxValue;

			for (var cols = 1; cols <= count; cols++)
			{
				var rows = (count + cols - 1) / cols;
				var score = Math.Abs(cols - rows);
				if (score < bestScore || (score == bestScore && cols < bestCols))
				{
					bestScore = score;
					bestCols = cols;
				}
			}

			return bestCols;
		}

		static CVec[] Circle(int count, int spacing)
		{
			if (count <= 1)
				return [CVec.Zero];

			var radius = (int)Math.Ceiling(Math.Sqrt(count)) + 1;
			var candidates = new List<(CVec Offset, int RadiusSquared, int AngleKey)>();

			for (var y = -radius; y <= radius; y++)
			{
				for (var x = -radius; x <= radius; x++)
				{
					var offset = new CVec(x * spacing, y * spacing);
					var radiusSquared = x * x + y * y;
					var angleKey = x == 0 && y == 0 ? 0 : WAngle.ArcTan(x, y).Angle;
					candidates.Add((offset, radiusSquared, angleKey));
				}
			}

			return candidates
				.OrderBy(c => c.RadiusSquared)
				.ThenBy(c => c.AngleKey)
				.Take(count)
				.Select(c => c.Offset)
				.ToArray();
		}

		static CVec[] LineHorizontal(int count, int spacing)
		{
			var offsets = new CVec[count];
			var startX = GridStart(count, spacing);

			for (var i = 0; i < count; i++)
				offsets[i] = new CVec(startX + i * spacing, 0);

			return offsets;
		}

		static CVec[] LineVertical(int count, int spacing)
		{
			var offsets = new CVec[count];
			var startY = GridStart(count, spacing);

			for (var i = 0; i < count; i++)
				offsets[i] = new CVec(0, startY + i * spacing);

			return offsets;
		}

		static CVec[] PyramidVertical(int count, int spacing, bool inverted)
		{
			var offsets = new List<CVec>(count);
			var row = 1;
			var depth = 0;

			while (offsets.Count < count)
			{
				var rowWidth = row;
				var startX = GridStart(rowWidth, spacing);
				var y = inverted ? depth * spacing : -depth * spacing;

				for (var col = 0; col < rowWidth && offsets.Count < count; col++)
					offsets.Add(new CVec(startX + col * spacing, y));

				row++;
				depth++;
			}

			return offsets.ToArray();
		}

		static CVec[] PyramidHorizontal(int count, int spacing, bool pointingRight)
		{
			var columns = new List<CVec[]>();
			var height = 1;
			var col = 0;
			var placed = 0;

			while (placed < count)
			{
				var colOffsets = new List<CVec>();
				var startY = GridStart(height, spacing);
				if (col % 2 == 1)
					startY += spacing / 2;

				for (var row = 0; row < height && placed < count; row++)
				{
					colOffsets.Add(new CVec(0, startY + row * spacing));
					placed++;
				}

				columns.Add(colOffsets.ToArray());
				col++;
				height++;
			}

			var offsets = new List<CVec>(count);
			for (var ci = 0; ci < columns.Count; ci++)
			{
				var x = (pointingRight ? -ci : ci) * spacing;
				foreach (var unit in columns[ci])
					offsets.Add(new CVec(x, unit.Y));
			}

			return offsets.ToArray();
		}

		static int GridStart(int count, int spacing)
		{
			return -(count / 2) * spacing;
		}

		static CVec[] VShapeVertical(int count, int spacing, bool inverted)
		{
			var offsets = new List<CVec>(count) { CVec.Zero };
			var arm = 1;

			while (offsets.Count < count)
			{
				var depth = arm * spacing;
				var lateral = arm * spacing;
				var y = inverted ? depth : -depth;

				if (offsets.Count < count)
					offsets.Add(new CVec(-lateral, y));

				if (offsets.Count < count)
					offsets.Add(new CVec(lateral, y));

				arm++;
			}

			return offsets.ToArray();
		}

		static CVec[] VShapeHorizontal(int count, int spacing, bool pointingLeft)
		{
			var offsets = new List<CVec>(count) { CVec.Zero };
			var arm = 1;

			while (offsets.Count < count)
			{
				var depth = arm * spacing;
				var lateral = arm * spacing;
				var x = pointingLeft ? -depth : depth;

				if (offsets.Count < count)
					offsets.Add(new CVec(x, -lateral));

				if (offsets.Count < count)
					offsets.Add(new CVec(x, lateral));

				arm++;
			}

			return offsets.ToArray();
		}

		static CVec[] Repeat(CVec value, int count)
		{
			var offsets = new CVec[count];
			for (var i = 0; i < count; i++)
				offsets[i] = value;

			return offsets;
		}
	}
}
