using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IjwFramework.Collections;

namespace OpenRa.Game.Traits
{
	class ProductionQueue : IOrder, ITick
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

		public Order IssueOrder( Actor self, int2 xy, MouseInput mi, Actor underCursor )
		{
			// production isn't done by clicks in the world; the chrome handles it.
			return null;
		}

		public void ResolveOrder( Actor self, Order order )
		{
			switch( order.OrderString )
			{
			case "StartProduction":
				{
					string group = Rules.UnitCategory[ order.TargetString ];
					var ui = Rules.UnitInfo[ order.TargetString ];
					var time = ui.Cost
						* Rules.General.BuildSpeed						/* todo: country-specific build speed bonus */
						 * ( 25 * 60 ) /* frames per min */				/* todo: build acceleration, if we do that */
						 / 1000;

					time = .08f * time;						/* temporary hax so we can build stuff fast for test */

					if( !Rules.TechTree.BuildableItems( order.Player, group ).Contains( order.TargetString ) )
						return;	/* you can't build that!! */

					bool hasPlayedSound = false;

					BeginProduction( group,
						new ProductionItem( order.TargetString, (int)time, ui.Cost,
							() => Game.world.AddFrameEndTask(
								_ =>
								{
									var isBuilding = group == "Building" || group == "Defense";
									if( !hasPlayedSound && order.Player == Game.LocalPlayer )
									{
										Sound.Play( isBuilding ? "conscmp1.aud" : "unitrdy1.aud" );
										hasPlayedSound = true;
									}
									if( !isBuilding )
										BuildUnit( order.TargetString );
								} ) ) );
					break;
				}
			case "PauseProduction":
				{
					var producing = CurrentItem( Rules.UnitCategory[ order.TargetString ] );
					if( producing != null && producing.Item == order.TargetString )
						producing.Paused = ( order.TargetLocation.X != 0 );
					break;
				}
			case "CancelProduction":
				{
					var producing = CurrentItem( Rules.UnitCategory[ order.TargetString ] );
					if( producing != null && producing.Item == order.TargetString )
						CancelProduction( Rules.UnitCategory[ order.TargetString ] );
					break;
				}
			}
		}

		// Key: Production category.
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

		public void CancelProduction( string category )
		{
			var queue = production[ category ];
			if (queue.Count == 0) return;

			var lastIndex = queue.FindLastIndex( a => a.Item == queue[0].Item );
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
			var newUnitType = Rules.UnitInfo[ name ];
			var producerTypes = Rules.TechTree.UnitBuiltAt( newUnitType );

			// TODO: choose producer based on "primary building"
			var producer = Game.world.Actors
				.Where( x => producerTypes.Contains( x.Info ) && x.Owner == self.Owner )
				.FirstOrDefault();

			if( producer == null )
			{
				CancelProduction( Rules.UnitCategory[ name ] );
				return;
			}

			if( producer.traits.WithInterface<IProducer>().Any( p => p.Produce( producer, newUnitType ) ) )
				FinishProduction( Rules.UnitCategory[ name ] );
		}
	}
}
