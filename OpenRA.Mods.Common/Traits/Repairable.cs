#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor can be sent to a structure for repairs.")]
	class RepairableInfo : ITraitInfo, Requires<HealthInfo>, Requires<IMoveInfo>, Requires<DockClientInfo>
	{
		public readonly HashSet<string> RepairBuildings = new HashSet<string> { "fix" };

		[VoiceReference] public readonly string Voice = "Action";

		public virtual object Create(ActorInitializer init) { return new Repairable(init.Self, this); }
	}

	class Repairable : IIssueOrder, IResolveOrder, IOrderVoice
	{
		readonly RepairableInfo info;
		readonly Health health;
		readonly AmmoPool[] ammoPools;
		readonly DockClient dockClient;

		public Repairable(Actor self, RepairableInfo info)
		{
			this.info = info;
			health = self.Trait<Health>();
			ammoPools = self.TraitsImplementing<AmmoPool>().ToArray();
			dockClient = self.Trait<DockClient>();
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new EnterAlliedActorTargeter<BuildingInfo>("Repair", 5, CanRepairAt, _ => CanRepair() || CanRearm());
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "Repair")
				return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };

			return null;
		}

		bool CanRepairAt(Actor target)
		{
			return info.RepairBuildings.Contains(target.Info.Name);
		}

		bool CanRearmAt(Actor target)
		{
			return info.RepairBuildings.Contains(target.Info.Name);
		}

		bool CanRepair()
		{
			return health.DamageState > DamageState.Undamaged;
		}

		bool CanRearm()
		{
			return ammoPools.Any(x => !x.Info.SelfReloads && !x.FullAmmo());
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return (order.OrderString == "Repair" && CanRepair()) ? info.Voice : null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			dockClient.Release();

			if (order.OrderString == "Repair")
			{
				if (!CanRepairAt(order.TargetActor) || (!CanRepair() && !CanRearm()))
					return;

				var target = Target.FromOrder(self.World, order);
				self.SetTargetLine(target, Color.Green);

				var host = target.Actor;
				var dm = host.Trait<DockManager>();
				self.CancelActivity();
				dm.ReserveDock(host, self, new RepairDocking(self, order.TargetActor));
			}
		}

		public Activity AfterReachActivities(Actor self, Actor host, Dock dock)
		{
			if (CanRearmAt(host) && CanRearm())
				return ActivityUtils.SequenceActivities(
					new Rearm(self),
					new Repair(self, host, new WDist(512)));

			// Add a CloseEnough range of 512 to ensure we're at the host actor
			return new Repair(self, host, new WDist(512));
		}

		public Actor FindRepairBuilding(Actor self)
		{
			var repairBuilding = self.World.ActorsWithTrait<RepairsUnits>()
				.Where(a => !a.Actor.IsDead && a.Actor.IsInWorld
					&& a.Actor.Owner.IsAlliedWith(self.Owner) &&
					info.RepairBuildings.Contains(a.Actor.Info.Name))
				.OrderBy(p => (self.Location - p.Actor.Location).LengthSquared);

			// Worst case FirstOrDefault() will return a TraitPair<null, null>, which is OK.
			return repairBuilding.FirstOrDefault().Actor;
		}

		static void TryCallTransport(Actor self, Target target, Activity nextActivity)
		{
			var transport = self.TraitOrDefault<ICallForTransport>();
			if (transport == null)
				return;

			var targetCell = self.World.Map.CellContaining(target.CenterPosition);
			if ((self.CenterPosition - target.CenterPosition).LengthSquared < transport.MinimumDistance.LengthSquared)
				return;

			transport.RequestTransport(self, targetCell, nextActivity);
		}
	}
}
