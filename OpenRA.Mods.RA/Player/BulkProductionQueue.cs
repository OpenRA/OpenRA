using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Traits;
using OpenRA.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Mods.RA
{
	class BulkProductionQueueInfo : ProductionQueueInfo, Requires<TechTreeInfo>, Requires<PowerManagerInfo>, Requires<PlayerResourcesInfo>
	{
		[Desc("Static production time if DynamicProductionTime isnt enabled")]
		public readonly int ProductionTime = 0;
		[Desc("Minimum amount of units that can be produced at one time")]
		public readonly int MinimumOrders = 1;
		[Desc("Maximum amount of units that can be produced at one time")]
		public readonly int MaximumOrders = 5;

		[Desc("Calculates production time according to which units are produced")]
		public readonly bool DynamicProductionTime = false;

		public override object Create(ActorInitializer init) { return new BulkProductionQueue(init, this); }
	}

	class BulkProductionQueue : ProductionQueue, ISync
	{
		static readonly ActorInfo[] NoItems = { };

		readonly Actor self;
		readonly BulkProductionQueueInfo info;

		protected List<ProductionItem> activeQueue = new List<ProductionItem>();

		public BulkProductionQueue(ActorInitializer init, BulkProductionQueueInfo info)
			: base(init, init.self, info)
		{
			this.self = init.self;
			this.info = info;
		}

		[Sync] bool isActive = false;
		int completedItems = 0;

		public override void Tick(Actor self)
		{
			isActive = false;
			foreach (var x in self.World.ActorsWithTrait<Production>())
			{
				if (x.Actor.Owner == self.Owner && x.Trait.Info.Produces.Contains(Info.Type))
				{
					var b = x.Actor.TraitOrDefault<Building>();
					if (b != null && b.Locked)
						continue;
					isActive = true;
					break;
				}
			}

			if (activeQueue.Count == 0) return;

			foreach (var item in activeQueue)
				item.Tick(playerResources);

			if (completedItems >= activeQueue.Count)
			{
				var hasPlayedSound = false;

				if (BuildUnits(activeQueue) && !hasPlayedSound)
					hasPlayedSound = Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", Info.ReadyAudio, self.Owner.Country.Race);
				
				completedItems = 0;
				FinishProduction();
			}
		}

		public override IEnumerable<ActorInfo> AllItems()
		{
			return isActive ? base.AllItems() : NoItems;
		}

		public override IEnumerable<ActorInfo> BuildableItems()
		{
			return isActive ? base.BuildableItems() : NoItems;
		}

		protected bool BuildUnits(IEnumerable<ProductionItem> items)
		{
			// Find a production structure to build this actor
			var producers = self.World.ActorsWithTrait<Production>()
				.Where(x => x.Actor.Owner == self.Owner
					&& x.Trait.Info.Produces.Contains(Info.Type))
					.OrderByDescending(x => x.Actor.IsPrimaryBuilding())
					.ThenByDescending(x => x.Actor.ActorID);

			if (!producers.Any())
			{
				FinishProduction();
				return true;
			}

			var ai = new List<ActorInfo>();
			foreach (var item in items)
			{
				ai.Add(self.World.Map.Rules.Actors[item.Item]);
			}

			foreach (var p in producers.Where(p => !p.Actor.IsDisabled()))
			{
				if (p.Trait.Produce(p.Actor, ai, Race))
				{
					return true;
				}
			}

			return false;
		}

		public override int GetBuildTime(string unitString)
		{
			if (self.World.AllowDevCommands && self.Owner.PlayerActor.Trait<DeveloperMode>().FastBuild)
				return 0;

			if (info.DynamicProductionTime)
			{
				var time = 0;

				foreach (var item in activeQueue)
				{
					var unit = self.World.Map.Rules.Actors[item.Item];
					if (unit == null || !unit.Traits.Contains<BuildableInfo>())
						continue;

					time += (int)(unit.GetBuildTime() * Info.BuildSpeed);
				}

				return time;
			}

			return info.ProductionTime;
		}

		public override void ResolveOrder(Actor self, Order order)
		{
			if (!Enabled)
				return;

			switch (order.OrderString)
			{
				case "StartProduction":
					{
						if (activeQueue.Count != 0)
						{
							queue.Clear();
							return; // Doesnt allow you to queue more if there are items currently queued
						}

						var unit = self.World.Map.Rules.Actors[order.TargetString];
						var bi = unit.Traits.Get<BuildableInfo>();
						if (!bi.Queue.Contains(Info.Type))
							return; /* Not built by this queue */

						var cost = unit.Traits.Contains<ValuedInfo>() ? unit.Traits.Get<ValuedInfo>().Cost : 0;

						if (BuildableItems().All(b => b.Name != order.TargetString))
							return;	/* you can't build that!! */

						var amountToBuild = order.ExtraData;
						for (var n = 0; n < amountToBuild; n++)
						{
							BeginProduction(new BulkProductionItem(this, order.TargetString, cost, playerPower, () => self.World.AddFrameEndTask(_ =>
							{
								completedItems++;
							})));
						}

						// Temp, should be handled by the FinalizeProduction order when the UI for it has been made.
						if (activeQueue.Count != 0 || queue.Count < info.MinimumOrders || queue.Count > info.MaximumOrders) return;
						activeQueue = queue.ToList();
						queue.Clear();

						break;
					}
				case "CancelProduction":
					{
						CancelProduction(order.TargetString, order.ExtraData);
						break;
					}
				case "FinalizeProduction":
					{
						// Doesnt allow you to queue more if there are items currently queued
						// and also checks if you are under the minimum
						// or over the maximum orders
						if (activeQueue.Count != 0 || queue.Count < info.MinimumOrders || queue.Count > info.MaximumOrders) return;

						activeQueue = queue;
						queue.Clear();

						break;
					}
			}
		}
		
		protected override void CancelProduction(string itemName, uint numberToCancel)
		{
			for (var i = 0; i < numberToCancel; i++)
			{
				var lastIndex = queue.FindLastIndex(a => a.Item == itemName);
				if (lastIndex <= 0) queue.Clear();
				else queue.RemoveAt(lastIndex);
			}
		}

		public override void FinishProduction()
		{
			activeQueue.Clear();
		}

		public class BulkProductionItem : ProductionItem
		{
			public BulkProductionItem(ProductionQueue queue, string item, int cost, PowerManager pm, Action onComplete)
				: base(queue, item, cost, pm, onComplete) { }

			public override void Tick(PlayerResources pr)
			{
				if (!Started)
				{
					var time = Queue.GetBuildTime(Item);
					if (time > 0)
						RemainingTime = TotalTime = time;

					if (TotalCost != 0)
					{
						pr.TakeCash(TotalCost);
						RemainingCost = 0;
					}

					Started = true;
				}

				if (Done)
				{
					if (OnComplete != null)
						OnComplete();

					return;
				}

				if (Paused)
					return;

				if (pm.PowerState != PowerState.Normal)
				{
					if (--Slowdown <= 0)
						Slowdown = Queue.Info.LowPowerSlowdown;
					else
						return;
				}

				RemainingTime -= 1;
				if (RemainingTime > 0)
					return;

				Done = true;
			}
		}
	}
}
