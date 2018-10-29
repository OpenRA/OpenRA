#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.AI
{
	public sealed class BotOrderManagerInfo : ITraitInfo
	{
		[Desc("Minimum portion of pending orders to issue each tick (e.g. 5 issues at least 1/5th of all pending orders). Excess orders remain queued for subsequent ticks.")]
		public readonly int MinOrderQuotientPerTick = 5;

		public object Create(ActorInitializer init) { return new BotOrderManager(this); }
	}

	public sealed class BotOrderManager : ITick
	{
		readonly BotOrderManagerInfo info;
		readonly Queue<Order> orders = new Queue<Order>();

		public BotOrderManager(BotOrderManagerInfo info)
		{
			this.info = info;
		}

		public void QueueOrder(Order order)
		{
			orders.Enqueue(order);
		}

		void IssueOrders(World world)
		{
			var ordersToIssueThisTick = Math.Min((orders.Count + info.MinOrderQuotientPerTick - 1) / info.MinOrderQuotientPerTick, orders.Count);
			for (var i = 0; i < ordersToIssueThisTick; i++)
				world.IssueOrder(orders.Dequeue());
		}

		void ITick.Tick(Actor self)
		{
			// Make sure we tick after all of the bot modules so that we don't introduce an additional tick delay
			self.World.AddFrameEndTask(IssueOrders);
		}
	}
}
