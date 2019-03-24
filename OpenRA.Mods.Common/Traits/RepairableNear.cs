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
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	class RepairableNearInfo : ITraitInfo, Requires<IHealthInfo>, Requires<IMoveInfo>
	{
		[FieldLoader.Require]
		[ActorReference] public readonly HashSet<string> RepairActors = new HashSet<string> { };

		public readonly WDist CloseEnough = WDist.FromCells(4);
		[VoiceReference] public readonly string Voice = "Action";

		public object Create(ActorInitializer init) { return new RepairableNear(init.Self, this); }
	}

	class RepairableNear : IIssueOrder, IResolveOrder, IOrderVoice
	{
		public readonly RepairableNearInfo Info;
		readonly Actor self;
		readonly IMove movement;

		public RepairableNear(Actor self, RepairableNearInfo info)
		{
			this.self = self;
			Info = info;
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
				return new Order(order.OrderID, self, target, queued);

			return null;
		}

		bool CanRepairAt(Actor target)
		{
			return Info.RepairActors.Contains(target.Info.Name);
		}

		bool ShouldRepair()
		{
			return self.GetDamageState() > DamageState.Undamaged;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "RepairNear" && ShouldRepair() ? Info.Voice : null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			// RepairNear orders are only valid for own/allied actors,
			// which are guaranteed to never be frozen.
			if (order.OrderString != "RepairNear" || order.Target.Type != TargetType.Actor)
				return;

			if (!CanRepairAt(order.Target.Actor) || !ShouldRepair())
				return;

			if (!order.Queued)
				self.CancelActivity();

			self.QueueActivity(movement.MoveWithinRange(order.Target, Info.CloseEnough, targetLineColor: Color.Green));
			self.QueueActivity(new Resupply(self, order.Target.Actor, Info.CloseEnough));

			self.SetTargetLine(order.Target, Color.Green, false);
		}

		public Actor FindRepairBuilding(Actor self)
		{
			var repairBuilding = self.World.ActorsWithTrait<RepairsUnits>()
				.Where(a => !a.Actor.IsDead && a.Actor.IsInWorld
					&& a.Actor.Owner.IsAlliedWith(self.Owner) &&
					Info.RepairActors.Contains(a.Actor.Info.Name))
				.OrderBy(p => (self.Location - p.Actor.Location).LengthSquared);

			// Worst case FirstOrDefault() will return a TraitPair<null, null>, which is OK.
			return repairBuilding.FirstOrDefault().Actor;
		}
	}
}
