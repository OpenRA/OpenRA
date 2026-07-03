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

using OpenRA.Primitives;



namespace OpenRA.Mods.Common.Orders

{

	public static class FormationLayout

	{

		/// <summary>

		/// Returns local-space cell offsets (X = right, Y = forward) centered on the anchor.

		/// Spacing is the center-to-center distance between adjacent slots on the grid.

		/// </summary>

		public static CVec[] GetOffsets(FormationType formation, int count, int spacing)

		{

			if (count <= 0)

				return [];



			if (count == 1)

				return [CVec.Zero];



			return formation switch

			{

				FormationType.Square => Square(count, spacing),

				FormationType.Circle => Circle(count, spacing),

				FormationType.LineHorizontal => LineHorizontal(count, spacing),

				FormationType.LineVertical => LineVertical(count, spacing),

				FormationType.Pyramid => Pyramid(count, spacing, inverted: false),

				FormationType.PyramidInverted => Pyramid(count, spacing, inverted: true),

				FormationType.VFormation => VShape(count, spacing, inverted: false),

				FormationType.VInverted => VShape(count, spacing, inverted: true),

				FormationType.VLeft => VShape(count, spacing, inverted: false),

				FormationType.VRight => VShape(count, spacing, inverted: false),

				_ => EnumerableRepeat(CVec.Zero, count),

			};

		}



		public static WAngle AdjustFacing(FormationType formation, WAngle movementFacing)

		{

			return formation switch

			{

				FormationType.VLeft => movementFacing + new WAngle(256),

				FormationType.VRight => movementFacing - new WAngle(256),

				_ => movementFacing,

			};

		}



		static CVec[] Square(int count, int spacing)

		{

			var cols = BestColumnCount(count);

			var rows = (count + cols - 1) / cols;

			var offsets = new List<CVec>(count);



			var startX = -(cols - 1) * spacing / 2;

			var startY = -(rows - 1) * spacing / 2;



			for (var row = 0; row < rows && offsets.Count < count; row++)

			{

				for (var col = 0; col < cols && offsets.Count < count; col++)

					offsets.Add(new CVec(startX + col * spacing, startY + row * spacing));

			}



			return offsets.ToArray();

		}



		static int BestColumnCount(int count)
		{
			var bestCols = (int)Math.Ceiling(Math.Sqrt(count));
			var bestRows = (count + bestCols - 1) / bestCols;
			var bestScore = Math.Abs(bestCols - bestRows);

			for (var cols = 1; cols <= count; cols++)
			{
				var rows = (count + cols - 1) / cols;
				var score = Math.Abs(cols - rows);
				if (score < bestScore)
				{
					bestScore = score;
					bestCols = cols;
					bestRows = rows;
				}
			}

			return bestCols;
		}



		static CVec[] Circle(int count, int spacing)

		{

			if (count <= 1)

				return [CVec.Zero];



			// Fill a square lattice, then take the closest points to the center (filled disc).

			var radius = (int)Math.Ceiling(Math.Sqrt(count)) + 1;

			var candidates = new List<(CVec Offset, int RadiusSquared, int AngleKey)>();



			for (var y = -radius; y <= radius; y++)

			{

				for (var x = -radius; x <= radius; x++)

				{

					var offset = new CVec(x * spacing, y * spacing);

					var radiusSquared = x * x + y * y;

					var angleKey = x == 0 && y == 0 ? 0 : WAngle.ArcTan(x, -y).Angle;

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

			var startX = -(count - 1) * spacing / 2;

			for (var i = 0; i < count; i++)

				offsets[i] = new CVec(startX + i * spacing, 0);



			return offsets;

		}



		static CVec[] LineVertical(int count, int spacing)

		{

			var offsets = new CVec[count];

			var startY = -(count - 1) * spacing / 2;

			for (var i = 0; i < count; i++)

				offsets[i] = new CVec(0, startY + i * spacing);



			return offsets;

		}



		static CVec[] Pyramid(int count, int spacing, bool inverted)

		{

			var offsets = new List<CVec>(count);

			var row = 1;

			var depth = 0;

			while (offsets.Count < count)

			{

				var rowWidth = row;

				var startX = -(rowWidth - 1) * spacing / 2;

				var y = inverted ? depth * spacing : -depth * spacing;

				for (var col = 0; col < rowWidth && offsets.Count < count; col++)

					offsets.Add(new CVec(startX + col * spacing, y));



				row++;

				depth++;

			}



			return offsets.ToArray();

		}



		static CVec[] VShape(int count, int spacing, bool inverted)

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



		static CVec[] EnumerableRepeat(CVec value, int count)

		{

			var offsets = new CVec[count];

			for (var i = 0; i < count; i++)

				offsets[i] = value;



			return offsets;

		}

	}

}


