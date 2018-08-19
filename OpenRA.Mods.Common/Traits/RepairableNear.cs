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

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	// DEPRECATED: This trait will soon be merged into Repairable
	public class RepairableNearInfo : RepairableInfo
	{
		public override object Create(ActorInitializer init) { return new RepairableNear(init.Self, this); }
	}

	public class RepairableNear : Repairable
	{
		public RepairableNear(Actor self, RepairableNearInfo info)
			: base(self, info) { }

		protected override string OrderString { get { return "RepairNear"; } }

		protected override void ResolveOrder(Actor self, Order order)
		{
			// RepairNear orders are only valid for own/allied actors,
			// which are guaranteed to never be frozen.
			if (order.OrderString != OrderString || order.Target.Type != TargetType.Actor)
				return;

			// Aircraft handle Repair orders directly in the Aircraft trait
			if (self.Info.HasTraitInfo<AircraftInfo>())
				return;

			var canRepairAtTarget = CanRepairAt(order.Target.Actor) && CanRepair();
			var canRearmAtTarget = CanRearmAt(order.Target.Actor) && CanRearm();
			if (!canRepairAtTarget && !canRearmAtTarget)
				return;

			if (!order.Queued)
				self.CancelActivity();

			self.QueueActivity(Movement.MoveWithinRange(order.Target, Info.CloseEnough, targetLineColor: Color.Green));

			if (canRearmAtTarget)
				self.QueueActivity(new Rearm(self, order.Target.Actor, Info.CloseEnough));

			if (canRepairAtTarget)
				self.QueueActivity(new Repair(self, order.Target.Actor, Info.CloseEnough));

			self.SetTargetLine(order.Target, Color.Green, false);
		}
	}
}
