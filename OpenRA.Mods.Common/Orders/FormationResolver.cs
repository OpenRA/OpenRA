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
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Orders
{
	public static class FormationResolver
	{
		public static bool ShouldApply(FormationType formation, int actorCount)
		{
			return formation != FormationType.Default && actorCount >= 2;
		}

		public static Dictionary<Actor, CPos> AssignDestinations(
			World world, Actor[] actors, CPos anchorCell, FormationType formation)
		{
			var result = new Dictionary<Actor, CPos>();
			if (actors.Length == 0)
				return result;

			if (!ShouldApply(formation, actors.Length))
			{
				foreach (var a in actors)
					result[a] = anchorCell;

				return result;
			}

			var spacing = GetSpacing(actors);
			var facing = GetFormationFacing(formation);
			var localOffsets = FormationLayout.GetOffsets(formation, actors.Length, spacing);
			var assignments = AssignSlotsInStableOrder(actors, localOffsets.Length);

			foreach (var kv in assignments)
				result[kv.Key] = anchorCell + RotateOffset(localOffsets[kv.Value], facing);

			return result;
		}

		public static Dictionary<Actor, CVec> AssignLocalOffsets(Actor[] actors, FormationType formation)
		{
			var result = new Dictionary<Actor, CVec>();
			if (actors.Length == 0)
				return result;

			if (!ShouldApply(formation, actors.Length))
			{
				foreach (var a in actors)
					result[a] = CVec.Zero;

				return result;
			}

			var spacing = GetSpacing(actors);
			var localOffsets = FormationLayout.GetOffsets(formation, actors.Length, spacing);
			var assignments = AssignSlotsInStableOrder(actors, localOffsets.Length);

			foreach (var kv in assignments)
				result[kv.Key] = localOffsets[kv.Value];

			return result;
		}

		static Dictionary<Actor, int> AssignSlotsInStableOrder(Actor[] actors, int slotCount)
		{
			var sorted = actors.OrderBy(a => a.ActorID).ToArray();
			var result = new Dictionary<Actor, int>();
			for (var i = 0; i < sorted.Length && i < slotCount; i++)
				result[sorted[i]] = i;

			return result;
		}

		static WAngle GetFormationFacing(FormationType formation)
		{
			return FormationLayout.AdjustFacing(formation, WAngle.Zero);
		}

		public static CVec RotateOffset(CVec localOffset, WAngle facing)
		{
			var forward = new WVec(0, -1024, 0).Rotate(WRot.FromYaw(facing));
			var right = new WVec(1024, 0, 0).Rotate(WRot.FromYaw(facing));
			var world = right * localOffset.X + forward * (-localOffset.Y);
			return new CVec((world.X + 512) / 1024, (-world.Y + 512) / 1024);
		}

		public static CVec UnrotateOffset(CVec worldOffset, WAngle facing)
		{
			var forward = new WVec(0, -1024, 0).Rotate(WRot.FromYaw(facing));
			var right = new WVec(1024, 0, 0).Rotate(WRot.FromYaw(facing));
			var world = new WVec(worldOffset.X * 1024, -worldOffset.Y * 1024, 0);

			var localX = (world.X * right.X + world.Y * right.Y) / (1024 * 1024);
			var localY = -(world.X * forward.X + world.Y * forward.Y) / (1024 * 1024);
			return new CVec(localX, localY);
		}

		public static WPos OffsetWorldPosition(WPos center, CVec localOffset, WAngle facing)
		{
			var rotated = RotateOffset(localOffset, facing);
			var cellOffset = new WVec(rotated.X * 1024, -rotated.Y * 1024, 0);
			return center + cellOffset;
		}

		public static void ApplyImmediateFormation(World world, IEnumerable<Actor> actors, FormationType formation)
		{
			var units = actors
				.Where(a => a.Owner == world.LocalPlayer && a.IsInWorld && !a.IsDead && a.Info.HasTraitInfo<IMoveInfo>())
				.ToArray();

			if (!ShouldApply(formation, units.Length))
				return;

			var anchor = GetCentroid(units);
			var destinations = AssignDestinations(world, units, anchor, formation);
			var localOffsets = AssignLocalOffsets(units, formation);

			foreach (var unit in units)
			{
				if (!destinations.TryGetValue(unit, out var cell) || unit.Location == cell)
					continue;

				var order = new Order("Move", unit, Target.FromCell(world, cell), false);
				if (localOffsets.TryGetValue(unit, out var offset) && offset != CVec.Zero)
					order.ExtraLocation = new CPos(offset.X, offset.Y);

				world.IssueOrder(order);
			}
		}

		static CPos GetCentroid(Actor[] actors)
		{
			if (actors.Length == 0)
				return CPos.Zero;

			var x = 0;
			var y = 0;
			foreach (var a in actors)
			{
				x += a.Location.X;
				y += a.Location.Y;
			}

			return new CPos(x / actors.Length, y / actors.Length);
		}

		static int GetSpacing(Actor[] actors)
		{
			var max = 1;
			foreach (var a in actors)
			{
				if (a.OccupiesSpace == null)
					continue;

				var cells = a.OccupiesSpace.OccupiedCells().Select(p => p.Cell).ToArray();
				if (cells.Length == 0)
				{
					// Airborne aircraft have no landing influence but still need formation spacing.
					if (a.Info.HasTraitInfo<AircraftInfo>())
						max = Math.Max(max, 2);

					continue;
				}

				var width = cells.Max(c => c.X) - cells.Min(c => c.X) + 1;
				var height = cells.Max(c => c.Y) - cells.Min(c => c.Y) + 1;
				max = Math.Max(max, Math.Max(width, height));
			}

			return FormationPreferences.SelectedSpacing.Apply(max);
		}

		public static IEnumerable<Order> ApplyToMoveOrders(World world, CPos anchorCell, IEnumerable<Order> orders)
		{
			var orderList = orders.ToList();
			var moveOrders = orderList.Where(o => o.OrderString == "Move" && o.Target.Type == TargetType.Terrain).ToList();
			if (moveOrders.Count < 2 || !ShouldApply(FormationPreferences.Selected, moveOrders.Count))
				return orderList;

			var actors = moveOrders.Select(o => o.Subject).ToArray();
			var destinations = AssignDestinations(world, actors, anchorCell, FormationPreferences.Selected);
			var localOffsets = AssignLocalOffsets(actors, FormationPreferences.Selected);

			return orderList.Select(o =>
			{
				if (o.OrderString == "Move" && destinations.TryGetValue(o.Subject, out var cell))
				{
					var order = new Order("Move", o.Subject, Target.FromCell(world, cell), o.Queued);
					if (localOffsets.TryGetValue(o.Subject, out var offset) && offset != CVec.Zero)
						order.ExtraLocation = new CPos(offset.X, offset.Y);

					return order;
				}

				return o;
			});
		}
	}
}
