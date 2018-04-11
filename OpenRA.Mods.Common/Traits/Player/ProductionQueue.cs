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

		[Desc("The build time is multiplied with this value on low power.")]
		public readonly int LowPowerSlowdown = 3;

		[Desc("Notification played when production is complete.",
			"The filename of the audio is defined per faction in notifications.yaml.")]
		public readonly string ReadyAudio = "UnitReady";

		[Desc("Notification played when you can't train another unit",
			"when the build limit exceeded or the exit is jammed.",
			"The filename of the audio is defined per faction in notifications.yaml.")]
		public readonly string BlockedAudio = "NoBuild";

		[Desc("Notification played when user clicks on the build palette icon.",
			"The filename of the audio is defined per faction in notifications.yaml.")]
		public readonly string QueuedAudio = "Training";

		[Desc("Notification played when player right-clicks on the build palette icon.",
			"The filename of the audio is defined per faction in notifications.yaml.")]
		public readonly string OnHoldAudio = "OnHold";

		[Desc("Notification played when player right-clicks on a build palette icon that is already on hold.",
			"The filename of the audio is defined per faction in notifications.yaml.")]
		public readonly string CancelledAudio = "Cancelled";

		public virtual object Create(ActorInitializer init) { return new ProductionQueue(init, init.Self.Owner.PlayerActor, this); }

		public void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			if (LowPowerSlowdown <= 0)
				throw new YamlException("Production queue must have LowPowerSlowdown of at least 1.");
		}
	}

	public class ProductionQueue : IResolveOrder, ITick, ITechTreeElement, INotifyOwnerChanged, INotifyKilled, INotifySold, ISync, INotifyTransform, INotifyCreated
	{
		public readonly ProductionQueueInfo Info;
		readonly Actor self;

		// A list of things we could possibly build
		readonly Dictionary<ActorInfo, ProductionState> producible = new Dictionary<ActorInfo, ProductionState>();
		readonly List<ProductionItem> queue = new List<ProductionItem>();
		readonly IEnumerable<ActorInfo> allProducibles;
		readonly IEnumerable<ActorInfo> buildableProducibles;

		Production[] productionTraits;

		// Will change if the owner changes
		PowerManager playerPower;
		PlayerResources playerResources;
		protected DeveloperMode developerMode;

		public Actor Actor { get { return self; } }

		[Sync] public int QueueLength { get { return queue.Count; } }
		[Sync] public int CurrentRemainingCost { get { return QueueLength == 0 ? 0 : queue[0].RemainingCost; } }
		[Sync] public int CurrentRemainingTime { get { return QueueLength == 0 ? 0 : queue[0].RemainingTime; } }
		[Sync] public int CurrentSlowdown { get { return QueueLength == 0 ? 0 : queue[0].Slowdown; } }
		[Sync] public bool CurrentPaused { get { return QueueLength != 0 && queue[0].Paused; } }
		[Sync] public bool CurrentDone { get { return QueueLength != 0 && queue[0].Done; } }
		[Sync] public bool Enabled { get; protected set; }

		public string Faction { get; private set; }
		[Sync] public bool IsValidFaction { get; private set; }

		public ProductionQueue(ActorInitializer init, Actor playerActor, ProductionQueueInfo info)
		{
			self = init.Self;
			Info = info;
			playerResources = playerActor.Trait<PlayerResources>();
			playerPower = playerActor.Trait<PowerManager>();
			developerMode = playerActor.Trait<DeveloperMode>();

			Faction = init.Contains<FactionInit>() ? init.Get<FactionInit, string>() : self.Owner.Faction.InternalName;
			IsValidFaction = !info.Factions.Any() || info.Factions.Contains(Faction);
			Enabled = IsValidFaction;

			CacheProducibles(playerActor);
			allProducibles = producible.Where(a => a.Value.Buildable || a.Value.Visible).Select(a => a.Key);
			buildableProducibles = producible.Where(a => a.Value.Buildable).Select(a => a.Key);
		}

		void INotifyCreated.Created(Actor self)
		{
			productionTraits = self.TraitsImplementing<Production>().ToArray();
		}

		protected void ClearQueue()
		{
			if (queue.Count == 0)
				return;

			// Refund the current item
			playerResources.GiveCash(queue[0].TotalCost - queue[0].RemainingCost);
			queue.Clear();
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			ClearQueue();

			playerPower = newOwner.PlayerActor.Trait<PowerManager>();
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
			producible.Clear();
			if (!Enabled)
				return;

			var ttc = playerActor.Trait<TechTree>();

			foreach (var a in AllBuildables(Info.Type))
			{
				var bi = a.TraitInfo<BuildableInfo>();

				producible.Add(a, new ProductionState());
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
			producible[self.World.Map.Rules.Actors[key]].Buildable = true;
		}

		public void PrerequisitesUnavailable(string key)
		{
			producible[self.World.Map.Rules.Actors[key]].Buildable = false;
		}

		public void PrerequisitesItemHidden(string key)
		{
			producible[self.World.Map.Rules.Actors[key]].Visible = false;
		}

		public void PrerequisitesItemVisible(string key)
		{
			producible[self.World.Map.Rules.Actors[key]].Visible = true;
		}

		public ProductionItem CurrentItem()
		{
			return queue.ElementAtOrDefault(0);
		}

		public virtual IEnumerable<ProductionItem> AllQueued()
		{
			return queue;
		}

		public virtual IEnumerable<ActorInfo> AllItems()
		{
			if (developerMode.AllTech)
				return producible.Keys;

			return allProducibles;
		}

		public virtual IEnumerable<ActorInfo> BuildableItems()
		{
			if (!Enabled)
				return Enumerable.Empty<ActorInfo>();
			if (developerMode.AllTech)
				return producible.Keys;

			return buildableProducibles;
		}

		public bool CanBuild(ActorInfo actor)
		{
			ProductionState ps;
			if (!producible.TryGetValue(actor, out ps))
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
			while (queue.Count > 0 && BuildableItems().All(b => b.Name != queue[0].Item))
			{
				// Refund what's been paid so far
				playerResources.GiveCash(queue[0].TotalCost - queue[0].RemainingCost);
				FinishProduction();
			}

			if (queue.Count > 0 && !allProductionPaused)
				queue[0].Tick(playerResources);
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
					if (!developerMode.AllTech && bi.BuildLimit > 0)
					{
						var inQueue = queue.Count(pi => pi.Item == order.TargetString);
						var owned = self.Owner.World.ActorsHavingTrait<Buildable>().Count(a => a.Info.Name == order.TargetString && a.Owner == self.Owner);
						fromLimit = bi.BuildLimit - (inQueue + owned);

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
						})));
					}

					break;
				case "PauseProduction":
					if (queue.Count > 0 && queue[0].Item == order.TargetString)
						queue[0].Pause(order.ExtraData != 0);

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

		protected void CancelProduction(string itemName, uint numberToCancel)
		{
			for (var i = 0; i < numberToCancel; i++)
				CancelProductionInner(itemName);
		}

		void CancelProductionInner(string itemName)
		{
			var lastIndex = queue.FindLastIndex(a => a.Item == itemName);

			if (lastIndex > 0)
				queue.RemoveAt(lastIndex);
			else if (lastIndex == 0)
			{
				var item = queue[0];

				// Refund what has been paid
				playerResources.GiveCash(item.TotalCost - item.RemainingCost);
				FinishProduction();
			}
		}

		public void FinishProduction()
		{
			if (queue.Count != 0)
				queue.RemoveAt(0);
		}

		protected void BeginProduction(ProductionItem item)
		{
			queue.Add(item);
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
				FinishProduction();
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
				return (pm.PowerState == PowerState.Normal) ? RemainingTime :
					RemainingTime * Queue.Info.LowPowerSlowdown;
			}
		}

		public bool Paused { get; private set; }
		public bool Done { get; private set; }
		public bool Started { get; private set; }
		public int Slowdown { get; private set; }

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

			if (pm.PowerState != PowerState.Normal)
			{
				if (--Slowdown <= 0)
					Slowdown = Queue.Info.LowPowerSlowdown;
				else
					return;
			}

			var costThisFrame = RemainingCost / RemainingTime;
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
