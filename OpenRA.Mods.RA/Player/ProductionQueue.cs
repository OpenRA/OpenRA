#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.Common.Power;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Attach this to an actor (usually a building) to let it produce units or construct buildings.", 
		"If one builds another actor of this type, he will get a separate queue to create two actors", 
		"at the same time. Will only work together with the Production: trait.")]
	public class ProductionQueueInfo : ITraitInfo
	{
		[Desc("What kind of production will be added (e.g. Building, Infantry, Vehicle, ...)")]
		public readonly string Type = null;

		[Desc("Group queues from separate buildings together into the same tab.")]
		public readonly string Group = null;

		[Desc("Filter buildable items based on their Owner.")]
		public readonly bool RequireOwner = true;

		[Desc("Only enable this queue for certain factions")]
		public readonly string[] Race = { };

		[Desc("Should the prerequisite remain enabled if the owner changes?")]
		public readonly bool Sticky = true;

		[Desc("This value is used to translate the unit cost into build time.")]
		public readonly float BuildSpeed = 0.4f;

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

		public virtual object Create(ActorInitializer init) { return new ProductionQueue(init, init.self.Owner.PlayerActor, this); }
	}

	public class ProductionQueue : IResolveOrder, ITick, ITechTreeElement, INotifyOwnerChanged, INotifyKilled, INotifySold, ISync, INotifyTransform
	{
		int nextQueueID = 1;

		public readonly ProductionQueueInfo Info;
		public readonly int QueueID;
		readonly Actor self;

		// Will change if the owner changes
		PowerManager playerPower;
		PlayerResources playerResources;
		protected DeveloperMode developerMode;

		// A list of things we could possibly build
		Dictionary<ActorInfo, ProductionState> produceable;
		List<ProductionItem> queue = new List<ProductionItem>();
		bool allTech = false;

		// A list of things we are currently building
		public Actor Actor { get { return self; } }

		[Sync] public int QueueLength { get { return queue.Count; } }
		[Sync] public int CurrentRemainingCost { get { return QueueLength == 0 ? 0 : queue[0].RemainingCost; } }
		[Sync] public int CurrentRemainingTime { get { return QueueLength == 0 ? 0 : queue[0].RemainingTime; } }
		[Sync] public int CurrentSlowdown { get { return QueueLength == 0 ? 0 : queue[0].Slowdown; } }
		[Sync] public bool CurrentPaused { get { return QueueLength != 0 && queue[0].Paused; } }
		[Sync] public bool CurrentDone { get { return QueueLength != 0 && queue[0].Done; } }
		[Sync] public bool Enabled { get; private set; }

		public string Race { get; private set; }
		public int Name = 0;
		public string DisplayName = "";

		public ProductionQueue(ActorInitializer init, Actor playerActor, ProductionQueueInfo info)
		{
			self = init.self;
			Info = info;
			playerResources = playerActor.Trait<PlayerResources>();
			playerPower = playerActor.Trait<PowerManager>();
			developerMode = playerActor.Trait<DeveloperMode>();

			Race = init.Contains<RaceInit>() ? init.Get<RaceInit, string>() : self.Owner.Country.Race;
			Enabled = (!Info.Race.Any() || Info.Race.Contains(Race)) || developerMode.AllTech;
			QueueID = nextQueueID++;

			CacheProduceables(playerActor);
		}

		void ClearQueue()
		{
			if (queue.Count == 0)
				return;

			// Refund the current item
			playerResources.GiveCash(queue[0].TotalCost - queue[0].RemainingCost);
			queue.Clear();
		}

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			ClearQueue();

			playerPower = newOwner.PlayerActor.Trait<PowerManager>();
			playerResources = newOwner.PlayerActor.Trait<PlayerResources>();
			developerMode = newOwner.PlayerActor.Trait<DeveloperMode>();

			if (!Info.Sticky)
			{
				Race = self.Owner.Country.Race;
				Enabled = (!Info.Race.Any() || Info.Race.Contains(Race)) || developerMode.AllTech;
			}

			// Regenerate the produceables and tech tree state
			oldOwner.PlayerActor.Trait<TechTree>().Remove(this);
			CacheProduceables(newOwner.PlayerActor);
			newOwner.PlayerActor.Trait<TechTree>().Update();
		}

		public void Killed(Actor killed, AttackInfo e) { if (killed == self) { ClearQueue(); Enabled = false; } }
		public void Selling(Actor self) { ClearQueue(); Enabled = false; }
		public void Sold(Actor self) { }

		public void BeforeTransform(Actor self) { ClearQueue(); Enabled = false; }
		public void OnTransform(Actor self) { }
		public void AfterTransform(Actor self) { }

		void CacheProduceables(Actor playerActor)
		{
			produceable = new Dictionary<ActorInfo, ProductionState>();
			if (!Enabled)
				return;

			var ttc = playerActor.Trait<TechTree>();

			foreach (var a in AllBuildables(Info.Type))
			{
				var bi = a.Traits.Get<BuildableInfo>();

				// Can our race build this by satisfying normal prerequisites?
				var buildable = !Info.RequireOwner || bi.Owner.Contains(Race);

				// Checks if Prerequisites want to hide the Actor from buildQueue if they are false
				produceable.Add(a, new ProductionState { Visible = buildable });

				if (buildable)
					ttc.Add(a.Name, bi.Prerequisites, bi.BuildLimit, this);
			}
		}

		IEnumerable<ActorInfo> AllBuildables(string category)
		{
			return self.World.Map.Rules.Actors.Values
				.Where(x =>
					x.Name[0] != '^' &&
					x.Traits.Contains<BuildableInfo>() &&
					x.Traits.Get<BuildableInfo>().Queue.Contains(category));
		}

		public void PrerequisitesAvailable(string key)
		{
			produceable[self.World.Map.Rules.Actors[key]].Buildable = true;
		}

		public void PrerequisitesUnavailable(string key)
		{
			produceable[self.World.Map.Rules.Actors[key]].Buildable = false;
		}

		public void PrerequisitesItemHidden(string key)
		{
			produceable[self.World.Map.Rules.Actors[key]].Visible = false;
		}

		public void PrerequisitesItemVisible(string key)
		{
			produceable[self.World.Map.Rules.Actors[key]].Visible = true;
		}

		public ProductionItem CurrentItem()
		{
			return queue.ElementAtOrDefault(0);
		}

		public IEnumerable<ProductionItem> AllQueued()
		{
			return queue;
		}

		public virtual IEnumerable<ActorInfo> AllItems()
		{
			if (self.World.AllowDevCommands && developerMode.AllTech)
				return produceable.Select(a => a.Key);

			return produceable.Where(a => a.Value.Buildable || a.Value.Visible).Select(a => a.Key);
		}

		public virtual IEnumerable<ActorInfo> BuildableItems()
		{
			if (!Enabled)
				return Enumerable.Empty<ActorInfo>();
			if (self.World.AllowDevCommands && developerMode.AllTech)
				return produceable.Select(a => a.Key);

			return produceable.Where(a => a.Value.Buildable).Select(a => a.Key);
		}

		public bool CanBuild(ActorInfo actor)
		{
			ProductionState ps;
			if (!produceable.TryGetValue(actor, out ps))
				return false;

			return ps.Buildable || (self.World.AllowDevCommands && developerMode.AllTech);
		}

		public virtual void Tick(Actor self)
		{
			if (self.World.AllowDevCommands && developerMode.AllTech != allTech)
			{
				allTech = developerMode.AllTech;
				
				Enabled = (!Info.Race.Any() || Info.Race.Contains(Race)) || developerMode.AllTech;
				OnOwnerChanged(self, self.Owner, self.Owner);

				self.World.AddFrameEndTask((World w) => {
					var selected = w.Selection.Contains(self);
					var controlgroup = w.Selection.GetControlGroupForActor(self);

					w.Remove(self); // force production palettes to update
					w.Add(self);

					if (selected)
						w.Selection.Add(w, self);
					if (controlgroup.HasValue)
						w.Selection.AddToControlGroup(self, controlgroup.Value);
				});
			}

			while (queue.Count > 0 && BuildableItems().All(b => b.Name != queue[0].Item))
			{
				playerResources.GiveCash(queue[0].TotalCost - queue[0].RemainingCost); // refund what's been paid so far.
				FinishProduction();
			}

			if (queue.Count > 0)
				queue[0].Tick(playerResources);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (!Enabled)
				return;

			switch (order.OrderString)
			{
			case "StartProduction":
				{
					if (order.ExtraLocation.X != QueueID)
						return;

					var unit = self.World.Map.Rules.Actors[order.TargetString];
					var bi = unit.Traits.Get<BuildableInfo>();
					if (!bi.Queue.Contains(Info.Type))
						return; /* Not built by this queue */

					var cost = unit.Traits.Contains<ValuedInfo>() ? unit.Traits.Get<ValuedInfo>().Cost : 0;
					var time = GetBuildTime(order.TargetString);

					if (BuildableItems().All(b => b.Name != order.TargetString))
						return;	/* you can't build that!! */

					// Check if the player is trying to build more units that they are allowed
					var fromLimit = int.MaxValue;
					if (bi.BuildLimit > 0)
					{
						var inQueue = queue.Count(pi => pi.Item == order.TargetString);
						var owned = self.Owner.World.ActorsWithTrait<Buildable>().Count(a => a.Actor.Info.Name == order.TargetString && a.Actor.Owner == self.Owner);
						fromLimit = bi.BuildLimit - (inQueue + owned);

						if (fromLimit <= 0)
							return;
					}

					var amountToBuild = Math.Min(fromLimit, order.ExtraLocation.Y);
					for (var n = 0; n < amountToBuild; n++)
					{
						var hasPlayedSound = false;
						BeginProduction(new ProductionItem(this, order.TargetString, cost, playerPower, () => self.World.AddFrameEndTask(_ =>
						{
							var isBuilding = unit.Traits.Contains<BuildingInfo>();
							if (isBuilding && !hasPlayedSound)
							{
								hasPlayedSound = Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", Info.ReadyAudio, self.Owner.Country.Race);
							}
							else if (!isBuilding)
							{
								if (BuildUnit(order.TargetString))
									Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", Info.ReadyAudio, self.Owner.Country.Race);
								else if (!hasPlayedSound && time > 0)
								{
									hasPlayedSound = Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", Info.BlockedAudio, self.Owner.Country.Race);
								}
							}
						})));
					}

					break;
				}

			case "PauseProduction":
				{
					if (order.ExtraLocation.X != QueueID)
						return;
					if (queue.Count > 0 && queue[0].Item == order.TargetString)
						queue[0].Pause(order.ExtraLocation.Y != 0);

					break;
				}

			case "CancelProduction":
				{
					if (order.ExtraLocation.X != QueueID)
						return;
					CancelProduction(order.TargetString, (uint)order.ExtraLocation.Y);
					break;
				}
			}
		}

		public virtual int GetBuildTime(string unitString)
		{
			var unit = self.World.Map.Rules.Actors[unitString];
			if (unit == null || !unit.Traits.Contains<BuildableInfo>())
				return 0;

			if (self.World.AllowDevCommands && self.Owner.PlayerActor.Trait<DeveloperMode>().FastBuild)
				return 0;

			var time = unit.GetBuildTime() * Info.BuildSpeed;

			return (int)time;
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
				playerResources.GiveCash(item.TotalCost - item.RemainingCost);	// refund what has been paid
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

		// Builds a unit from the actor that holds this queue (1 queue per building)
		// Returns false if the unit can't be built
		protected virtual bool BuildUnit(string name)
		{
			// Cannot produce if i'm dead
			if (!self.IsInWorld || self.IsDead())
			{
				CancelProduction(name, 1);
				return true;
			}

			var sp = self.TraitsImplementing<Production>().FirstOrDefault(p => p.Info.Produces.Contains(Info.Type));
			if (sp != null && !self.IsDisabled() && sp.Produce(self, self.World.Map.Rules.Actors[name], Race))
			{
				FinishProduction();
				return true;
			}

			return false;
		}
	}

	public class ProductionState
	{
		public bool Visible = false;
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

		readonly PowerManager pm;

		public ProductionItem(ProductionQueue queue, string item, int cost, PowerManager pm, Action onComplete)
		{
			Item = item;
			RemainingTime = TotalTime = 1;
			RemainingCost = TotalCost = cost;
			OnComplete = onComplete;
			Queue = queue;
			this.pm = pm;
		}

		public void Tick(PlayerResources pr)
		{
			if (!Started)
			{
				var time = Queue.GetBuildTime(Item);
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
			if (costThisFrame != 0 && !pr.TakeCash(costThisFrame))
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
