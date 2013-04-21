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
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	// Exception when overriding Chronoshift event; removed for now, will look into it.
	class DemoTruckInfo : TraitInfo<DemoTruck>, Requires<ExplodesInfo> {}

	class DemoTruck : IIssueOrder, IResolveOrder, IOrderVoice
	{
		void Explode(Actor self)
		{
			self.World.AddFrameEndTask(w =>
			{
				self.InflictDamage(self, int.MaxValue, null);
			});
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new TargetTypeOrderTargeter("DemoTruck", "DemoAttack", 5, "attack", true, false) { ForceAttack = false };
				yield return new DeployOrderTargeter("DemoDeploy", 5);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "DemoAttack" || order.OrderID == "DemoDeploy")
				return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };

			return null;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return "Attack";
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "DemoAttack")
			{
				self.SetTargetLine(Target.FromOrder(order), Color.Red);
				self.World.AddFrameEndTask(w =>
				{
					self.QueueActivity(new MoveAdjacentTo(Target.FromOrder(order)));
					self.QueueActivity(new CallFunc(() => Explode(self)));
				});
			}
			if (order.OrderString == "DemoDeploy")
				Explode(self);
		}
	}
}
