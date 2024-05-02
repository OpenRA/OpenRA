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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class DeliverBulkOrder : Activity
	{
		readonly Actor producer;
		readonly Actor transport;
		readonly List<ActorInfo> orderedActors;
		readonly string productionType;
		readonly BulkProductionQueue queue;
		readonly Cargo cargo;
		int delayBetweenUnloads = 0;

		public DeliverBulkOrder(Actor transport, Actor producer, List<ActorInfo> orderedActors, string productionType, BulkProductionQueue queue)
		{
			this.producer = producer;
			this.transport = transport;
			this.orderedActors = orderedActors;
			this.productionType = productionType;
			this.queue = queue;
			cargo = transport.Trait<Cargo>();
		}

		protected override void OnFirstRun(Actor self)
		{
			var landingOffset = producer.Info.TraitInfo<ProductionStarportInfo>().LandOffset;
			QueueChild(new Land(transport, Target.FromActor(producer), WDist.FromCells(0), landingOffset));
			QueueChild(new Wait(cargo.Info.BeforeUnloadDelay));
		}

		protected override void OnLastRun(Actor self)
		{
			if (!producer.IsDead || producer.IsInWorld)
			{
				foreach (var cargo in producer.TraitsImplementing<INotifyDelivery>())
					cargo.Delivered(producer);
			}

			Queue(new Wait(cargo.Info.AfterUnloadDelay));
			Queue(new FlyOffMap(self, Target.FromCell(self.World, self.World.Map.ChooseClosestEdgeCell(self.Location))));
			Queue(new RemoveSelf());
		}

		protected override void OnActorDispose(Actor self)
		{
			queue.DeliverFinished();
		}

		public override bool Tick(Actor self)
		{
			if (!producer.IsInWorld || producer.IsDead)
			{
				// Try to find another ProductionStarport
				var newProducer = self.World.ActorsWithTrait<ProductionStarport>().Where(a => a.Actor.Owner == self.Owner).ToArray();
				if (newProducer.Length != 0)
				{
					Cancel(self);
					Queue(new DeliverBulkOrder(transport, newProducer[0].Actor, orderedActors, productionType, queue));
					return true;
				}
				else
				{
					queue.DeliverFinished();
					return true;
				}
			}

			if (orderedActors == null || orderedActors.Count == 0)
			{
				queue.DeliverFinished();
				return true;
			}

			var actor = orderedActors.Last();
			var exit = producer.Trait<ProductionStarport>().PublicExit(producer, actor, productionType);
			if (exit == null)
			{
				var exits = producer.Info.TraitInfos<ExitInfo>().First();
				var cell = producer.Location + exits.ExitCell;
				producer.NotifyBlocker(cell);
				return false;
			}
			else
			{
				if (delayBetweenUnloads > 0)
				{
					delayBetweenUnloads--;
					return false;
				}

				delayBetweenUnloads = cargo.Info.DelayBetweenUnloads;
				producer.World.AddFrameEndTask(ww =>
				{
					var inits = new TypeDictionary
					{
						new OwnerInit(self.Owner),
						new FactionInit(BuildableInfo.GetInitialFaction(actor, producer.Trait<ProductionStarport>().Faction))
					};
					producer.Trait<ProductionStarport>().DoProduction(producer, actor, exit?.Info, productionType, inits);
					orderedActors.Remove(actor);
				});
				return false;
			}
		}
	}
}
