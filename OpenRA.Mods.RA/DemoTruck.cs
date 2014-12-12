#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System.Collections.Generic;
using OpenRA.Traits;
using OpenRA.Mods.RA.Orders;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.Common.Orders;

namespace OpenRA.Mods.RA
{
	class DemoTruckInfo : ITraitInfo, Requires<ExplodesInfo>
	{
		[Desc("Acceptable stances of target's owner.")]
		public readonly Stance TargetPlayers = Stance.Enemy | Stance.Neutral;

		public object Create(ActorInitializer init) { return new DemoTruck(this); }
	}

	class DemoTruck : IIssueOrder, IResolveOrder, IOrderVoice
	{
		readonly DemoTruckInfo info;

		public DemoTruck(DemoTruckInfo info) { this.info = info; }

		static void Explode(Actor self)
		{
			self.World.AddFrameEndTask(w => self.InflictDamage(self, int.MaxValue, null));
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new TargetTypeOrderTargeter(new[] { "DetonateAttack" }, "DetonateAttack", 5, "attack", info.TargetPlayers) { ForceAttack = false };
				yield return new DeployOrderTargeter("Detonate", 5);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID != "DetonateAttack" && order.OrderID != "Detonate")
				return null;

			if (target.Type == TargetType.FrozenActor)
				return new Order(order.OrderID, self, queued) { ExtraData = target.FrozenActor.ID };

			return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return "Attack";
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "DetonateAttack")
			{
				var target = self.ResolveFrozenActorOrder(order, Color.Red);
				if (target.Type != TargetType.Actor)
					return;

				if (!order.Queued)
					self.CancelActivity();

				self.SetTargetLine(target, Color.Red);
				self.QueueActivity(new MoveAdjacentTo(self, target));
				self.QueueActivity(new CallFunc(() => Explode(self)));
			}

			else if (order.OrderString == "Detonate")
				Explode(self);
		}
	}
}
