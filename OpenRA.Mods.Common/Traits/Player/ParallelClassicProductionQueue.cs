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

namespace OpenRA.Mods.Common.Traits
{
	public class ParallelClassicProductionQueueInfo : ClassicProductionQueueInfo
	{
		public override object Create(ActorInitializer init) { return new ParallelClassicProductionQueue(init, this); }
	}

	public class ParallelClassicProductionQueue : ClassicProductionQueue
	{
		[Sync] public new bool CurrentDone { get { return Queue.Find(i => i.Done) != null; } }

		public ParallelClassicProductionQueue(ActorInitializer init, ClassicProductionQueueInfo info) : base(init, info)
		{
		}

		protected override void ProgressQueue()
		{
			for (var i = 0; i < Queue.Count;)
			{
				var first = Queue[0];

				var before = first.RemainingTime;
				first.Tick(playerResources);

				foreach (var item in Queue.FindAll(a => a.Item == first.Item))
				{
					++i;
					Queue.Remove(item);
					Queue.Add(item);
				}

				if (first.RemainingTime != before)
					return;
			}
		}

		protected override void PauseProduction(string itemName, uint extraData)
		{
			var item = Queue.Find(a => a.Item == itemName);

			if (item != null)
				item.Pause(extraData != 0);
		}

		public override bool IsProducing(ProductionItem item)
		{
			return Queue.Contains(item);
		}

		public override int RemainingTimeActual(ProductionItem item)
		{
			var parallelBuilds = 0;
			var added = new List<string>();

			foreach (var i in Queue)
			{
				if (added.Contains(i.Item))
					continue;

				added.Add(i.Item);

				if (!i.Paused && i.RemainingTime > 0)
					++parallelBuilds;
			}

			return item.RemainingTimeActual * parallelBuilds;
		}
	}
}