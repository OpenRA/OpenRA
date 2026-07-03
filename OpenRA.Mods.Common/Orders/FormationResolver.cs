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

using OpenRA.Primitives;

using OpenRA.Traits;



namespace OpenRA.Mods.Common.Orders

{

	public static class FormationResolver

	{

		const int MaxCellSearchRadius = 12;



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

			var facing = GetFormationFacing(world, actors, anchorCell, formation);

			var localOffsets = FormationLayout.GetOffsets(formation, actors.Length, spacing);

			var slots = localOffsets

				.Select(o => anchorCell + RotateOffset(o, facing))

				.ToArray();



			var assignments = GreedyAssignSlots(actors, slots);

			var reserved = new HashSet<CPos>();



			foreach (var kv in assignments.OrderBy(a => (slots[a.Value] - anchorCell).LengthSquared))

			{

				var validated = ValidateDestination(world, kv.Key, slots[kv.Value], reserved);

				result[kv.Key] = validated;

				reserved.Add(validated);

			}



			return result;

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



			foreach (var unit in units)

			{

				if (!destinations.TryGetValue(unit, out var cell) || unit.Location == cell)

					continue;



				world.IssueOrder(new Order("Move", unit, Target.FromCell(world, cell), false));

			}

		}



		static Dictionary<Actor, int> GreedyAssignSlots(Actor[] actors, CPos[] slots)

		{

			var unassignedUnits = actors.ToList();

			var unassignedSlots = Enumerable.Range(0, slots.Length).ToList();

			var result = new Dictionary<Actor, int>();



			while (unassignedUnits.Count > 0)

			{

				Actor bestUnit = null;

				var bestSlot = -1;

				var bestDistance = int.MaxValue;



				foreach (var unit in unassignedUnits)

				{

					foreach (var slotIndex in unassignedSlots)

					{

						var distance = (unit.Location - slots[slotIndex]).LengthSquared;

						if (distance < bestDistance)

						{

							bestDistance = distance;

							bestUnit = unit;

							bestSlot = slotIndex;

						}

					}

				}



				result[bestUnit] = bestSlot;

				unassignedUnits.Remove(bestUnit);

				unassignedSlots.Remove(bestSlot);

			}



			return result;

		}



		static WAngle GetFormationFacing(World world, Actor[] actors, CPos anchorCell, FormationType formation)

		{

			var centroid = GetCentroid(actors);

			if (centroid != anchorCell)

				return FormationLayout.AdjustFacing(formation, world.Map.FacingBetween(centroid, anchorCell, WAngle.Zero));



			var facings = actors

				.Select(a => a.TraitOrDefault<IFacing>())

				.Where(f => f != null)

				.Select(f => f.Facing)

				.ToArray();



			if (facings.Length == 0)

				return FormationLayout.AdjustFacing(formation, WAngle.Zero);



			long sin = 0;

			long cos = 0;

			foreach (var facing in facings)

			{

				sin += facing.Sin();

				cos += facing.Cos();

			}



			var average = WAngle.ArcTan((int)(sin / facings.Length), (int)(cos / facings.Length));

			return FormationLayout.AdjustFacing(formation, average);

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

			var slotCells = localOffsets

				.Select(o => new CPos(o.X, o.Y))

				.ToArray();



			var assignments = GreedyAssignSlots(actors, slotCells);



			foreach (var kv in assignments)

				result[kv.Key] = localOffsets[kv.Value];



			return result;

		}



		public static CVec RotateOffset(CVec localOffset, WAngle facing)

		{

			var forward = new WVec(0, -1024, 0).Rotate(WRot.FromYaw(facing));

			var right = new WVec(1024, 0, 0).Rotate(WRot.FromYaw(facing));

			var world = right * localOffset.X + forward * localOffset.Y;

			return new CVec((world.X + 512) / 1024, (-world.Y + 512) / 1024);

		}



		public static WPos OffsetWorldPosition(WPos center, CVec localOffset, WAngle facing)

		{

			var rotated = RotateOffset(localOffset, facing);

			var cellOffset = new WVec(rotated.X * 1024, -rotated.Y * 1024, 0);

			return center + cellOffset;

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

					continue;



				var width = cells.Max(c => c.X) - cells.Min(c => c.X) + 1;

				var height = cells.Max(c => c.Y) - cells.Min(c => c.Y) + 1;

				max = Math.Max(max, Math.Max(width, height));

			}



			return Math.Max(2, max + 1);

		}



		static CPos ValidateDestination(World world, Actor actor, CPos desired, HashSet<CPos> reserved)

		{

			if (IsAvailableDestination(world, actor, desired, reserved))

				return desired;



			for (var radius = 1; radius <= MaxCellSearchRadius; radius++)

			{

				foreach (var candidate in world.Map.FindTilesInCircle(desired, radius))

				{

					if (IsAvailableDestination(world, actor, candidate, reserved))

						return candidate;

				}

			}



			return desired;

		}



		static bool IsAvailableDestination(World world, Actor actor, CPos cell, HashSet<CPos> reserved)

		{

			if (reserved.Contains(cell) || !world.Map.Contains(cell))

				return false;



			var mobile = actor.TraitOrDefault<Mobile>();

			if (mobile != null && !mobile.IsTraitDisabled && !mobile.IsTraitPaused)

				return mobile.CanEnterCell(cell, check: BlockedByActor.Immovable) && mobile.CanStayInCell(cell);



			var aircraft = actor.TraitOrDefault<Aircraft>();

			if (aircraft != null && !aircraft.IsTraitDisabled && !aircraft.IsTraitPaused)

				return aircraft.CanLand(cell, blockedByMobile: false);



			return true;

		}



		public static IEnumerable<Order> ApplyToMoveOrders(World world, CPos anchorCell, IEnumerable<Order> orders)

		{

			var orderList = orders.ToList();

			if (!ShouldApply(FormationPreferences.Selected, orderList.Count(o => o.OrderString == "Move")))

				return orderList;



			var moveOrders = orderList.Where(o => o.OrderString == "Move" && o.Target.Type == TargetType.Terrain).ToList();

			if (moveOrders.Count < 2)

				return orderList;



			var actors = moveOrders.Select(o => o.Subject).ToArray();

			var destinations = AssignDestinations(world, actors, anchorCell, FormationPreferences.Selected);



			return orderList.Select(o =>

			{

				if (o.OrderString == "Move" && destinations.TryGetValue(o.Subject, out var cell))

					return new Order("Move", o.Subject, Target.FromCell(world, cell), o.Queued);



				return o;

			});

		}

	}

}


