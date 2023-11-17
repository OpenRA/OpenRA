#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;

namespace OpenRA.Mods.Common.Traits
{
	public class ParallelProductionQueueInfo : ProductionQueueInfo
	{
		public override object Create(ActorInitializer init) { return new ParallelProductionQueue(init, this); }
	}

	public class ParallelProductionQueue : ProductionQueue
	{
		public ParallelProductionQueue(ActorInitializer init, ParallelProductionQueueInfo info)
			: base(init, info) { }

		protected override void TickInner(Actor self, bool allProductionPaused)
		{
			CancelUnbuildableItems();

			var item = Queue.FirstOrDefault(i => !i.Paused);
			if (item == null)
				return;

			var before = item.RemainingTime;
			item.Tick(playerResources);

			if (item.RemainingTime == before)
				return;

			// As we have progressed this actor type, we will move all queued items of this actor to the end.
			foreach (var other in Queue.FindAll(a => a.Item == item.Item))
			{
				Queue.Remove(other);
				Queue.Add(other);
			}
		}

		public override bool IsProducing(ProductionItem item)
		{
			return Queue.Contains(item);
		}

		protected override void BeginProduction(ProductionItem item, bool hasPriority)
		{
			// Ignore `hasPriority` as it's not relevant in parallel production context.
			base.BeginProduction(item, false);
		}

		public override int RemainingTimeActual(ProductionItem item)
		{
			var parallelBuilds = Queue.FindAll(i => !i.Paused && !i.Done)
				.GroupBy(i => i.Item)
				.ToList()
				.Count;
			return item.RemainingTimeActual * parallelBuilds;
		}
	}
}
