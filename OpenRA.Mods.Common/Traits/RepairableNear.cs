#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	class RepairableNearInfo : ITraitInfo, Requires<HealthInfo>, Requires<IMoveInfo>
	{
		[ActorReference] public readonly HashSet<string> Buildings = new HashSet<string> { "spen", "syrd" };
		public readonly WDist CloseEnough = WDist.FromCells(4);
		[VoiceReference] public readonly string Voice = "Action";

		public object Create(ActorInitializer init) { return new RepairableNear(init.Self, this); }
	}

	class RepairableNear : IIssueOrder, IResolveOrder, IOrderVoice
	{
		readonly Actor self;
		readonly RepairableNearInfo info;
		readonly IMove movement;

		public RepairableNear(Actor self, RepairableNearInfo info)
		{
			this.self = self;
			this.info = info;
			movement = self.Trait<IMove>();
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new EnterAlliedActorTargeter<BuildingInfo>("RepairNear", 5,
					target => CanRepairAt(target), _ => ShouldRepair());
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "RepairNear")
				return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };

			return null;
		}

		bool CanRepairAt(Actor target)
		{
			return info.Buildings.Contains(target.Info.Name);
		}

		bool ShouldRepair()
		{
			return self.GetDamageState() > DamageState.Undamaged;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return (order.OrderString == "RepairNear" && ShouldRepair()) ? info.Voice : null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "RepairNear" && CanRepairAt(order.TargetActor) && ShouldRepair())
			{
				var target = Target.FromOrder(self.World, order);

				self.CancelActivity();
				self.QueueActivity(movement.MoveWithinRange(target, info.CloseEnough));
				self.QueueActivity(new Repair(order.TargetActor));

				self.SetTargetLine(target, Color.Green, false);
			}
		}
	}
}
