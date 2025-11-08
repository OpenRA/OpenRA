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

namespace OpenRA.Mods.Common.MapGenerator
{
	public static class Symmetry
	{
		/// <summary>
		/// Trivial mirroring configurations. These are not tied to a specific coordinate system.
		/// </summary>
		public enum Mirror
		{
			/// <summary>No mirror.</summary>
			None = 0,

			/// <summary>Match low X with high X.</summary>
			LeftMatchesRight = 1,

			/// <summary>Match low X, low Y with high X, high Y.</summary>
			TopLeftMatchesBottomRight = 2,

			/// <summary>Match low Y with high Y.</summary>
			TopMatchesBottom = 3,

			/// <summary>Match low X, high Y with high X, low Y.</summary>
			TopRightMatchesBottomLeft = 4,
		}

		/// <summary>
		/// Wraps a mirror defined in terms of WPos space and provides conversions to appropriate
		/// coordinate systems.
		/// </summary>
		public readonly struct WMirror
		{
			readonly Mirror mirror;
			readonly MapGridType gridType;

			/// <summary>Create a WMirror.</summary>
			/// <param name="mirror">Mirror relative to WPos space.</param>
			/// <param name="gridType">Map grid type used for conversions.</param>
			public WMirror(Mirror mirror, MapGridType gridType)
			{
				this.mirror = mirror;
				this.gridType = gridType;
			}

			public bool HasMirror => mirror != Mirror.None;

			/// <summary>Return a Mirror relative to the WPos coordinate system.</summary>
			public Mirror ForWPos()
			{
				return mirror;
			}

			/// <summary>Return a Mirror relative to the CPos coordinate system.</summary>
			public Mirror ForCPos()
			{
				if (mirror == Mirror.None)
					return Mirror.None;

				if (gridType == MapGridType.RectangularIsometric)
					return (Mirror)((((int)mirror + 2) & 0b11) + 1);

				return mirror;
			}
		}

		public static bool TryParseMirror(string s, out Mirror mirror) => Enum.TryParse(s, out mirror);

		/// <summary>
		/// <para>
		/// Mirrors a (zero-area) point around a given center.
		/// </para>
		/// <para>
		/// For example, if using a center of (40, 40) a point at (1, 1) could be projected
		/// to (1, 1), (1, 79), (79, 1), or (79, 79).
		/// </para>
		/// </summary>
		public static int2 MirrorPointAround(Mirror mirror, int2 original, int2 center)
		{
			switch (mirror)
			{
				case Mirror.None:
					throw new ArgumentException("Mirror.None has no transformed point");
				case Mirror.LeftMatchesRight:
					return new int2(2 * center.X - original.X, original.Y);
				case Mirror.TopLeftMatchesBottomRight:
					return new int2(
						center.Y - original.Y + center.X,
						center.X - original.X + center.Y);
				case Mirror.TopMatchesBottom:
					return new int2(original.X, 2 * center.Y - original.Y);
				case Mirror.TopRightMatchesBottomLeft:
					return new int2(
						center.X + original.Y - center.Y,
						center.Y + original.X - center.X);
				default:
					throw new ArgumentException("Bad mirror");
			}
		}

		public static WPos MirrorWPosAround(WMirror wmirror, WPos original, WPos center)
		{
			var result = MirrorPointAround(
				wmirror.ForWPos(),
				new int2(original.X, original.Y),
				new int2(center.X, center.Y));
			return new WPos(result.X, result.Y, original.Z);
		}

		/// <summary>
		/// Given rotation and mirror parameters, return the total number of projected points this
		/// would result in (including the original point).
		/// </summary>
		public static int RotateAndMirrorProjectionCount(int rotations, Mirror mirror)
			=> mirror == Mirror.None ? rotations : rotations * 2;

		public static WPos[] RotateAndMirrorWPosAround(
			WPos original,
			WPos center,
			int rotations,
			WMirror wmirror)
		{
			var projections = new WPos[RotateAndMirrorProjectionCount(rotations, wmirror.ForWPos())];
			var projectionIndex = 0;

			for (var rotation = 0; rotation < rotations; rotation++)
			{
				// This could be made more accurate using dedicated, higher precision
				// rotation count to cos and sin lookup tables.
				var wangle = new WAngle(rotation * 1024 / rotations);
				var cos1024 = wangle.Cos();
				var sin1024 = wangle.Sin();
				var relOrig = original - center;
				var projX = (relOrig.X * cos1024 - relOrig.Y * sin1024) / 1024 + center.X;
				var projY = (relOrig.X * sin1024 + relOrig.Y * cos1024) / 1024 + center.Y;
				var projection = new WPos(projX, projY, original.Z);
				projections[projectionIndex++] = projection;

				if (wmirror.HasMirror)
					projections[projectionIndex++] = MirrorWPosAround(wmirror, projection, center);
			}

			return projections;
		}

		public static WPos[] RotateAndMirrorWPos<T>(
			WPos original,
			CellLayer<T> cellLayer,
			int rotations,
			WMirror wmirror)
		{
			return RotateAndMirrorWPosAround(
				original,
				CellLayerUtils.Center(cellLayer),
				rotations,
				wmirror);
		}

		public static CPos[] RotateAndMirrorCPos<T>(
			CPos original,
			CellLayer<T> cellLayer,
			int rotations,
			WMirror wmirror)
		{
			var cposProjections = new CPos[RotateAndMirrorProjectionCount(rotations, wmirror.ForWPos())];
			var wpos = CellLayerUtils.CPosToWPos(original, cellLayer.GridType);
			var wposProjections = RotateAndMirrorWPos(wpos, cellLayer, rotations, wmirror);
			for (var i = 0; i < wposProjections.Length; i++)
				cposProjections[i] = CellLayerUtils.WPosToCPos(wposProjections[i], cellLayer.GridType);
			return cposProjections;
		}

		/// <summary>
		/// Determine the shortest distance between projected positions.
		/// </summary>
		public static int ProjectionProximity(int2[] projections)
		{
			if (projections.Length == 1)
				return int.MaxValue;
			var worstSpacingSq = long.MaxValue;
			for (var i1 = 0; i1 < projections.Length; i1++)
				for (var i2 = 0; i2 < projections.Length; i2++)
				{
					if (i1 == i2)
						continue;
					var spacingSq = (projections[i1] - projections[i2]).LengthSquared;
					if (spacingSq < worstSpacingSq)
						worstSpacingSq = spacingSq;
				}

			return (int)Math.Sqrt(worstSpacingSq);
		}

		/// <summary>
		/// Determine the shortest distance between projected positions.
		/// </summary>
		public static int ProjectionProximity(CPos[] projections)
		{
			return ProjectionProximity(
				projections.Select(cpos => new int2(cpos.X, cpos.Y)).ToArray());
		}

		/// <summary>
		/// <para>
		/// Duplicate an original point into an array of projected points according to a rotation
		/// and mirror specification.
		/// </para>
		/// <para>
		/// Rotations use WAngel-based trigonometric math for consistency with other Symmetry
		/// functions. This may be slightly imprecise for non-trivial rotations.
		/// </para>
		/// <para>
		/// For example, if using a center of (40, 40) a point at (1, 1) could be projected
		/// to (1, 1), (1, 79), (79, 1), and (79, 79).
		/// </para>
		/// </summary>
		public static int2[] RotateAndMirrorPointAround(
			int2 original,
			int2 center,
			int rotations,
			Mirror mirror)
		{
			var projections = new int2[RotateAndMirrorProjectionCount(rotations, mirror)];
			var projectionIndex = 0;

			for (var rotation = 0; rotation < rotations; rotation++)
			{
				// This could be made more accurate using dedicated, higher precision
				// rotation count to cos and sin lookup tables.
				var wangle = new WAngle(rotation * 1024 / rotations);
				long cos = wangle.Cos();
				long sin = wangle.Sin();
				var relOrig = original - center;
				var projX = (relOrig.X * cos - relOrig.Y * sin) / 1024 + center.X;
				var projY = (relOrig.X * sin + relOrig.Y * cos) / 1024 + center.Y;
				var projection = new int2((int)projX, (int)projY);
				projections[projectionIndex++] = projection;

				if (mirror != Mirror.None)
					projections[projectionIndex++] = MirrorPointAround(mirror, projection, center);
			}

			return projections;
		}

		/// <summary>
		/// Rotate and mirror multiple actor plans. See RotateAndMirrorActorPlan.
		/// </summary>
		public static ImmutableArray<ActorPlan> RotateAndMirrorActorPlans(
			IReadOnlyList<ActorPlan> originals,
			int rotations,
			WMirror wmirror)
		{
			var projections = new List<ActorPlan>(
				originals.Count * RotateAndMirrorProjectionCount(rotations, wmirror.ForWPos()));
			foreach (var original in originals)
				projections.AddRange(RotateAndMirrorActorPlan(original, rotations, wmirror));

			return projections.ToImmutableArray();
		}

		/// <summary>
		/// Rotate and mirror a single actor plan, adding to an accumulator list.
		/// Locations (CPos) are necessarily snapped to grid.
		/// </summary>
		public static ImmutableArray<ActorPlan> RotateAndMirrorActorPlan(
			ActorPlan original,
			int rotations,
			WMirror wmirror)
		{
			var projections = new List<ActorPlan>(RotateAndMirrorProjectionCount(rotations, wmirror.ForWPos()));
			var points = RotateAndMirrorWPos(
				original.WPosCenterLocation,
				original.Map.Tiles,
				rotations,
				wmirror);
			foreach (var point in points)
			{
				var plan = original.Clone();
				plan.WPosCenterLocation = point;
				projections.Add(plan);
			}

			return projections.ToImmutableArray();
		}

		/// <summary>
		/// Calls action(projections, original) over all possible original
		/// CPos positions, where each projection in projections is a
		/// mirrored/rotated point. For non-trivial symmetries, projections may
		/// be outside the bounds defined by cellLayer.
		/// </summary>
		public static void RotateAndMirrorOverCPos<T>(
			CellLayer<T> cellLayer,
			int rotations,
			WMirror wmirror,
			Action<CPos[], CPos> action)
		{
			var size = cellLayer.Size;
			for (var v = 0; v < size.Height; v++)
				for (var u = 0; u < size.Width; u++)
				{
					var original = new MPos(u, v).ToCPos(cellLayer.GridType);
					var projections = RotateAndMirrorCPos(original, cellLayer, rotations, wmirror);
					action(projections, original);
				}
		}
	}
}
