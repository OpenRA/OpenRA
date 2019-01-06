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
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Attach this to an actor (usually a building) to let it produce units or construct buildings.",
		"If one builds another actor of this type, he will get a separate queue to create two actors",
		"at the same time. Will only work together with the Production: trait.")]
	public class ProductionQueueInfo : ITraitInfo
	{
		[FieldLoader.Require]
		[Desc("What kind of production will be added (e.g. Building, Infantry, Vehicle, ...)")]
		public readonly string Type = null;

		[Desc("Group queues from separate buildings together into the same tab.")]
		public readonly string Group = null;

		[Desc("Only enable this queue for certain factions.")]
		public readonly HashSet<string> Factions = new HashSet<string>();

		[Desc("Should the prerequisite remain enabled if the owner changes?")]
		public readonly bool Sticky = true;

		[Desc("Should right clicking on the icon instantly cancel the production instead of putting it on hold?")]
		public readonly bool DisallowPaused = false;

		[Desc("This percentage value is multiplied with actor cost to translate into build time (lower means faster).")]
		public readonly int BuildDurationModifier = 100;

		[Desc("Maximum number of a single actor type that can be queued (0 = infinite).")]
		public readonly int ItemLimit = 999;

		[Desc("Maximum number of items that can be queued across all actor types (0 = infinite).")]
		public readonly int QueueLimit = 0;

		[Desc("The build time is multiplied with this percentage on low power.")]
		public readonly int LowPowerModifier = 100;

		[Desc("Production items that have more than this many items in the queue will be produced in a loop.")]
		public readonly int InfiniteBuildLimit = -1;

		[NotificationReference("Speech")]
		[Desc("Notification played when production is complete.",
			"The filename of the audio is defined per faction in notifications.yaml.")]
		public readonly string ReadyAudio = null;

		[NotificationReference("Speech")]
		[Desc("Notification played when you can't train another actor",
			"when the build limit exceeded or the exit is jammed.",
			"The filename of the audio is defined per faction in notifications.yaml.")]
		public readonly string BlockedAudio = null;

		[NotificationReference("Speech")]
		[Desc("Notification played when you can't queue another actor",
			"when the queue length limit is exceeded.",
			"The filename of the audio is defined per faction in notifications.yaml.")]
		public readonly string LimitedAudio = null;

		[NotificationReference("Speech")]
		[Desc("Notification played when user clicks on the build palette icon.",
			"The filename of the audio is defined per faction in notifications.yaml.")]
		public readonly string QueuedAudio = null;

		[NotificationReference("Speech")]
		[Desc("Notification played when player right-clicks on the build palette icon.",
			"The filename of the audio is defined per faction in notifications.yaml.")]
		public readonly string OnHoldAudio = null;

		[NotificationReference("Speech")]
		[Desc("Notification played when player right-clicks on a build palette icon that is already on hold.",
			"The filename of the audio is defined per faction in notifications.yaml.")]
		public readonly string CancelledAudio = null;

		public virtual object Create(ActorInitializer init) { return new ProductionQueue(init, init.Self.Owner.PlayerActor, this); }

		public void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			if (LowPowerModifier <= 0)
				throw new YamlException("Production queue must have LowPowerModifier of at least 1.");
		}
	}

	public class ProductionQueue : IResolveOrder, ITick, ITechTreeElement, INotifyOwnerChanged, INotifyKilled, INotifySold, ISync, INotifyTransform, INotifyCreated
	{
		public readonly ProductionQueueInfo Info;
		readonly Actor self;

		// A list of things we could possibly build
		protected readonly Dictionary<ActorInfo, ProductionState> Producible = new Dictionary<ActorInfo, ProductionState>();
		protected readonly List<ProductionItem> Queue = new List<ProductionItem>();
		readonly IEnumerable<ActorInfo> allProducibles;
		readonly IEnumerable<ActorInfo> buildableProducibles;

		protected Production[] productionTraits;

		// Will change if the owner changes
		PowerManager playerPower;
		protected PlayerResources playerResources;
		protected DeveloperMode developerMode;

		public Actor Actor { get { return self; } }

		[Sync] public bool Enabled { get; protected set; }

		public string Faction { get; private set; }
		[Sync] public bool IsValidFaction { get; private set; }

		public ProductionQueue(ActorInitializer init, Actor playerActor, ProductionQueueInfo info)
		{
			self = init.Self;
			Info = info;
			playerResources = playerActor.Trait<PlayerResources>();
			developerMode = playerActor.Trait<DeveloperMode>();

			Faction = init.Contains<FactionInit>() ? init.Get<FactionInit, string>() : self.Owner.Faction.InternalName;
			IsValidFaction = !info.Factions.Any() || info.Factions.Contains(Faction);
			Enabled = IsValidFaction;

			CacheProducibles(playerActor);
			allProducibles = Producible.Where(a => a.Value.Buildable || a.Value.Visible).Select(a => a.Key);
			buildableProducibles = Producible.Where(a => a.Value.Buildable).Select(a => a.Key);
		}

		void INotifyCreated.Created(Actor self)
		{
			// Special case handling is required for the Player actor.
			// Created is called before Player.PlayerActor is assigned,
			// so we must query other player traits from self, knowing that
			// it refers to the same actor as self.Owner.PlayerActor
			playerPower = (self.Info.Name == "player" ? self : self.Owner.PlayerActor).TraitOrDefault<PowerManager>();
			productionTraits = self.TraitsImplementing<Production>().Where(p => p.Info.Produces.Contains(Info.Type)).ToArray();
		}

		protected void ClearQueue()
		{
			// Refund the current item
			foreach (var item in Queue)
				playerResources.GiveCash(item.TotalCost - item.RemainingCost);
			Queue.Clear();
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			ClearQueue();

			playerPower = newOwner.PlayerActor.TraitOrDefault<PowerManager>();
			playerResources = newOwner.PlayerActor.Trait<PlayerResources>();
			developerMode = newOwner.PlayerActor.Trait<DeveloperMode>();

			if (!Info.Sticky)
			{
				Faction = self.Owner.Faction.InternalName;
				IsValidFaction = !Info.Factions.Any() || Info.Factions.Contains(Faction);
			}

			// Regenerate the producibles and tech tree state
			oldOwner.PlayerActor.Trait<TechTree>().Remove(this);
			CacheProducibles(newOwner.PlayerActor);
			newOwner.PlayerActor.Trait<TechTree>().Update();
		}

		void INotifyKilled.Killed(Actor killed, AttackInfo e) { if (killed == self) { ClearQueue(); Enabled = false; } }
		void INotifySold.Selling(Actor self) { ClearQueue(); Enabled = false; }
		void INotifySold.Sold(Actor self) { }

		void INotifyTransform.BeforeTransform(Actor self) { ClearQueue(); Enabled = false; }
		void INotifyTransform.OnTransform(Actor self) { }
		void INotifyTransform.AfterTransform(Actor self) { }

		void CacheProducibles(Actor playerActor)
		{
			Producible.Clear();
			if (!Enabled)
				return;

			var ttc = playerActor.Trait<TechTree>();

			foreach (var a in AllBuildables(Info.Type))
			{
				var bi = a.TraitInfo<BuildableInfo>();

				Producible.Add(a, new ProductionState());
				ttc.Add(a.Name, bi.Prerequisites, bi.BuildLimit, this);
			}
		}

		IEnumerable<ActorInfo> AllBuildables(string category)
		{
			return self.World.Map.Rules.Actors.Values
				.Where(x =>
					x.Name[0] != '^' &&
					x.HasTraitInfo<BuildableInfo>() &&
					x.TraitInfo<BuildableInfo>().Queue.Contains(category));
		}

		public void PrerequisitesAvailable(string key)
		{
			Producible[self.World.Map.Rules.Actors[key]].Buildable = true;
		}

		public void PrerequisitesUnavailable(string key)
		{
			Producible[self.World.Map.Rules.Actors[key]].Buildable = false;
		}

		public void PrerequisitesItemHidden(string key)
		{
			Producible[self.World.Map.Rules.Actors[key]].Visible = false;
		}

		public void PrerequisitesItemVisible(string key)
		{
			Producible[self.World.Map.Rules.Actors[key]].Visible = true;
		}

		public virtual bool IsProducing(ProductionItem item)
		{
			return Queue.Count > 0 && Queue[0] == item;
		}

		public virtual IEnumerable<ProductionItem> AllQueued()
		{
			return Queue;
		}

		public virtual IEnumerable<ActorInfo> AllItems()
		{
			if (productionTraits.Any() && productionTraits.All(p => p.IsTraitDisabled))
				return Enumerable.Empty<ActorInfo>();
			if (developerMode.AllTech)
				return Producible.Keys;

			return allProducibles;
		}

		public virtual IEnumerable<ActorInfo> BuildableItems()
		{
			if (productionTraits.Any() && productionTraits.All(p => p.IsTraitDisabled))
				return Enumerable.Empty<ActorInfo>();
			if (!Enabled)
				return Enumerable.Empty<ActorInfo>();
			if (developerMode.AllTech)
				return Producible.Keys;

			return buildableProducibles;
		}

		public bool CanBuild(ActorInfo actor)
		{
			ProductionState ps;
			if (!Producible.TryGetValue(actor, out ps))
				return false;

			return ps.Buildable || developerMode.AllTech;
		}

		void ITick.Tick(Actor self)
		{
			Tick(self);
		}

		protected virtual void Tick(Actor self)
		{
			// PERF: Avoid LINQ when checking whether all production traits are disabled/paused
			var anyEnabledProduction = false;
			var anyUnpausedProduction = false;
			foreach (var p in productionTraits)
			{
				anyEnabledProduction |= !p.IsTraitDisabled;
				anyUnpausedProduction |= !p.IsTraitPaused;
			}

			if (!anyEnabledProduction)
				ClearQueue();

			Enabled = IsValidFaction && anyEnabledProduction;
			TickInner(self, !anyUnpausedProduction);
		}

		protected virtual void TickInner(Actor self, bool allProductionPaused)
		{
			CancelUnbuildableItems();

			if (Queue.Count > 0 && !allProductionPaused)
				Queue[0].Tick(playerResources);
		}

		protected void CancelUnbuildableItems()
		{
			var buildableNames = BuildableItems().Select(b => b.Name).ToList();

			// EndProduction removes the item from the queue, so we enumerate
			// by index in reverse to avoid issues with index reassignment
			for (var i = Queue.Count - 1; i >= 0; i--)
			{
				if (buildableNames.Contains(Queue[i].Item))
					continue;

				// Refund what's been paid so far
				playerResources.GiveCash(Queue[i].TotalCost - Queue[i].RemainingCost);
				EndProduction(Queue[i]);
			}
		}

		public bool CanQueue(ActorInfo actor, out string notificationAudio)
		{
			notificationAudio = Info.BlockedAudio;

			var bi = actor.TraitInfoOrDefault<BuildableInfo>();
			if (bi == null)
				return false;

			if (!developerMode.AllTech)
			{
				if (Info.QueueLimit > 0 && Queue.Count >= Info.QueueLimit)
				{
					notificationAudio = Info.LimitedAudio;
					return false;
				}

				var queueCount = Queue.Count(i => i.Item == actor.Name);
				if (Info.ItemLimit > 0 && queueCount >= Info.ItemLimit)
				{
					notificationAudio = Info.LimitedAudio;
					return false;
				}

				if (bi.BuildLimit > 0)
				{
					var owned = self.Owner.World.ActorsHavingTrait<Buildable>()
						.Count(a => a.Info.Name == actor.Name && a.Owner == self.Owner);
					if (queueCount + owned >= bi.BuildLimit)
						return false;
				}
			}

			notificationAudio = Info.QueuedAudio;
			return true;
		}

		public void ResolveOrder(Actor self, Order order)
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

					var valued = unit.TraitInfoOrDefault<ValuedInfo>();
					var cost = valued != null ? valued.Cost : 0;
					var time = GetBuildTime(unit, bi);
					var amountToBuild = Math.Min(fromLimit, order.ExtraData);
					for (var n = 0; n < amountToBuild; n++)
					{
						var hasPlayedSound = false;
						BeginProduction(new ProductionItem(this, order.TargetString, cost, playerPower, () => self.World.AddFrameEndTask(_ =>
						{
							var isBuilding = unit.HasTraitInfo<BuildingInfo>();

							if (isBuilding && !hasPlayedSound)
								hasPlayedSound = Game.Sound.PlayNotification(rules, self.Owner, "Speech", Info.ReadyAudio, self.Owner.Faction.InternalName);
							else if (!isBuilding)
							{
								if (BuildUnit(unit))
									Game.Sound.PlayNotification(rules, self.Owner, "Speech", Info.ReadyAudio, self.Owner.Faction.InternalName);
								else if (!hasPlayedSound && time > 0)
									hasPlayedSound = Game.Sound.PlayNotification(rules, self.Owner, "Speech", Info.BlockedAudio, self.Owner.Faction.InternalName);
							}
						})), !order.Queued);
					}

					break;
				case "PauseProduction":
					PauseProduction(order.TargetString, order.ExtraData != 0);

					break;
				case "CancelProduction":
					CancelProduction(order.TargetString, order.ExtraData);
					break;
			}
		}

		public virtual int GetBuildTime(ActorInfo unit, BuildableInfo bi)
		{
			if (developerMode.FastBuild)
				return 0;

			var time = bi.BuildDuration;
			if (time == -1)
			{
				var valued = unit.TraitInfoOrDefault<ValuedInfo>();
				time = valued != null ? valued.Cost : 0;
			}

			time = time * bi.BuildDurationModifier * Info.BuildDurationModifier / 10000;
			return time;
		}

		protected void PauseProduction(string itemName, bool paused)
		{
			var item = Queue.FirstOrDefault(a => a.Item == itemName);
			if (item != null)
				item.Pause(paused);
		}

		protected void CancelProduction(string itemName, uint numberToCancel)
		{
			for (var i = 0; i < numberToCancel; i++)
				if (!CancelProductionInner(itemName))
					break;
		}

		bool CancelProductionInner(string itemName)
		{
			var item = Queue.LastOrDefault(a => a.Item == itemName);

			if (item != null)
			{
				if (item.Infinite)
				{
					item.Infinite = false;
					for (var i = 1; i < Info.InfiniteBuildLimit; i++)
						Queue.Add(new ProductionItem(this, item.Item, item.TotalCost, playerPower, item.OnComplete));
				}
				else
				{
					// Refund what has been paid
					playerResources.GiveCash(item.TotalCost - item.RemainingCost);
					EndProduction(item);
				}

				return true;
			}

			return false;
		}

		public void EndProduction(ProductionItem item)
		{
			Queue.Remove(item);

			if (item.Infinite)
				Queue.Add(new ProductionItem(this, item.Item, item.TotalCost, playerPower, item.OnComplete) { Infinite = true });
		}

		protected virtual void BeginProduction(ProductionItem item, bool hasPriority)
		{
			if (Queue.Any(i => i.Item == item.Item && i.Infinite))
				return;

			if (hasPriority && Queue.Count > 1)
				Queue.Insert(1, item);
			else
				Queue.Add(item);

			if (Info.InfiniteBuildLimit < 0)
				return;

			var queued = Queue.FindAll(i => i.Item == item.Item);

			if (queued.Count <= Info.InfiniteBuildLimit)
				return;

			queued[0].Infinite = true;

			for (var i = 1; i < queued.Count; i++)
			{
				// Refund what has been paid
				playerResources.GiveCash(queued[i].TotalCost - queued[i].RemainingCost);
				EndProduction(queued[i]);
			}
		}

		public virtual int RemainingTimeActual(ProductionItem item)
		{
			return item.RemainingTimeActual;
		}

		// Returns the actor/trait that is most likely (but not necessarily guaranteed) to produce something in this queue
		public virtual TraitPair<Production> MostLikelyProducer()
		{
			var traits = productionTraits.Where(p => !p.IsTraitDisabled && p.Info.Produces.Contains(Info.Type));
			var unpaused = traits.FirstOrDefault(a => !a.IsTraitPaused);
			return new TraitPair<Production>(self, unpaused != null ? unpaused : traits.FirstOrDefault());
		}

		// Builds a unit from the actor that holds this queue (1 queue per building)
		// Returns false if the unit can't be built
		protected virtual bool BuildUnit(ActorInfo unit)
		{
			var mostLikelyProducerTrait = MostLikelyProducer().Trait;

			// Cannot produce if I'm dead or trait is disabled
			if (!self.IsInWorld || self.IsDead || mostLikelyProducerTrait == null)
			{
				CancelProduction(unit.Name, 1);
				return false;
			}

			var inits = new TypeDictionary
			{
				new OwnerInit(self.Owner),
				new FactionInit(BuildableInfo.GetInitialFaction(unit, Faction))
			};

			var bi = unit.TraitInfo<BuildableInfo>();
			var type = developerMode.AllTech ? Info.Type : (bi.BuildAtProductionType ?? Info.Type);

			if (!mostLikelyProducerTrait.IsTraitPaused && mostLikelyProducerTrait.Produce(self, unit, type, inits))
			{
				EndProduction(Queue.FirstOrDefault(i => i.Done && i.Item == unit.Name));
				return true;
			}

			return false;
		}
	}

	public class ProductionState
	{
		public bool Visible = true;
		public bool Buildable = false;
	}

	public class ProductionItem
	{
		public readonly string Item;
		public readonly ProductionQueue Queue;
		public readonly int TotalCost;
		public readonly Action OnComplete;

		public int TotalTime { get; private set; }
		public int RemainingTime { get; private set; }
		public int RemainingCost { get; private set; }
		public int RemainingTimeActual
		{
			get
			{
				return (pm == null || pm.PowerState == PowerState.Normal) ? RemainingTime :
					RemainingTime * Queue.Info.LowPowerModifier / 100;
			}
		}

		public bool Paused { get; private set; }
		public bool Done { get; private set; }
		public bool Started { get; private set; }
		public int Slowdown { get; private set; }
		public bool Infinite { get; set; }

		readonly ActorInfo ai;
		readonly BuildableInfo bi;
		readonly PowerManager pm;

		public ProductionItem(ProductionQueue queue, string item, int cost, PowerManager pm, Action onComplete)
		{
			Item = item;
			RemainingTime = TotalTime = 1;
			RemainingCost = TotalCost = cost;
			OnComplete = onComplete;
			Queue = queue;
			this.pm = pm;
			ai = Queue.Actor.World.Map.Rules.Actors[Item];
			bi = ai.TraitInfo<BuildableInfo>();
			Infinite = false;
		}

		public void Tick(PlayerResources pr)
		{
			if (!Started)
			{
				var time = Queue.GetBuildTime(ai, bi);
				if (time > 0)
					RemainingTime = TotalTime = time;

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

			if (pm != null && pm.PowerState != PowerState.Normal)
			{
				Slowdown -= 100;
				if (Slowdown < 0)
					Slowdown = Queue.Info.LowPowerModifier + Slowdown;
				else
					return;
			}

			var expectedRemainingCost = RemainingTime == 1 ? 0 : TotalCost * RemainingTime / Math.Max(1, TotalTime);
			var costThisFrame = RemainingCost - expectedRemainingCost;
			if (costThisFrame != 0 && !pr.TakeCash(costThisFrame, true))
				return;

			RemainingCost -= costThisFrame;
			RemainingTime -= 1;
			if (RemainingTime > 0)
				return;

			Done = true;
		}

		public void Pause(bool paused) { Paused = paused; }
	}
}
