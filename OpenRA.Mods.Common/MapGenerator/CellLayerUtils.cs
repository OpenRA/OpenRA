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
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Support;

namespace OpenRA.Mods.Common.MapGenerator
{
	public static class CellLayerUtils
	{
		static int FloorDiv(int a, int b)
		{
			var q = Math.DivRem(a, b, out var r);
			if (r < 0)
				return q - 1;
			else
				return q;
		}

		/// <summary>Return true iff a and b have the same grid type and size.</summary>
		public static bool AreSameShape<T, U>(CellLayer<T> a, CellLayer<U> b)
		{
			return a.Size == b.Size && a.GridType == b.GridType;
		}

		/// <summary>
		/// Returns the half-way point between the centers of the top-left (first) and bottom-right
		/// (last) MPos cells. This will either lie in the exact center of a cell, an edge between
		/// two cells, or a corner between four cells. Note that this might not fit all reasonable
		/// or intuitive definitions of a map center, but has convenient properties.
		/// </summary>
		public static WPos Center<T>(CellLayer<T> cellLayer)
		{
			switch (cellLayer.GridType)
			{
				case MapGridType.Rectangular:
					return new WPos(
						cellLayer.Size.Width * 512,
						cellLayer.Size.Height * 512,
						0);
				case MapGridType.RectangularIsometric:
					return new WPos(
						(cellLayer.Size.Width * 2 + (~cellLayer.Size.Height & 1)) * 362,
						(cellLayer.Size.Height + 1) * 362,
						0);
				default:
					throw new NotImplementedException();
			}
		}

		/// <summary>Get the WPos of the -X-Y corner of a CPos cell.</summary>
		public static WPos CornerToWPos(CPos xy, MapGridType gridType)
		{
			switch (gridType)
			{
				case MapGridType.Rectangular:
					return new WPos(
						xy.X * 1024,
						xy.Y * 1024,
						0);
				case MapGridType.RectangularIsometric:
					return new WPos(
						(xy.X - xy.Y) * 724 + 724,
						(xy.X + xy.Y) * 724,
						0);
				default:
					throw new NotImplementedException();
			}
		}

		/// <summary>Get the WVec representing the same translation as the given CVec.</summary>
		public static WVec CVecToWVec(CVec cvec, MapGridType gridType)
		{
			switch (gridType)
			{
				case MapGridType.Rectangular:
					return new WVec(
						cvec.X * 1024,
						cvec.Y * 1024,
						0);
				case MapGridType.RectangularIsometric:
					return new WVec(
						(cvec.X - cvec.Y) * 724,
						(cvec.X + cvec.Y) * 724,
						0);
				default:
					throw new NotImplementedException();
			}
		}

		/// <summary>Get the WPos center of a CPos cell.</summary>
		public static WPos CPosToWPos(CPos cpos, MapGridType gridType)
		{
			var wvec = CVecToWVec(new CVec(cpos.X, cpos.Y), gridType);
			switch (gridType)
			{
				case MapGridType.Rectangular:
					return new WPos(512, 512, 0) + wvec;
				case MapGridType.RectangularIsometric:
					return new WPos(724, 724, 0) + wvec;
				default:
					throw new NotImplementedException();
			}
		}

		/// <summary>
		/// Find the CPos cell in which a WPos position lies. WPos positions on
		/// an edge or corner match the CPos with higher X and/or Y positions.
		/// </summary>
		public static CPos WPosToCPos(WPos wpos, MapGridType gridType)
		{
			switch (gridType)
			{
				case MapGridType.Rectangular:
					return new CPos(
						FloorDiv(wpos.X, 1024),
						FloorDiv(wpos.Y, 1024),
						0);
				case MapGridType.RectangularIsometric:
					return new CPos(
						FloorDiv(wpos.Y + wpos.X - 724, 1448),
						FloorDiv(wpos.Y - wpos.X + 724, 1448),
						0);
				default:
					throw new NotImplementedException();
			}
		}

		/// <summary>Get the WPos center of an MPos cell.</summary>
		public static WPos MPosToWPos(MPos mpos, MapGridType gridType)
		{
			return CPosToWPos(mpos.ToCPos(gridType), gridType);
		}

		/// <summary>
		/// Find the MPos cell in which a WPos position lies. WPos positions on
		/// an edge or corner match the CPos (not necessarily MPos) with higher
		/// X and/or Y positions.
		/// </summary>
		public static MPos WPosToMPos(WPos wpos, MapGridType gridType)
		{
			return WPosToCPos(wpos, gridType).ToMPos(gridType);
		}

		/// <summary>
		/// Translates CPos-like positions to zero-based positions.
		/// </summary>
		public static int2[][] ToMatrixPoints<T>(
			IEnumerable<CPos[]> pointArrayArray, CellLayer<T> cellLayer)
		{
			var cellBounds = CellBounds(cellLayer);
			return pointArrayArray
				.Select(xys => xys
					.Select(xy => new int2(xy.X - cellBounds.Left, xy.Y - cellBounds.Top))
					.ToArray())
				.ToArray();
		}

		/// <summary>
		/// Translates zero-based positions to CPos-like positions.
		/// </summary>
		public static CPos[][] FromMatrixPoints<T>(
			IEnumerable<int2[]> pointArrayArray, CellLayer<T> cellLayer)
		{
			var cellBounds = CellBounds(cellLayer);
			return pointArrayArray
				.Select(xys => xys
					.Select(xy => new CPos(xy.X + cellBounds.Left, xy.Y + cellBounds.Top))
					.ToArray())
				.ToArray();
		}

		/// <summary>
		/// <para>
		/// Run an action over the inside or outside of a circle of given center and radius in
		/// world coordinates. The action is called with cells' MPos, CPos, WPos center, and the
		/// squared distance to the WPos center from the circle's center.
		/// If outside is true, the action is run for cells outside of the circle instead of the
		/// inside.
		/// </para>
		/// <para>
		/// A cell is inside the circle if its center is &lt;= wRadius from wCenter.
		/// Coordinates outside of the CellLayer are ignored.
		/// </para>
		/// </summary>
		public static void OverCircle<T>(
			CellLayer<T> cellLayer,
			WPos wCenter,
			int wRadius,
			bool outside,
			Action<MPos, CPos, WPos, long> action)
		{
			var gridType = cellLayer.GridType;
			int minU;
			int minV;
			int maxU;
			int maxV;
			if (outside)
			{
				minU = 0;
				minV = 0;
				maxU = cellLayer.Size.Width - 1;
				maxV = cellLayer.Size.Height - 1;
			}
			else
			{
				var mCenter = WPosToMPos(wCenter, gridType);

				int mRadiusU;
				int mRadiusV;
				switch (gridType)
				{
					case MapGridType.Rectangular:
						mRadiusU = wRadius / 1024 + 1;
						mRadiusV = wRadius / 1024 + 1;
						break;
					case MapGridType.RectangularIsometric:
						mRadiusU = wRadius / 1448 + 2;
						mRadiusV = wRadius / 724 + 2;
						break;
					default:
						throw new NotImplementedException();
				}

				minU = Math.Max(mCenter.U - mRadiusU, 0);
				minV = Math.Max(mCenter.V - mRadiusV, 0);
				maxU = Math.Min(mCenter.U + mRadiusU, cellLayer.Size.Width - 1);
				maxV = Math.Min(mCenter.V + mRadiusV, cellLayer.Size.Height - 1);
			}

			var wRadiusSquared = (long)wRadius * wRadius;
			for (var v = minV; v <= maxV; v++)
				for (var u = minU; u <= maxU; u++)
				{
					var mpos = new MPos(u, v);
					var cpos = mpos.ToCPos(gridType);
					var wpos = CPosToWPos(cpos, gridType);
					var offset = wCenter - wpos;
					var thisRadiusSquared = offset.LengthSquared;
					if (thisRadiusSquared <= wRadiusSquared != outside)
						action(mpos, cpos, wpos, thisRadiusSquared);
				}
		}

		/// <summary>
		/// Return a linear copy of all entries in a CellLayer, ordered v * width + u, similar to
		/// MPos(0, 0), MPos(1, 0), MPos(2, 0), ..., MPos(0, 1), MPos(1, 1), MPos(2, 1), ...
		/// </summary>
		public static T[] Entries<T>(CellLayer<T> cellLayer)
		{
			var i = 0;
			var entries = new T[cellLayer.Size.Width * cellLayer.Size.Height];
			foreach (var value in cellLayer)
				entries[i++] = value;
			return entries;
		}

		/// <summary>
		/// Uniformally add to or subtract from all cells such that count out of every outOf cells,
		/// are no greater than the given target value.
		/// </summary>
		public static void CalibrateQuantileInPlace(CellLayer<int> cellLayer, int target, int count, int outOf)
		{
			var sorted = Entries(cellLayer);
			Array.Sort(sorted);
			var adjustment = target - sorted[(long)(sorted.Length - 1) * count / outOf];
			foreach (var mpos in cellLayer.CellRegion.MapCoords)
				cellLayer[mpos] += adjustment;
		}

		/// <summary>
		/// Get the smallest CPos rectangle that contains all cells for the specified grid.
		/// </summary>
		public static Rectangle CellBounds(Size size, MapGridType gridType)
		{
			switch (gridType)
			{
				case MapGridType.Rectangular:
					return new Rectangle(0, 0, size.Width, size.Height);

				case MapGridType.RectangularIsometric:
				{
					var maxCX =
						new MPos(size.Width - 1, size.Height - 1)
							.ToCPos(gridType).X;
					var minCY =
						new MPos(size.Width - 1, 0)
							.ToCPos(gridType).Y;
					var maxCY =
						new MPos(0, size.Height - 1)
							.ToCPos(gridType).Y;
					return Rectangle.FromLTRB(0, minCY, maxCX + 1, maxCY + 1);
				}

				default:
					throw new NotImplementedException();
			}
		}

		/// <summary>
		/// Get the smallest CPos rectangle that contains all cells in a CellLayer.
		/// </summary>
		public static Rectangle CellBounds<T>(CellLayer<T> cellLayer)
		{
			return CellBounds(cellLayer.Size, cellLayer.GridType);
		}

		/// <summary>
		/// Get the smallest CPos rectangle that contains all cells in a map.
		/// </summary>
		public static Rectangle CellBounds(Map map)
		{
			return CellBounds(new Size(map.MapSize.X, map.MapSize.Y), map.Grid.Type);
		}

		/// <summary>
		/// Copies a CPos-aligned Matrix into a CellLayer. Depending on the grid type, this may
		/// discard data for cells that don't exist in the CellLayer.
		/// </summary>
		public static void FromMatrix<T>(
			CellLayer<T> cellLayer,
			Matrix<T> matrix,
			bool allowOversizedMatrix = false)
		{
			var cellBounds = CellBounds(cellLayer);
			var size = cellBounds.Size.ToInt2();
			if (allowOversizedMatrix)
			{
				if (matrix.Size.X < size.X || matrix.Size.Y < size.Y)
					throw new ArgumentException("source Matrix does not cover destination CellLayer");
			}
			else if (matrix.Size != size)
			{
				throw new ArgumentException("destination and source have incompatible sizes");
			}

			foreach (var cpos in cellLayer.CellRegion)
				cellLayer[cpos] = matrix[cpos.X - cellBounds.Left, cpos.Y - cellBounds.Top];
		}

		/// <summary>
		/// Copies a CellLayer into a CPos-aligned Matrix. Depending on the grid type, this may
		/// fill the matrix with some default values for cells that don't exist in the CellLayer.
		/// </summary>
		public static Matrix<T> ToMatrix<T>(CellLayer<T> cellLayer, T defaultValue)
		{
			var cellBounds = CellBounds(cellLayer);
			var matrix = new Matrix<T>(cellBounds.Size.ToInt2()).Fill(defaultValue);
			foreach (var cpos in cellLayer.CellRegion)
				matrix[cpos.X - cellBounds.Left, cpos.Y - cellBounds.Top] = cellLayer[cpos];
			return matrix;
		}

		/// <summary>Wrapper around MatrixUtils.BordersToPoints in CPos space.</summary>
		public static CPos[][] BordersToPoints(CellLayer<bool> cellLayer, CellLayer<bool> mask = null)
		{
			if (mask != null)
			{
				if (!AreSameShape(cellLayer, mask))
					throw new ArgumentException("cellLayer and mask must have same shape.");
			}
			else
			{
				mask = new CellLayer<bool>(cellLayer.GridType, cellLayer.Size);
				mask.Clear(true);
			}

			var matrix = ToMatrix(cellLayer, false);
			var maskMatrix = ToMatrix(mask, false);
			var matrixPoints = MatrixUtils.BordersToPoints(matrix, maskMatrix);
			return FromMatrixPoints(matrixPoints, cellLayer);
		}

		/// <summary>Wrapper around MatrixUtils.ChebyshevRoom in CPos space.</summary>
		public static void ChebyshevRoom(
			CellLayer<int> output,
			CellLayer<bool> input,
			bool outsideValue)
		{
			var matrix = ToMatrix(input, outsideValue);
			var roominess = MatrixUtils.ChebyshevRoom(matrix, outsideValue);
			FromMatrix(output, roominess);
		}

		/// <summary>
		/// Wrapper around MatrixUtils.WalkingDistance in CPos space.
		/// Returns world distances (1024ths).
		/// </summary>
		public static void WalkingDistances(
			CellLayer<int> distances,
			CellLayer<bool> passable,
			IEnumerable<CPos> seeds,
			int maxDistance)
		{
			var passableMatrix = ToMatrix(passable, false);
			var cellBounds = CellBounds(passable);
			var int2Seeds = seeds
				.Select(cpos => new int2(cpos.X - cellBounds.Left, cpos.Y - cellBounds.Top));
			var distancesMatrix = MatrixUtils.WalkingDistances(passableMatrix, int2Seeds, maxDistance);
			FromMatrix(distances, distancesMatrix);
		}

		/// <summary>
		/// Rank all cell values and select the best (greatest compared) value.
		/// If there are equally good best candidates, choose one at random.
		/// </summary>
		public static (MPos MPos, T Value) FindRandomBest<T>(
			CellLayer<T> cellLayer,
			MersenneTwister random,
			Comparison<T> comparison)
		{
			var candidates = new List<MPos>();
			var best = cellLayer[new MPos(0, 0)];
			foreach (var mpos in cellLayer.CellRegion.MapCoords)
			{
				var rank = comparison(cellLayer[mpos], best);
				if (rank > 0)
				{
					best = cellLayer[mpos];
					candidates.Clear();
				}

				if (rank >= 0)
					candidates.Add(mpos);
			}

			var choice = candidates[random.Next(candidates.Count)];
			return (choice, best);
		}

		/// <summary>
		/// Pick a random MPos position in a CellLayer where each cell is a
		/// selection weight.
		/// </summary>
		public static MPos PickWeighted(CellLayer<int> weights, MersenneTwister random)
		{
			var entries = Entries(weights);
			var choice = random.PickWeighted(entries);
			var v = Math.DivRem(choice, weights.Size.Width, out var u);
			return new MPos(u, v);
		}

		/// <summary>
		/// <para>
		/// Perform a generic flood fill starting at seeds <c>[(cpos, prop), ...]</c>.
		/// </para>
		/// <para>
		/// For each point being considered for fill, <c>filler(cpos, prop)</c> is
		/// called with the current position (cpos) and propagation value (prop).
		/// filler should return the value to be propagated or null if not to be
		/// propagated. Propagation happens to all neighbours (offsets) defined
		/// by spread, regardless of whether they have previously been visited,
		/// so filler is responsible for terminating propagation by returning
		/// nulls. Usually, <c>Direction.Spread4CVec</c> or <c>Direction.Spread8CVec</c>
		/// is appropriate as a spread pattern.
		/// </para>
		/// <para>
		/// filler should capture and manipulate any necessary input and output
		/// arrays.
		/// </para>
		/// <para>
		/// Each call to filler will have either an equal or greater
		/// growth/propagation distance from their seed value than all calls
		/// before it. (You can think of this as them being called in ordered
		/// growth layers.)
		/// </para>
		/// <para>
		/// Note that filler may be called multiple times for the same spot,
		/// perhaps with different propagation values. Within the same
		/// growth/propagation distance, filler will be called from values
		/// propagated from earlier seeds before values propagated from later
		/// seeds.
		/// </para>
		/// <para>
		/// filler is not called for positions outside of cellLayer EXCEPT for
		/// points being processed as seed values.
		/// </para>
		/// </summary>
		public static void FloodFill<T, P>(
			CellLayer<T> cellLayer,
			IEnumerable<(CPos CPos, P Prop)> seeds,
			Func<CPos, P, P?> filler,
			ImmutableArray<CVec> spread) where P : struct
		{
			var next = seeds.ToList();
			while (next.Count != 0)
			{
				var current = next;
				next = new List<(CPos, P)>();
				foreach (var (source, prop) in current)
				{
					var newProp = filler(source, prop);
					if (newProp != null)
						foreach (var offset in spread)
						{
							var destination = source + offset;
							if (cellLayer.Contains(destination))
								next.Add((destination, (P)newProp));
						}
				}
			}
		}
	}
}
