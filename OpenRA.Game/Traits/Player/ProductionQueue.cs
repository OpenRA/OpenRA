#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;

namespace OpenRA.Traits
{
	class ProductionQueueInfo : ITraitInfo
	{
		public object Create(Actor self) { return new ProductionQueue(self); }
	}

	class ProductionQueue : IResolveOrder, ITick
	{
		Actor self;

		public ProductionQueue( Actor self )
		{
			this.self = self;
		}

		public void Tick( Actor self )
		{
			foreach( var p in production )
				if( p.Value.Count > 0 )
					(p.Value)[0].Tick( self.Owner );
		}

		public void ResolveOrder( Actor self, Order order )
		{
			switch( order.OrderString )
			{
			case "StartProduction":
				{
					for (var n = 0; n < order.TargetLocation.X; n++)	// repeat count
					{
						var unit = Rules.Info[order.TargetString];
						var ui = unit.Traits.Get<BuildableInfo>();
						var time = ui.Cost
							* self.World.Defaults.BuildSpeed						/* todo: country-specific build speed bonus */
							 * (25 * 60) /* frames per min */				/* todo: build acceleration, if we do that */
							 / 1000;

						if (!Rules.TechTree.BuildableItems(order.Player, unit.Category).Contains(order.TargetString))
							return;	/* you can't build that!! */

						bool hasPlayedSound = false;

						BeginProduction(unit.Category,
							new ProductionItem(order.TargetString, (int)time, ui.Cost,
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
					var producing = CurrentItem( Rules.Info[ order.TargetString ].Category );
					if( producing != null && producing.Item == order.TargetString )
						producing.Paused = ( order.TargetLocation.X != 0 );
					break;
				}
			case "CancelProduction":
				{
					CancelProduction(order.TargetString);
					break;
				}
			}
		}

		// Key: Production category.
		// TODO: sync this
		readonly Cache<string, List<ProductionItem>> production 
			= new Cache<string, List<ProductionItem>>( _ => new List<ProductionItem>() );

		public ProductionItem CurrentItem(string category)
		{
			return production[category].ElementAtOrDefault(0);
		}

		public IEnumerable<ProductionItem> AllItems(string category)
		{
			return production[category];
		}

		public void CancelProduction( string itemName )
		{
			var category = Rules.Info[itemName].Category;
			var queue = production[ category ];
			if (queue.Count == 0) return;

			var lastIndex = queue.FindLastIndex( a => a.Item == itemName );
			if (lastIndex > 0)
			{
				queue.RemoveAt(lastIndex);
			}
			else
			{
				var item = queue[0];
				self.Owner.GiveCash(item.TotalCost - item.RemainingCost); // refund what's been paid so far.
				FinishProduction(category);
			}
		}

		public void FinishProduction( string category )
		{
			var queue = production[category];
			if (queue.Count == 0) return;
			queue.RemoveAt(0);
		}

		public void BeginProduction( string group, ProductionItem item )
		{
			production[group].Add(item);
		}

		public void BuildUnit( string name )
		{
			var newUnitType = Rules.Info[ name ];
			var producerTypes = Rules.TechTree.UnitBuiltAt( newUnitType );
			Actor producer = null;
			
			// Prioritise primary structure in build order
			var primaryProducers = self.World.Queries.OwnedBy[self.Owner]
				.WithTrait<Production>()
				.Where(x => producerTypes.Contains(x.Actor.Info)
					&& x.Trait.IsPrimary);
			
			foreach (var p in primaryProducers)
			{
				// Ignore buildings that are disabled
				if (p.Actor.traits.Contains<Building>() && p.Actor.traits.Get<Building>().Disabled)
					continue;
				producer = p.Actor;
				break;
			}
			
			// TODO: Be smart about disabled buildings. Units in progress should be paused(?)
			// Ignore this for now
			
			// Pick the first available producer
			if (producer == null)
			{
				producer = self.World.Queries.OwnedBy[self.Owner]
					.Where( x => producerTypes.Contains( x.Info ) )
					.FirstOrDefault();
			}
			
			// Something went wrong somewhere...
			if( producer == null )
			{
				CancelProduction( name );
				return;
			}

			if( producer.traits.WithInterface<IProducer>().Any( p => p.Produce( producer, newUnitType ) ) )
				FinishProduction( newUnitType.Category );
		}
	}

	class ProductionItem
	{
		public readonly string Item;

		public readonly int TotalTime;
		public readonly int TotalCost;
		public int RemainingTime { get; private set; }
		public int RemainingCost { get; private set; }

		public bool Paused = false, Done = false;
		public Action OnComplete;

		int slowdown = 0;

		public ProductionItem(string item, int time, int cost, Action onComplete)
		{
			if (time <= 0)
				time = 1;
			Item = item;
			RemainingTime = TotalTime = time;
			RemainingCost = TotalCost = cost;
			OnComplete = onComplete;
		}

		public void Tick(Player player)
		{
			if (Done)
			{
				if (OnComplete != null) OnComplete();
				return;
			}

			if (Paused) return;

			if (player.GetPowerState() != PowerState.Normal)
			{
				if (--slowdown <= 0)
					slowdown = player.World.Defaults.LowPowerSlowdown;
				else
					return;
			}

			var costThisFrame = RemainingCost / RemainingTime;
			if (costThisFrame != 0 && !player.TakeCash(costThisFrame)) return;

			RemainingCost -= costThisFrame;
			RemainingTime -= 1;
			if (RemainingTime > 0) return;

			Done = true;
		}
	}
}
