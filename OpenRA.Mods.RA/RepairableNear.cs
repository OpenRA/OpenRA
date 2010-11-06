#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Move;
using OpenRA.Mods.RA.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class RepairableNearInfo : ITraitInfo, ITraitPrerequisite<HealthInfo>
	{
		[ActorReference]
		public readonly string[] Buildings = { "spen", "syrd" };

		public object Create( ActorInitializer init ) { return new RepairableNear( init.self ); }
	}

	class RepairableNear : IIssueOrder, IResolveOrder
	{
		readonly Actor self;

		public RepairableNear( Actor self ) { this.self = self; }

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new EnterOrderTargeter<Building>( "RepairNear", 5, false, true,
					target => CanRepairAt( target ), _ => ShouldRepair() );
			}
		}

		public Order IssueOrder( Actor self, IOrderTargeter order, Target target )
		{
			if( order.OrderID == "RepairNear" )
				return new Order( order.OrderID, self, target.Actor );

			return null;
		}

		bool CanRepairAt( Actor target )
		{
			return self.Info.Traits.Get<RepairableNearInfo>().Buildings.Contains( target.Info.Name );
		}

		bool ShouldRepair()
		{
			return self.GetDamageState() > DamageState.Undamaged;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "RepairNear" && CanRepairAt(order.TargetActor) && ShouldRepair())
			{
				var mobile = self.Trait<Mobile>();
				self.CancelActivity();
				self.QueueActivity(mobile.MoveWithinRange(order.TargetActor, 1));
				if (self.Owner == self.World.LocalPlayer)
					self.World.AddFrameEndTask( w =>
					{
						if (self.Destroyed) return;
						var line = self.TraitOrDefault<DrawLineToTarget>();
						if (line != null)
							line.SetTargetSilently(self, Target.FromActor(order.TargetActor), Color.Green);
					});
				self.QueueActivity(new Repair(order.TargetActor));
			}
		}
	}
}
