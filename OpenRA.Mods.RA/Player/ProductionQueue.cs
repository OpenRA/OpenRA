#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Traits;
using OpenRA.FileFormats;

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

		[Desc("This value is used to translate the unit cost into build time.")]
		public float BuildSpeed = 0.4f;
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

		public virtual object Create(ActorInitializer init) { return new ProductionQueue(init.self, init.self.Owner.PlayerActor, this); }
	}

	public class ProductionQueue : IResolveOrder, ITick, ITechTreeElement, INotifyCapture, INotifyKilled, INotifySold, ISync, INotifyTransform
	{
		public readonly Actor self;
		public ProductionQueueInfo Info;
		PowerManager PlayerPower;
		PlayerResources playerResources;
		readonly CountryInfo Race;

		// A list of things we are currently building
		public List<ProductionItem> Queue = new List<ProductionItem>();

		[Sync] public int QueueLength { get { return Queue.Count; } }
		[Sync] public int CurrentRemainingCost { get { return QueueLength == 0 ? 0 : Queue[0].RemainingCost; } }
		[Sync] public int CurrentRemainingTime { get { return QueueLength == 0 ? 0 : Queue[0].RemainingTime; } }
		[Sync] public int CurrentSlowdown { get { return QueueLength == 0 ? 0 : Queue[0].slowdown; } }
		[Sync] public bool CurrentPaused { get { return QueueLength != 0 && Queue[0].Paused; } }
		[Sync] public bool CurrentDone { get { return QueueLength != 0 && Queue[0].Done; } }

		// A list of things we could possibly build, even if our race doesn't normally get it
		public Dictionary<ActorInfo, ProductionState> Produceable;

		public ProductionQueue( Actor self, Actor playerActor, ProductionQueueInfo info )
		{
			this.self = self;
			this.Info = info;
			playerResources = playerActor.Trait<PlayerResources>();
			PlayerPower = playerActor.Trait<PowerManager>();

			Race = self.Owner.Country;
			Produceable = InitTech(playerActor);
		}

		void ClearQueue()
		{
			if (Queue.Count == 0)
				return;

			// Refund the current item
			playerResources.GiveCash(Queue[0].TotalCost - Queue[0].RemainingCost);
			Queue.Clear();
		}

		public void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			PlayerPower = newOwner.PlayerActor.Trait<PowerManager>();
			playerResources = newOwner.PlayerActor.Trait<PlayerResources>();
			ClearQueue();

			// Produceable contains the tech from the original owner - this is desired so we don't clear it.
			Produceable = InitTech(self.Owner.PlayerActor);

			// Force a third(!) tech tree update to ensure that prerequisites are correct.
			// The first two updates are triggered by adding/removing the actor when
			// changing ownership, *before* the new techtree watchers have been set up.
			// This is crap.
			self.Owner.PlayerActor.Trait<TechTree>().Update();
		}

		public void Killed(Actor killed, AttackInfo e) { if (killed == self) ClearQueue(); }
		public void Selling(Actor self) {}
		public void Sold(Actor self) { ClearQueue(); }
		public void OnTransform(Actor self) { ClearQueue(); }

		Dictionary<ActorInfo, ProductionState> InitTech(Actor playerActor)
		{
			var tech = new Dictionary<ActorInfo, ProductionState>();
			var ttc = playerActor.Trait<TechTree>();

			foreach (var a in AllBuildables(Info.Type))
			{
				var bi = a.Traits.Get<BuildableInfo>();
				// Can our race build this by satisfying normal prereqs?
				var buildable = bi.Owner.Contains(Race.Race);
				tech.Add(a, new ProductionState { Visible = buildable && !bi.Hidden });
				if (buildable)
					ttc.Add(a.Name, bi, this);
			}

			return tech;
		}

		IEnumerable<ActorInfo> AllBuildables(string category)
		{
			return Rules.Info.Values
				.Where( x => x.Name[ 0 ] != '^' )
				.Where( x => x.Traits.Contains<BuildableInfo>() )
				.Where( x => x.Traits.Get<BuildableInfo>().Queue == category );
		}

		public void OverrideProduction(ActorInfo type, bool buildable)
		{
			Produceable[type].Buildable = buildable;
			Produceable[type].Sticky = true;
		}

		public void PrerequisitesAvailable(string key)
		{
			var ps = Produceable[ Rules.Info[key] ];
			if (!ps.Sticky)
				ps.Buildable = true;
		}

		public void PrerequisitesUnavailable(string key)
		{
			var ps = Produceable[ Rules.Info[key] ];
			if (!ps.Sticky)
				ps.Buildable = false;
		}

		public ProductionItem CurrentItem()
		{
			return Queue.ElementAtOrDefault(0);
		}

		public IEnumerable<ProductionItem> AllQueued()
		{
			return Queue;
		}

		public virtual IEnumerable<ActorInfo> AllItems()
		{
			if (self.World.LobbyInfo.GlobalSettings.AllowCheats && self.Owner.PlayerActor.Trait<DeveloperMode>().AllTech)
				return Produceable.Select(a => a.Key);

			return Produceable.Where(a => a.Value.Buildable || a.Value.Visible).Select(a => a.Key);
		}

		public virtual IEnumerable<ActorInfo> BuildableItems()
		{
			if (self.World.LobbyInfo.GlobalSettings.AllowCheats && self.Owner.PlayerActor.Trait<DeveloperMode>().AllTech)
				return Produceable.Select(a => a.Key);

			return Produceable.Where(a => a.Value.Buildable).Select(a => a.Key);
		}

		public bool CanBuild(ActorInfo actor)
		{
			return Produceable.ContainsKey(actor) && Produceable[actor].Buildable;
		}

		public virtual void Tick(Actor self)
		{
			while (Queue.Count > 0 && BuildableItems().All(b => b.Name != Queue[ 0 ].Item))
			{
				playerResources.GiveCash(Queue[0].TotalCost - Queue[0].RemainingCost); // refund what's been paid so far.
				FinishProduction();
			}
			if (Queue.Count > 0)
				Queue[ 0 ].Tick(playerResources);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			switch(order.OrderString)
			{
			case "StartProduction":
				{
					var unit = Rules.Info[order.TargetString];
					var bi = unit.Traits.Get<BuildableInfo>();
					if (bi.Queue != Info.Type)
						return; /* Not built by this queue */

					var cost = unit.Traits.Contains<ValuedInfo>() ? unit.Traits.Get<ValuedInfo>().Cost : 0;
					var time = GetBuildTime(order.TargetString);

					if (BuildableItems().All(b => b.Name != order.TargetString))
						return;	/* you can't build that!! */

					// Check if the player is trying to build more units that they are allowed
					if (bi.BuildLimit > 0)
					{
						var inQueue = Queue.Count(pi => pi.Item == order.TargetString);
						var owned = self.Owner.World.ActorsWithTrait<Buildable>().Count(a => a.Actor.Info.Name == order.TargetString && a.Actor.Owner == self.Owner);
						if (inQueue + owned >= bi.BuildLimit)
							return;
					}

					for (var n = 0; n < order.TargetLocation.X; n++) // repeat count
					{
						bool hasPlayedSound = false;
						BeginProduction(new ProductionItem(this, order.TargetString, cost, PlayerPower,
								() => self.World.AddFrameEndTask(_ =>
								{
									var isBuilding = unit.Traits.Contains<BuildingInfo>();
									if (isBuilding && !hasPlayedSound)
									{
										hasPlayedSound = Sound.PlayNotification(self.Owner, "Speech", Info.ReadyAudio, self.Owner.Country.Race);
									}
									else if (!isBuilding)
									{
										if (BuildUnit(order.TargetString))
											Sound.PlayNotification(self.Owner, "Speech", Info.ReadyAudio, self.Owner.Country.Race);
										else if (!hasPlayedSound && time > 0)
										{
											hasPlayedSound = Sound.PlayNotification(self.Owner, "Speech", Info.BlockedAudio, self.Owner.Country.Race);
										}
									}
								})));
					}
					break;
				}
			case "PauseProduction":
				{
					if (Queue.Count > 0 && Queue[0].Item == order.TargetString)
						Queue[0].Paused = ( order.TargetLocation.X != 0 );
					break;
				}
			case "CancelProduction":
				{
					CancelProduction(order.TargetString, order.TargetLocation.X);
					break;
				}
			}
		}

		virtual public int GetBuildTime(String unitString)
		{
			var unit = Rules.Info[unitString];
			if (unit == null || ! unit.Traits.Contains<BuildableInfo>())
				return 0;

			if (self.World.LobbyInfo.GlobalSettings.AllowCheats && self.Owner.PlayerActor.Trait<DeveloperMode>().FastBuild)
				return 0;

			var time = unit.GetBuildTime() * Info.BuildSpeed;

			return (int) time;
		}

		protected void CancelProduction(string itemName, int numberToCancel)
		{
			for (var i = 0; i < numberToCancel; i++)
				CancelProductionInner(itemName);
		}

		void CancelProductionInner(string itemName)
		{
			var lastIndex = Queue.FindLastIndex(a => a.Item == itemName);

			if (lastIndex > 0)
				Queue.RemoveAt(lastIndex);
			else if (lastIndex == 0)
			{
				var item = Queue[0];
				playerResources.GiveCash(item.TotalCost - item.RemainingCost);	// refund what has been paid
				FinishProduction();
			}
		}

		public void FinishProduction()
		{
			if (Queue.Count == 0) return;
			Queue.RemoveAt(0);
		}

		protected void BeginProduction(ProductionItem item)
		{
			Queue.Add(item);
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
			if (sp != null && !self.IsDisabled() && sp.Produce(self, Rules.Info[name]))
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
		public bool Sticky = false;
	}

	public class ProductionItem
	{
		public readonly string Item;
		public readonly ProductionQueue Queue;
		readonly PowerManager pm;
		public int TotalTime;
		public readonly int TotalCost;
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

		public bool Paused = false, Done = false, Started = false;
		public Action OnComplete;
		public int slowdown = 0;

		public ProductionItem(ProductionQueue queue, string item, int cost, PowerManager pm, Action onComplete)
		{
			Item = item;
			RemainingTime = TotalTime = 1;
			RemainingCost = TotalCost = cost;
			OnComplete = onComplete;
			Queue = queue;
			this.pm = pm;
			//Log.Write("debug", "new ProductionItem: {0} time={1} cost={2}", item, time, cost);
		}

		public void Tick(PlayerResources pr)
		{
			if (!Started)
			{
				var time = Queue.GetBuildTime(Item);
				if (time > 0) RemainingTime = TotalTime = time;
				Started = true;
			}

			if (Done)
			{
				if (OnComplete != null) OnComplete();
				return;
			}

			if (Paused) return;

			if (pm.PowerState != PowerState.Normal)
			{
				if (--slowdown <= 0)
					slowdown = Queue.Info.LowPowerSlowdown;
				else
					return;
			}

			var costThisFrame = RemainingCost / RemainingTime;
			if (costThisFrame != 0 && !pr.TakeCash(costThisFrame)) return;
			RemainingCost -= costThisFrame;
			RemainingTime -= 1;
			if (RemainingTime > 0) return;

			Done = true;
		}
	}
}
