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
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Tcd.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Tcd.Formations
{
	// Turns a set of actors into a formation: works out where each one should stand,
	// snaps those positions to cells they can actually occupy, and issues move orders.
	public static class FormationPlanner
	{
		public const int CellSize = 1024;
		const int DefaultRank = 1;

		// A full turn is 1024, so eight compass directions are 128 apart. Snapping the
		// formation's facing to these keeps offsets aligned to the cell grid; arbitrary
		// angles land between cells and the snapping leaves uneven gaps.
		const int FacingStep = 128;

		// spacingCells is in whole cells on purpose. Anything else lands between cells
		// and produces alternating one- and two-cell gaps once positions are snapped.
		// Returns the number of actors that were given a move order.
		public static int Apply(World world, IEnumerable<Actor> actors, FormationShape shape,
			int spacingCells, int maxRowWidth, WPos? faceToward = null)
		{
			var members = actors
				.Where(a => a.Owner == world.LocalPlayer && a.IsInWorld && !a.IsDead && a.TraitOrDefault<Mobile>() != null)
				.ToList();

			if (members.Count == 0)
				return 0;

			var spacing = Math.Max(1, spacingCells) * CellSize;
			var centre = Centroid(members);
			var rotation = WRot.FromYaw(Quantise(FacingFor(members, centre, faceToward)));

			// One cell to the formation's right, used to keep left-to-right order stable
			// so units walk to the nearest slot instead of crossing through each other.
			var right = new WVec(CellSize, 0, 0).Rotate(rotation);

			members = members
				.OrderBy(RankOf)
				.ThenBy(a => Lateral(a.CenterPosition - centre, right))
				.ToList();

			var ranks = members.ConvertAll(RankOf);
			var offsets = FormationShapes.Offsets(shape, ranks, spacing, maxRowWidth);

			return IssueMoves(world, members, offsets, centre, rotation);
		}

		// Places each actor at its offset, resolving collisions, and issues the moves.
		public static int IssueMoves(World world, IReadOnlyList<Actor> members, WVec[] offsets, WPos centre, WRot rotation)
		{
			var taken = new HashSet<CPos>();
			var ordered = 0;

			for (var i = 0; i < members.Count; i++)
			{
				var actor = members[i];
				var mobile = actor.TraitOrDefault<Mobile>();
				if (mobile == null)
					continue;

				var wanted = world.Map.CellContaining(centre + offsets[i].Rotate(rotation));
				var cell = FreeCellNear(mobile, wanted, taken);
				if (!taken.Add(cell))
					continue;

				world.IssueOrder(new Order("Move", actor, Target.FromCell(world, cell), false));
				ordered++;
			}

			return ordered;
		}

		// Nearest legal cell that nobody in this formation has claimed yet.
		static CPos FreeCellNear(Mobile mobile, CPos wanted, HashSet<CPos> taken)
		{
			var cell = mobile.NearestMoveableCell(wanted);
			if (!taken.Contains(cell))
				return cell;

			for (var ring = 1; ring <= 4; ring++)
			{
				for (var dy = -ring; dy <= ring; dy++)
				{
					for (var dx = -ring; dx <= ring; dx++)
					{
						// Only the outline of each ring, so we spiral outwards evenly.
						if (Math.Abs(dx) != ring && Math.Abs(dy) != ring)
							continue;

						var candidate = wanted + new CVec(dx, dy);
						if (taken.Contains(candidate) || !mobile.CanEnterCell(candidate))
							continue;

						return candidate;
					}
				}
			}

			return cell;
		}

		static WAngle Quantise(WAngle angle)
		{
			return new WAngle((angle.Angle + FacingStep / 2) / FacingStep * FacingStep);
		}

		static int RankOf(Actor a)
		{
			var info = a.Info.TraitInfoOrDefault<FormationRoleInfo>();
			return info?.Rank ?? DefaultRank;
		}

		public static WPos Centroid(IReadOnlyList<Actor> members)
		{
			long x = 0;
			long y = 0;
			foreach (var a in members)
			{
				var p = a.CenterPosition;
				x += p.X;
				y += p.Y;
			}

			return new WPos((int)(x / members.Count), (int)(y / members.Count), 0);
		}

		static WAngle FacingFor(List<Actor> members, WPos centre, WPos? faceToward)
		{
			if (faceToward == null)
				return AverageFacing(members);

			var delta = faceToward.Value - centre;
			if (delta.X == 0 && delta.Y == 0)
				return AverageFacing(members);

			// The local frame points forward along -Y, so north maps to yaw zero.
			return WAngle.ArcTan(delta.X, -delta.Y);
		}

		// Averages facings as unit vectors so that north-ish and south-ish do not cancel
		// into something arbitrary the way averaging raw angles would.
		static WAngle AverageFacing(List<Actor> members)
		{
			long x = 0;
			long y = 0;
			var found = 0;
			foreach (var a in members)
			{
				var facing = a.TraitOrDefault<IFacing>();
				if (facing == null)
					continue;

				x += facing.Facing.Cos();
				y += facing.Facing.Sin();
				found++;
			}

			if (found == 0 || (x == 0 && y == 0))
				return WAngle.Zero;

			return WAngle.ArcTan((int)y, (int)x);
		}

		static int Lateral(WVec v, WVec right)
		{
			return (v.X * right.X + v.Y * right.Y) / CellSize;
		}
	}
}
