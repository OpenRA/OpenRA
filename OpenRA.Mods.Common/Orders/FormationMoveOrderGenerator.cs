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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Orders
{
	public class FormationMoveOrderGenerator : UnitOrderGenerator
	{
		protected override MouseActionType ActionType => MouseActionType.ConfirmOrder;

		public FormationMoveOrderGenerator(World world)
			: base(world) { }

		protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			var queued = mi.Modifiers.HasModifier(Modifiers.Shift);
			if (!queued)
				world.CancelInputMode();

			cell = world.Map.Clamp(cell);

			var target = TargetForInput(world, cell, worldPixel, mi);
			var orders = world.Selection.Actors
				.Select(a => OrderForUnit(a, target, cell, mi))
				.Where(o => o != null)
				.ToList();

			var actorsInvolved = orders.Select(o => o.Actor).Distinct().ToArray();
			if (actorsInvolved.Length == 0)
				yield break;

			var moveOrders = orders.Where(x => x.Order.OrderID == "Move").ToList();
			uint groupSpeedCap = 0;
			var minSpeed = int.MaxValue;
			foreach (var mo in moveOrders)
			{
				var mobile = mo.Actor.TraitOrDefault<Mobile>();
				if (mobile != null && mobile.Info.Speed < minSpeed)
					minSpeed = mobile.Info.Speed;
			}

			if (moveOrders.Count > 1 && minSpeed > 0 && minSpeed < int.MaxValue)
				groupSpeedCap = (uint)minSpeed;

			CPos? selectionCenter = null;
			if (moveOrders.Count > 1 && groupSpeedCap > 0)
			{
				var sumX = 0;
				var sumY = 0;
				foreach (var mo in moveOrders)
				{
					sumX += mo.Actor.Location.X;
					sumY += mo.Actor.Location.Y;
				}

				selectionCenter = new CPos(sumX / moveOrders.Count, sumY / moveOrders.Count, cell.Layer);
			}

			yield return new Order("CreateGroup", actorsInvolved[0].Owner.PlayerActor, false, actorsInvolved);

			foreach (var o in orders)
			{
				var order = CheckSameOrder(o.Order, o.Trait.IssueOrder(o.Actor, o.Order, o.Target, queued));
				if (order == null)
					continue;

				if (order.OrderString == "Move" && groupSpeedCap > 0)
				{
					order.ExtraData = groupSpeedCap;
					if (selectionCenter != null)
					{
						var formationOffset = new CVec(
							o.Actor.Location.X - selectionCenter.Value.X,
							o.Actor.Location.Y - selectionCenter.Value.Y);
						var formationCell = world.Map.Clamp(new CPos(
							cell.X + formationOffset.X,
							cell.Y + formationOffset.Y,
							cell.Layer));
						order = new Order("Move", o.Actor, Target.FromCell(world, formationCell), queued)
						{
							ExtraData = groupSpeedCap
						};
					}
				}

				yield return order;
			}
		}

		public override void SelectionChanged(World world, IEnumerable<Actor> selected)
		{
			if (!selected.Any(s => !s.IsDead && s.Info.HasTraitInfo<MobileInfo>()))
				world.CancelInputMode();
		}

		public override bool InputOverridesSelection(World world, int2 xy, MouseInput mi)
		{
			return true;
		}

		public override bool ClearSelectionOnLeftClick => false;
	}
}
