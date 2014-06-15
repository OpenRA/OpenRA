#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class RepairableInfo : ITraitInfo, Requires<HealthInfo>
	{
		public readonly string[] RepairBuildings = { "fix" };
		public virtual object Create(ActorInitializer init) { return new Repairable(init.self); }
	}

	class Repairable : IIssueOrder, IResolveOrder, IOrderVoice
	{
		readonly Actor self;
		readonly Health Health;

		public Repairable(Actor self)
		{
			this.self = self;
			Health = self.Trait<Health>();
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new EnterAlliedActorTargeter<Building>("Repair", 5,
				target => CanRepairAt(target), _ => CanRepair()); }
		}

		public Order IssueOrder( Actor self, IOrderTargeter order, Target target, bool queued )
		{
			if( order.OrderID == "Repair" )
				return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };

			return null;
		}

		bool CanRepairAt( Actor target )
		{
			return self.Info.Traits.Get<RepairableInfo>().RepairBuildings.Contains( target.Info.Name );
		}

		bool CanRepair()
		{
			var li = self.TraitOrDefault<LimitedAmmo>();
			return (Health.DamageState > DamageState.Undamaged || (li != null && !li.FullAmmo()) );
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return (order.OrderString == "Repair" && CanRepair()) ? "Move" : null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Repair")
			{
				if( !CanRepairAt( order.TargetActor ) || !CanRepair() )
					return;

				var movement = self.Trait<IMove>();
				var target = Target.FromOrder(order);
				self.SetTargetLine(target, Color.Green);

				self.CancelActivity();
				self.QueueActivity(new MoveAdjacentTo(self, target));
				self.QueueActivity(movement.MoveTo(order.TargetActor.CenterPosition.ToCPos(), order.TargetActor));
				self.QueueActivity(new Rearm(self));
				self.QueueActivity(new Repair(order.TargetActor));

				var rp = order.TargetActor.TraitOrDefault<RallyPoint>();
				if (rp != null)
					self.QueueActivity(new CallFunc(() =>
					{
						self.SetTargetLine(Target.FromCell(rp.rallyPoint), Color.Green);
						self.QueueActivity(movement.MoveTo(rp.rallyPoint, order.TargetActor));
					}));
			}
		}
	}
}
