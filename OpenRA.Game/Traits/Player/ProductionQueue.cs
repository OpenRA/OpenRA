#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;

namespace OpenRA.Traits
{
	public class ProductionQueueInfo : ITraitInfo
	{
		public readonly string Type = null;
		public float BuildSpeed = 0.4f;
		public readonly int LowPowerSlowdown = 3;
		public object Create(ActorInitializer init) { return new ProductionQueue(init.self, this); }
	}

	public class ProductionQueue : IResolveOrder, ITick
	{
		public readonly Actor self;
		public ProductionQueueInfo Info;
		// TODO: sync this
		List<ProductionItem> Producing = new List<ProductionItem>();

		public ProductionQueue( Actor self, ProductionQueueInfo info )
		{
			this.self = self;
			this.Info = info;
		}

		public ProductionItem CurrentItem()
		{
			return Producing.ElementAtOrDefault(0);
		}
		
		public IEnumerable<ProductionItem> AllItems()
		{
			return Producing;
		}
		
		public void Tick( Actor self )
		{			
			while( Producing.Count > 0 && !Rules.TechTree.BuildableItems( self.Owner, Info.Type ).Contains( Producing[ 0 ].Item ) )
			{
				self.Owner.PlayerActor.Trait<PlayerResources>().GiveCash(Producing[0].TotalCost - Producing[0].RemainingCost); // refund what's been paid so far.
				FinishProduction();
			}
			if( Producing.Count > 0 )
				Producing[ 0 ].Tick( self.Owner );
		}

		public void ResolveOrder( Actor self, Order order )
		{
			switch( order.OrderString )
			{
			case "StartProduction":
				{
					var unit = Rules.Info[order.TargetString];
					if (unit.Category != Info.Type)
						return; /* Not built by this queue */
				
					var cost = unit.Traits.Contains<ValuedInfo>() ? unit.Traits.Get<ValuedInfo>().Cost : 0;
					var time = GetBuildTime(order.TargetString);

					if (!Rules.TechTree.BuildableItems(order.Player, unit.Category).Contains(order.TargetString))
						return;	/* you can't build that!! */
				
					bool hasPlayedSound = false;
					
					for (var n = 0; n < order.TargetLocation.X; n++)	// repeat count
					{
						BeginProduction(new ProductionItem(this, order.TargetString, (int)time, cost,
								() => self.World.AddFrameEndTask(
									_ =>
									{
										var isBuilding = unit.Traits.Contains<BuildingInfo>();
										if (!hasPlayedSound)
										{
											var eva = self.World.WorldActor.Info.Traits.Get<EvaAlertsInfo>();
											Sound.PlayToPlayer(order.Player, isBuilding ? eva.BuildingReadyAudio : eva.UnitReadyAudio);
											hasPlayedSound = true;
										}
										if (!isBuilding)
											BuildUnit(order.TargetString);
									})));
					}
					break;
				}
			case "PauseProduction":
				{
					if (Rules.Info[ order.TargetString ].Category != Info.Type)
						return; /* Not built by this queue */	

					if( Producing.Count > 0 && Producing[0].Item == order.TargetString )
						Producing[0].Paused = ( order.TargetLocation.X != 0 );
					break;
				}
			case "CancelProduction":
				{
					CancelProduction(order.TargetString);
					break;
				}
			}
		}
		
		public int GetBuildTime(String unitString)
		{
			var unit = Rules.Info[unitString];
			if (unit == null || ! unit.Traits.Contains<BuildableInfo>())
				return 0;
			
			if (Game.LobbyInfo.GlobalSettings.AllowCheats && self.Owner.PlayerActor.Trait<DeveloperMode>().FastBuild) return 0;
			var cost = unit.Traits.Contains<ValuedInfo>() ? unit.Traits.Get<ValuedInfo>().Cost : 0;
			var time = cost
				* Info.BuildSpeed
				* (25 * 60) /* frames per min */				/* todo: build acceleration, if we do that */
				 / 1000;
			return (int) time;
		}

		void CancelProduction( string itemName )
		{
			var category = Rules.Info[itemName].Category;
			
			if (category != Info.Type || Producing.Count == 0)
				return; // Nothing to do here
			
			var lastIndex = Producing.FindLastIndex( a => a.Item == itemName );
			if (lastIndex > 0)
			{
				Producing.RemoveAt(lastIndex);
			}
			else if( lastIndex == 0 )
			{
				var item = Producing[0];
				self.Owner.PlayerActor.Trait<PlayerResources>().GiveCash(item.TotalCost - item.RemainingCost); // refund what's been paid so far.
				FinishProduction();
			}
		}

		public void FinishProduction()
		{
			if (Producing.Count == 0) return;
			Producing.RemoveAt(0);
		}

		void BeginProduction( ProductionItem item )
		{
			Producing.Add(item);
		}

		static bool IsDisabledBuilding(Actor a)
		{
			var building = a.TraitOrDefault<Building>();
			return building != null && building.Disabled;
		}

		void BuildUnit( string name )
		{			
			// If the actor has a production trait, use it.
			var sp = self.TraitsImplementing<Production>().Where(p => p.Info.Produces.Contains(Info.Type)).FirstOrDefault();
			if (sp != null)
			{
				if (!IsDisabledBuilding(self) && sp.Produce(self, Rules.Info[ name ]))
					FinishProduction();
				return;
			}
						
			var producers = self.World.Queries.OwnedBy[self.Owner]
				.WithTrait<Production>()
				.Where(x => x.Trait.Info.Produces.Contains(Info.Type))
				.OrderByDescending(x => x.Actor.IsPrimaryBuilding() ? 1 : 0 ) // prioritize the primary.
				.ToArray();

			if (producers.Length == 0)
			{
				CancelProduction(name);
				return;
			}
			
			foreach (var p in producers)
			{
				if (IsDisabledBuilding(p.Actor)) continue;

				if (p.Trait.Produce(p.Actor, Rules.Info[ name ]))
				{
					FinishProduction();
					break;
				}
			}
		}
	}

	public class ProductionItem
	{
		public readonly string Item;
		public readonly ProductionQueue Queue;
		public readonly int TotalTime;
		public readonly int TotalCost;
		public int RemainingTime { get; private set; }
		public int RemainingCost { get; private set; }

		public bool Paused = false, Done = false;
		public Action OnComplete;

		int slowdown = 0;

		public ProductionItem(ProductionQueue queue, string item, int time, int cost, Action onComplete)
		{
			if (time <= 0) time = 1;
			Item = item;
			RemainingTime = TotalTime = time;
			RemainingCost = TotalCost = cost;
			OnComplete = onComplete;
			Queue = queue;

			Log.Write("debug", "new ProductionItem: {0} time={1} cost={2}", item, time, cost);
		}

		public void Tick(Player player)
		{
			if (Done)
			{
				if (OnComplete != null) OnComplete();
				return;
			}

			if (Paused) return;

			if (player.PlayerActor.Trait<PlayerResources>().GetPowerState() != PowerState.Normal)
			{
				if (--slowdown <= 0)
					slowdown = Queue.Info.LowPowerSlowdown; 
				else
					return;
			}

			var costThisFrame = RemainingCost / RemainingTime;
			if (costThisFrame != 0 && !player.PlayerActor.Trait<PlayerResources>().TakeCash(costThisFrame)) return;
			RemainingCost -= costThisFrame;
			RemainingTime -= 1;
			if (RemainingTime > 0) return;

			Done = true;
		}
	}
}
