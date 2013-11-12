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
using OpenRA.Mods.RA.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class C4DemolitionInfo : ITraitInfo
	{
		public readonly int C4Delay = 45; // 1.8 seconds
		public object Create(ActorInitializer init) { return new C4Demolition(this); }
	}

	class C4Demolition : IIssueOrder, IResolveOrder, IOrderVoice
	{
		readonly C4DemolitionInfo info;

		public C4Demolition(C4DemolitionInfo info)
		{
			this.info = info;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new TargetTypeOrderTargeter("C4", "C4", 6, "c4", true, false); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID != "C4")
				return null;

			if (target.Type == TargetType.FrozenActor)
				return new Order(order.OrderID, self, queued) { ExtraData = target.FrozenActor.ID };

			return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "C4")
				return;

			var target = self.ResolveFrozenActorOrder(order, Color.Red);
			if (target.Type != TargetType.Actor)
				return;

			if (!order.Queued)
				self.CancelActivity();

			self.SetTargetLine(target, Color.Red);
			self.QueueActivity(new Enter(target.Actor, new Demolish(target.Actor, info.C4Delay)));
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "C4" ? "Attack" : null;
		}
	}
}
