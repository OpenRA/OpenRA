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
using System.Collections.Immutable;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Attach this to the player actor (not a building!) to define a new shared build queue.",
		"Allows you to build multiple actors from Stock",
		"Stock replenishment is tight to BuildDuration inside Buildable Trait",
		"Will work together with the `ProductionBulkAirDrop` trait on the actor that actually does the production.",
		"You will also want to add `PrimaryBuilding` to let the user choose where new units should exit.")]
	public class BulkProductionQueueInfo : ProductionQueueInfo, Requires<TechTreeInfo>, Requires<PlayerResourcesInfo>
	{
		[Desc("Maximum order capacity.")]
		public readonly int MaxCapacity = 6;

		[Desc("Maximum stock capacity per unit.")]
		public readonly int MaxStockCapacity = 5;

		[Desc("Should game start with full stockpiles?")]
		public readonly bool StartFullyStocked = false;

		[Desc("Delivery delay in ticks.")]
		public readonly int DeliveryDelay = 1500;

		[NotificationReference("Speech")]
		[Desc("Notification played when the delivery started.")]
		public readonly string StartDeliveryNotification = null;

		[Desc("Return funds if the delivery fails.")]
		public readonly bool RefundUndeliveredActors = true;

		[FluentReference(optional: true)]
		[Desc("Notification displayed when the delivery started.")]
		public readonly string StartDeliveryTextNotification = null;

		[NotificationReference("Speech")]
		[Desc("Notifications to play when the delivery actor is on its way. Last notification is played when the actors to deliver are spawned on the map.")]
		public readonly ImmutableArray<string> DeliveryProgressNotifications = [];

		public override object Create(ActorInitializer init) { return new BulkProductionQueue(init, this); }
	}

	public class BulkProductionQueue : ProductionQueue
	{
		static readonly ActorInfo[] NoItems = [];

		readonly Actor self;
		public new readonly BulkProductionQueueInfo Info;

		protected readonly List<(ActorInfo Actor, int Resources, int Cash)> ActorsReadyForDelivery = [];

		protected List<(ActorInfo Actor, int Count)> stocks = [];
		public int DeliveryDelay { get; private set; } = 0;
		protected int notificationInterval = 0;
		protected int notificationIndex = 0;

		protected bool deliveryProcessStarted = false;

		public bool RestockingEnabled = true;

		public BulkProductionQueue(ActorInitializer init, BulkProductionQueueInfo info)
			: base(init, info)
		{
			self = init.Self;
			Info = info;
			if (info.DeliveryProgressNotifications.Length != 0)
				notificationInterval = info.DeliveryDelay / info.DeliveryProgressNotifications.Length;

			foreach (var item in AllBuildables(Info.Type))
			{
				if (info.StartFullyStocked)
					stocks.Add((item, info.MaxCapacity));
				else
					stocks.Add((item, 0));
			}
		}

		protected override void Tick(Actor self)
		{
			if (RestockingEnabled)
				ReplenishmentLogic();

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
			}
			else
			{
				if (HasDeliveryStarted() && DeliveryDelay > 0)
				{
					if (developerMode.FastBuild)
						DeliveryDelay = 0;
					else
					{
						DeliveryDelay--;
						PlayDeliveryProgressNotifications();
					}
				}
				else if (HasDeliveryStarted() && DeliveryDelay == 0)
				{
					PlayDeliveryProgressNotifications();
					DeliveryHasArrived();
					DeliveryDelay--;
				}
			}

			TickInner(self, !isActive);
		}

		protected override void TickInner(Actor self, bool allProductionPaused)
		{ }

		public override IEnumerable<ActorInfo> AllItems()
		{
			return Enabled ? base.AllItems() : NoItems;
		}

		public override IEnumerable<ActorInfo> BuildableItems()
		{
			if (!Enabled || ActorsReadyForDelivery.Count == Info.MaxCapacity
				|| deliveryProcessStarted)
				return NoItems;
			if (!Info.PayUpFront && developerMode.AllTech)
				return Producible.Keys;
			if (Info.PayUpFront && developerMode.AllTech)
				return Producible.Keys.Where(a => GetProductionCost(a) <= playerResources.GetCashAndResources() || IsInQueue(a));
			var buildableProducibles = Producible.Where(a => a.Value.Buildable).Select(a => a.Key)
				.Where(a => stocks.FirstOrDefault(s => s.Actor.Name == a.Name).Count > 0);
			if (Info.PayUpFront)
				return buildableProducibles.Where(a => GetProductionCost(a) <= playerResources.GetCashAndResources());

			return buildableProducibles;
		}

		public override TraitPair<Production> MostLikelyProducer()
		{
			return self.World.ActorsWithTrait<Production>()
				.Where(x => x.Actor.Owner == self.Owner
					&& !x.Trait.IsTraitDisabled && x.Trait.Info.Produces.Contains(Info.Type))
				.OrderBy(x => x.Trait.IsTraitPaused)
				.ThenByDescending(x => x.Actor.Trait<PrimaryBuilding>().IsPrimary)
				.ThenByDescending(x => x.Actor.ActorID)
				.FirstOrDefault();
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
						if (bi.BuildLimit > 0)
						{
							var owned = self.Owner.World.ActorsHavingTrait<Buildable>().Count(a => a.Info.Name == order.TargetString && a.Owner == self.Owner);
							fromLimit = Math.Min(fromLimit, bi.BuildLimit - owned);
						}

						if (fromLimit <= 0)
							return;
					}

					var cost = GetProductionCost(unit);
					var time = GetBuildTime(unit, bi);
					var amountToBuild = Math.Min(Info.MaxCapacity, order.ExtraData);
					for (var n = 0; n < amountToBuild; n++)
					{
						var stock = stocks.FirstOrDefault(a => a.Actor == unit);
						if (stock.Count == 0)
							return;
						if (cost > playerResources.GetCashAndResources())
							return;
						RemoveStock(stock);
						var resourcePaid = 0;
						if (playerResources.Resources > 0 && playerResources.Resources <= cost)
							resourcePaid = playerResources.Resources;
						else if (playerResources.Resources > cost)
							resourcePaid = cost;
						playerResources.TakeCash(cost);
						ActorsReadyForDelivery.Add((unit, resourcePaid, GetProductionCost(unit) - resourcePaid));
					}

					break;
				case "PauseProduction":
				case "CancelProduction":
				case "ReturnOrder":
					ReturnOrder(order.TargetString, order.ExtraData);
					break;
				case "PurchaseOrder":
					if (!deliveryProcessStarted && order.TargetString == Info.Type)
						StartDeliveryProcess();
					break;
			}
		}

		public void DeliverFinished()
		{
			if (Info.RefundUndeliveredActors || !deliveryProcessStarted)
				ReturnOrder();
			ActorsReadyForDelivery.Clear();
			deliveryProcessStarted = false;
			RestockingEnabled = true;
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
			deliveryProcessStarted = true;
			DeliveryDelay = Info.DeliveryDelay;
			var rules = self.World.Map.Rules;
			Game.Sound.PlayNotification(rules, self.Owner, "Speech", Info.StartDeliveryNotification, self.Owner.Faction.InternalName);
			if (Info.StartDeliveryTextNotification != null)
				TextNotificationsManager.AddTransientLine(self.Owner, Info.StartDeliveryTextNotification);
			RestockingEnabled = true;
		}

		protected void DeliveryHasArrived()
		{
			var producers = self.World.ActorsWithTrait<ProductionBulkAirdrop>()
				.Where(x => x.Actor.Owner == self.Owner
					&& !x.Trait.IsTraitDisabled
					&& !x.Trait.IsTraitPaused
					&& x.Trait.Info.Produces.Contains(Info.Type))
				.OrderByDescending(x => x.Actor.Trait<PrimaryBuilding>().IsPrimary)
				.ThenByDescending(x => x.Actor.ActorID);
			var p = producers.FirstOrDefault();
			p.Trait?.DeliverOrder(p.Actor, ActorsReadyForDelivery, Info.Type, this);
		}

		public void ReturnOrder(string itemName = null, uint numberToCancel = 1)
		{
			if (itemName == null)
			{
				for (var i = ActorsReadyForDelivery.Count - 1; i >= 0; i--)
				{
					playerResources.RefundResources(ActorsReadyForDelivery[i].Resources);
					playerResources.RefundCash(ActorsReadyForDelivery[i].Cash);
					var stock = stocks.FirstOrDefault(a => a.Actor.Name == ActorsReadyForDelivery[i].Actor.Name);

					// Actor must to removed before stock
					ActorsReadyForDelivery.RemoveAt(i);
					AddStock(stock);
				}

				return;
			}

			for (var i = 0; i < numberToCancel; i++)
			{
				var actor = ActorsReadyForDelivery.LastOrDefault(actor => actor.Actor.Name == itemName);
				if (actor.Actor == null)
					break;
				playerResources.RefundResources(actor.Resources);
				playerResources.RefundCash(actor.Cash);
				ActorsReadyForDelivery.Remove(actor);
				var stock = stocks.FirstOrDefault(a => a.Actor.Name == itemName);
				stock.Count++;
				stocks[stocks.FindIndex(a => a.Actor.Name == itemName)] = stock;
			}
		}

		protected void PlayDeliveryProgressNotifications()
		{
			if (notificationInterval == 0)
			{
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech",
				Info.DeliveryProgressNotifications[notificationIndex], self.Owner.Faction.InternalName);
				notificationInterval = Info.DeliveryDelay / Info.DeliveryProgressNotifications.Length;
				notificationIndex++;
				if (Info.DeliveryProgressNotifications.Length == notificationIndex)
				{
					notificationIndex = 0;
					return;
				}
			}

			notificationInterval--;
		}

		public override bool IsProducing(ProductionItem item)
		{
			return Queue.Contains(item);
		}

		protected void AddStock((ActorInfo Info, int Counter) stock)
		{
			var counter = stock.Counter + 1;
			var finishedStocks = counter + ActorsReadyForDelivery.Count(a => a.Actor.Name == stock.Info.Name);

			if (finishedStocks == Info.MaxStockCapacity && Queue.Any(a => a.Item == stock.Info.Name))
				Queue.Remove(Queue.FirstOrDefault(a => a.Item == stock.Info.Name));

			if (finishedStocks > Info.MaxStockCapacity)
				return;
			stocks[stocks.FindIndex(a => a.Actor.Name == stock.Info.Name)] = (stock.Info, counter);
		}

		public override bool CanQueue(ActorInfo actor, out string notificationAudio, out string notificationText)
		{
			// these notifications are irrelevant in Bulkproduction
			notificationAudio = null;
			notificationText = null;

			if (!HasDeliveryStarted() && stocks.FirstOrDefault(a => a.Actor.Name == actor.Name).Count > 0)
				return true;

			return false;
		}

		protected void RemoveStock((ActorInfo Info, int Count) stock)
		{
			if (stock.Count < 0)
				return;
			stock.Count--;
			stocks[stocks.FindIndex(a => a.Actor.Name == stock.Info.Name)] = stock;
		}

		protected void ReplenishmentLogic()
		{
			if (StocksFull())
			{
				RestockingEnabled = false;
				return;
			}

			foreach (var stock in stocks)
			{
				if (stock.Count + GetActorsReadyForDelivery().Count(a => a.Actor.Name == stock.Actor.Name) == Info.MaxStockCapacity)
					continue;

				// check if stock is already queued
				var item = Queue.FirstOrDefault(q => q.Item == stock.Actor.Name);
				if (item == null)
				{
					// Is AddFrameEndTask nesesary here?
					BeginProduction(new ProductionItem(this, stock.Actor.Name,
					 0, playerPower, () => self.World.AddFrameEndTask(_ =>
						{
							if (!Queue.Any(q => q.Done && q.Item == stock.Actor.Name))
								return;
							var finishedItem = Queue.First(q => q.Done && q.Item == stock.Actor.Name);

							// stock count can change between production tick
							var actualStock = stocks.FirstOrDefault(a => a.Actor.Name == stock.Actor.Name);
							AddStock(actualStock);
							EndProduction(finishedItem);
						})), false);
				}
				else
				{
					var before = item.RemainingTime;
					item.Tick(playerResources);
				}
			}
		}

		public bool StocksFull()
		{
			foreach (var stock in stocks)
			{
				var reserverdItems = GetActorsReadyForDelivery().Count(a => a.Actor.Name == stock.Actor.Name);

				if (stock.Count + reserverdItems < Info.MaxStockCapacity)
					return false;
			}

			return true;
		}

		public List<(ActorInfo Actor, int Count)> GetStocks()
		{
			return stocks;
		}
	}
}
