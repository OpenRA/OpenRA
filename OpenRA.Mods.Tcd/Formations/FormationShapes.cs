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

namespace OpenRA.Mods.Tcd.Formations
{
	public enum FormationShape
	{
		Grid,
		Wedge,
	}

	// The two tight preset shapes. Anything else is drawn by hand - see FormationPath.
	//
	// Pure geometry. No World, no Actor, no engine state - which is why this is the
	// one part of the mod that can be unit tested without launching the game.
	public static class FormationShapes
	{
		// 1024 / sqrt(2), for stepping along a 45 degree arm.
		const int Diagonal = 724;

		// Produces one offset per entry in ranks, in the same order.
		// The local frame is +X right, +Y backwards, so rank 0 ends up at the front
		// once the caller rotates the result by the formation's facing.
		// The block is centred on the origin.
		public static WVec[] Offsets(FormationShape shape, IReadOnlyList<int> ranks, int spacing, int maxRowWidth = 8)
		{
			ArgumentNullException.ThrowIfNull(ranks);
			if (spacing <= 0)
				throw new ArgumentOutOfRangeException(nameof(spacing), "Spacing must be positive.");
			if (maxRowWidth <= 0)
				throw new ArgumentOutOfRangeException(nameof(maxRowWidth), "Row width must be positive.");

			var count = ranks.Count;
			var offsets = new WVec[count];
			if (count == 0)
				return offsets;

			// Stable order: by rank, and by the caller's order within a rank. The caller
			// sorts within a rank by lateral position so units do not cross over.
			var order = new int[count];
			for (var i = 0; i < count; i++)
				order[i] = i;

			Array.Sort(order, (a, b) => ranks[a] != ranks[b] ? ranks[a].CompareTo(ranks[b]) : a.CompareTo(b));

			if (shape == FormationShape.Wedge)
				BuildWedge(order, offsets, spacing);
			else
				BuildGrid(order, offsets, spacing, maxRowWidth);

			Centre(offsets);
			return offsets;
		}

		// A rectangle as close to square as the count allows, capped at maxRowWidth.
		// Six units give two rows of three; twelve give three rows of four.
		static void BuildGrid(int[] order, WVec[] offsets, int spacing, int maxRowWidth)
		{
			var count = order.Length;
			var cols = 1;
			while (cols * cols < count)
				cols++;

			cols = Math.Min(cols, maxRowWidth);

			var row = 0;
			for (var start = 0; start < count; start += cols)
			{
				var width = Math.Min(cols, count - start);
				for (var i = 0; i < width; i++)
				{
					var x = (2 * i - (width - 1)) * spacing / 2;
					offsets[order[start + i]] = new WVec(x, row * spacing, 0);
				}

				row++;
			}
		}

		// An arrowhead: the front rank takes the tip, everyone else fills back along
		// the two arms. The arms run at 45 degrees, so the step is taken along the arm
		// rather than along each axis - otherwise neighbours sit spacing*sqrt(2) apart.
		static void BuildWedge(int[] order, WVec[] offsets, int spacing)
		{
			offsets[order[0]] = WVec.Zero;

			for (var i = 1; i < order.Length; i++)
			{
				var depth = (i + 1) / 2;
				var side = (i % 2 == 1) ? -1 : 1;
				var step = depth * spacing * Diagonal / 1024;
				offsets[order[i]] = new WVec(side * step, step, 0);
			}
		}

		static void Centre(WVec[] offsets)
		{
			var sum = WVec.Zero;
			foreach (var o in offsets)
				sum += o;

			var mean = sum / offsets.Length;
			for (var i = 0; i < offsets.Length; i++)
				offsets[i] -= mean;
		}
	}
}
