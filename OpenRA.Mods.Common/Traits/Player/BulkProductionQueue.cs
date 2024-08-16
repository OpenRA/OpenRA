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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Attach this to the player actor (not a building!) to define a new shared build queue.",
	"Allow you to build multiple actors before delivery",
	"Will work together with the ProductionProduction: trait on the actor that actually does the production.",
	"You will also want to add PrimaryBuildings: to let the user choose where new units should exit.")]
	public class BulkProductionQueueInfo : ProductionQueueInfo, Requires<TechTreeInfo>, Requires<PlayerResourcesInfo>
	{
		[Desc("Maximum order capacity.")]
		public readonly int MaxCapacity = 6;

		[Desc("Delivery delay in ticks.")]
		public readonly int DeliveryDelay = 1500;

		[NotificationReference("Speech")]
		[Desc("Notification played when deliver started.")]
		public readonly string StartDeliveryAudio = null;

		[Desc("Return funds if delivery fails.")]
		public readonly bool RefundUndeliveredActors = true;

		[TranslationReference(optional: true)]
		[Desc("Notification displayed when deliver started.")]
		public readonly string StartDeliveryNotification = null;

		[NotificationReference("Speech")]
		[Desc("Notifications to play when frigate is on its way. Last notification is played when delivery actor is spawned on the map.")]
		public readonly string[] DeliveryProgressNotifications = Array.Empty<string>();

		public override object Create(ActorInitializer init) { return new BulkProductionQueue(init, this); }
	}

	public class BulkProductionQueue : ProductionQueue
	{
		static readonly ActorInfo[] NoItems = Array.Empty<ActorInfo>();

		readonly Actor self;
		readonly BulkProductionQueueInfo info;

		// Used in refunds. Key is resources, Value is cash
		protected readonly List<(ActorInfo Actor, int Resources, int Cash)> ActorsReadyForDelivery = new();
		public int DeliveryDelay { get; private set; } = 0;
		protected int notificationInterval = 0;

		protected bool deliveryProcessStarted = false;
		List<string> notifications = new();
		public BulkProductionQueue(ActorInitializer init, BulkProductionQueueInfo info)
			: base(init, info)
		{
			self = init.Self;
			this.info = info;
			if (info.DeliveryProgressNotifications.Length != 0)
				notificationInterval = info.DeliveryDelay / info.DeliveryProgressNotifications.Length;
		}

		protected override void Tick(Actor self)
		{
			// PERF: Avoid LINQ.
			Enabled = false;
			var isActive = false;
			foreach (var x in self.World.ActorsWithTrait<Production>())
			{
				if (x.Trait.IsTraitDisabled)
					continue;

				if (x.Actor.Owner != self.Owner || !x.Trait.Info.Produces.Contains(Info.Type))
					continue;

				Enabled |= IsValidFaction;
				isActive |= !x.Trait.IsTraitPaused;
			}

			if (!Enabled)
			{
				DeliverFinished();
				ClearQueue();
			}
			else
			{
				if (HasDeliveryStarted() && DeliveryDelay > 0)
				{
					DeliveryDelay--;
					NotificationsSystem();
				}
				else if (HasDeliveryStarted() && DeliveryDelay == 0)
				{
					NotificationsSystem();
					DeliveryHasArrived();
					DeliveryDelay--;
				}
			}

			TickInner(self, !isActive);
		}

		public override IEnumerable<ActorInfo> AllItems()
		{
			return Enabled ? base.AllItems() : NoItems;
		}

		public override IEnumerable<ActorInfo> BuildableItems()
		{
			return Enabled && ActorsReadyForDelivery.Count != info.MaxCapacity && !deliveryProcessStarted ? base.BuildableItems() : NoItems;
		}

		public override TraitPair<Production> MostLikelyProducer()
		{
			var productionActor = self.World.ActorsWithTrait<Production>()
				.Where(x => x.Actor.Owner == self.Owner
					&& !x.Trait.IsTraitDisabled && x.Trait.Info.Produces.Contains(Info.Type))
				.OrderBy(x => x.Trait.IsTraitPaused)
				.ThenByDescending(x => x.Actor.Trait<PrimaryBuilding>().IsPrimary)
				.ThenByDescending(x => x.Actor.ActorID)
				.FirstOrDefault();

			return productionActor;
		}

		protected override bool BuildUnit(ActorInfo unit)
		{
			// Find a production structure to build this actor
			var bi = unit.TraitInfo<BuildableInfo>();

			// Some units may request a specific production type, which is ignored if the AllTech cheat is enabled
			var type = developerMode.AllTech ? Info.Type : (bi.BuildAtProductionType ?? Info.Type);

			var producers = self.World.ActorsWithTrait<Production>()
				.Where(x => x.Actor.Owner == self.Owner
					&& !x.Trait.IsTraitDisabled
					&& x.Trait.Info.Produces.Contains(type))
					.OrderByDescending(x => x.Actor.Trait<PrimaryBuilding>().IsPrimary)
					.ThenByDescending(x => x.Actor.ActorID);

			var anyProducers = false;
			foreach (var p in producers)
			{
				anyProducers = true;
				if (p.Trait.IsTraitPaused)
					continue;
				var item = Queue.First(i => i.Done && i.Item == unit.Name);
				if (ActorsReadyForDelivery.Count <= info.MaxCapacity)
				{
					ActorsReadyForDelivery.Add((unit, item.ResourcesPaid, item.TotalCost - item.ResourcesPaid));
					EndProduction(item);
					return true;
				}
			}

			if (!anyProducers)
				CancelProduction(unit.Name, 1);

			return false;
		}

		public override void ResolveOrder(Actor self, Order order)
		{
			if (!Enabled)
				return;

			var rules = self.World.Map.Rules;
			switch (order.OrderString)
			{
				case "StartProduction":
					var unit = rules.Actors[order.TargetString];
					var bi = unit.TraitInfo<BuildableInfo>();

					// Not built by this queue
					if (!bi.Queue.Contains(Info.Type))
						return;

					// You can't build that
					if (BuildableItems().All(b => b.Name != order.TargetString))
						return;

					// Check if the player is trying to build more units that they are allowed
					var fromLimit = int.MaxValue;
					if (!developerMode.AllTech)
					{
						if (Info.QueueLimit > 0)
							fromLimit = Info.QueueLimit - Queue.Count;

						if (Info.ItemLimit > 0)
							fromLimit = Math.Min(fromLimit, Info.ItemLimit - Queue.Count(i => i.Item == order.TargetString));

						if (bi.BuildLimit > 0)
						{
							var inQueue = Queue.Count(pi => pi.Item == order.TargetString);
							var owned = self.Owner.World.ActorsHavingTrait<Buildable>().Count(a => a.Info.Name == order.TargetString && a.Owner == self.Owner);
							fromLimit = Math.Min(fromLimit, bi.BuildLimit - (inQueue + owned));
						}

						if (fromLimit <= 0)
							return;
					}

					var cost = GetProductionCost(unit);
					var time = GetBuildTime(unit, bi);
					var amountToBuild = Math.Min(fromLimit, order.ExtraData);
					for (var n = 0; n < amountToBuild; n++)
					{
						if (Info.PayUpFront && cost > playerResources.GetCashAndResources())
							return;
						BeginProduction(new ProductionItem(this, order.TargetString, cost, PlayerPower, () => self.World.AddFrameEndTask(_ =>
						{
							// Make sure the item hasn't been invalidated between the ProductionItem ticking and this FrameEndTask running
							if (!Queue.Any(i => i.Done && i.Item == unit.Name))
								return;
							BuildUnit(unit);
						})), !order.Queued);
					}

					break;
				case "PauseProduction":
					PauseProduction(order.TargetString, order.ExtraData != 0);
					break;
				case "CancelProduction":
					CancelProduction(order.TargetString, order.ExtraData);
					break;
				case "ReturnOrder":
					ReturnOrder(order.TargetString, order.ExtraData);
					break;
				case "PurchaseOrder":
					if (!deliveryProcessStarted && order.TargetString == info.Type)
						StartDeliveryProcess();
					break;
			}
		}

		public void DeliverFinished()
		{
			if (info.RefundUndeliveredActors || !deliveryProcessStarted)
				ReturnOrder();
			ActorsReadyForDelivery.Clear();
			deliveryProcessStarted = false;
		}

		public bool HasDeliveryStarted()
		{
			return deliveryProcessStarted;
		}

		public List<(ActorInfo Actor, int Resources, int Cash)> GetActorsReadyForDelivery()
		{
			return ActorsReadyForDelivery;
		}

		protected void StartDeliveryProcess()
		{
			ClearQueue();
			deliveryProcessStarted = true;
			DeliveryDelay = info.DeliveryDelay;
			var rules = self.World.Map.Rules;
			Game.Sound.PlayNotification(rules, self.Owner, "Speech", info.StartDeliveryAudio, self.Owner.Faction.InternalName);
			if (info.StartDeliveryNotification != null)
				TextNotificationsManager.AddTransientLine(self.Owner, info.StartDeliveryNotification);
		}

		protected void DeliveryHasArrived()
		{
			var producers = self.World.ActorsWithTrait<ProductionStarport>()
				.Where(x => x.Actor.Owner == self.Owner
					&& !x.Trait.IsTraitDisabled
					&& !x.Trait.IsTraitPaused
					&& x.Trait.Info.Produces.Contains(Info.Type))
					.OrderByDescending(x => x.Actor.Trait<PrimaryBuilding>().IsPrimary)
					.ThenByDescending(x => x.Actor.ActorID);
			var p = producers.FirstOrDefault();
			if (p.Trait != null)
				p.Trait.DeliverOrder(p.Actor, ActorsReadyForDelivery, Info.Type, this);
			else
			{
				// Use regular production if StarportProduction wasnt found
				var regularProducer = MostLikelyProducer();
				if (regularProducer.Trait == null)
					return;
				foreach (var actor in ActorsReadyForDelivery)
				{
					var inits = new TypeDictionary
					{
						new OwnerInit(self.Owner),
						new FactionInit(BuildableInfo.GetInitialFaction(actor.Actor, regularProducer.Trait.Faction))
					};
					var cost = actor.Cash + actor.Resources;
					regularProducer.Trait.Produce(regularProducer.Actor, actor.Actor, Info.Type, inits, cost);
				}

				ActorsReadyForDelivery.Clear();
				DeliverFinished();
			}
		}

		public void ReturnOrder(string itemName = null, uint numberToCancel = 1)
		{
			if (itemName == null)
			{
				foreach (var actorData in ActorsReadyForDelivery)
				{
					playerResources.GiveResources(actorData.Resources);
					playerResources.GiveCash(actorData.Cash);
				}

				ActorsReadyForDelivery.Clear();
			}

			for (var i = 0; i < numberToCancel; i++)
			{
				var actor = ActorsReadyForDelivery.LastOrDefault(actor => actor.Actor.Name == itemName);
				if (actor.Actor == null)
					break;
				playerResources.GiveResources(actor.Resources);
				playerResources.GiveCash(actor.Cash);
				ActorsReadyForDelivery.Remove(actor);
			}
		}

		protected void NotificationsSystem()
		{
			if (info.DeliveryProgressNotifications.Length == 0)
				return;
			if (notificationInterval == 0)
			{
				if (notifications.Count == 0)
					notifications = info.DeliveryProgressNotifications.ToList();
				notificationInterval = info.DeliveryDelay / info.DeliveryProgressNotifications.Length;
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", notifications.FirstOrDefault(), self.Owner.Faction.InternalName);
				notifications.RemoveAt(0);
			}

			notificationInterval--;
		}
	}
}
