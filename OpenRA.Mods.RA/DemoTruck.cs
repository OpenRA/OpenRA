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
	class DemoTruckInfo : TraitInfo<DemoTruck>, Requires<ExplodesInfo> { }

	class DemoTruck : IIssueOrder, IResolveOrder, IOrderVoice
	{
		static void Explode(Actor self)
		{
			self.World.AddFrameEndTask(w => self.InflictDamage(self, int.MaxValue, null));
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new TargetTypeOrderTargeter(new[] { "DetonateAttack" }, OrderCode.DetonateAttack, 5, "attack", true, false) { ForceAttack = false };
				yield return new DeployOrderTargeter(OrderCode.Detonate, 5);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID != OrderCode.DetonateAttack && order.OrderID != OrderCode.Detonate)
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
			if (order.ID == OrderCode.DetonateAttack)
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

			else if (order.ID == OrderCode.Detonate)
				Explode(self);
		}
	}
}
