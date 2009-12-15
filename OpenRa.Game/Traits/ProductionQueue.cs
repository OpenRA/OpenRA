using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits
{
	class ProductionQueue : IOrder, ITick
	{
		Actor self;

		public ProductionQueue( Actor self )
		{
			this.self = self;
			foreach( var cat in Rules.Categories.Keys )
				ProductionInit( cat );
		}

		public void Tick( Actor self )
		{
			foreach( var p in production )
				if( p.Value != null )
					p.Value.Tick( self.Owner );
		}

		public Order IssueOrder( Actor self, int2 xy, bool lmb, Actor underCursor )
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
					var producing = Producing( Rules.UnitCategory[ order.TargetString ] );
					if( producing != null && producing.Item == order.TargetString )
						producing.Paused = ( order.TargetLocation.X != 0 );
					break;
				}
			case "CancelProduction":
				{
					var producing = Producing( Rules.UnitCategory[ order.TargetString ] );
					if( producing != null && producing.Item == order.TargetString )
						CancelProduction( Rules.UnitCategory[ order.TargetString ] );
					break;
				}
			}
		}

		// Key: Production category. Categories are: Building, Infantry, Vehicle, Ship, Plane (and one per super, if they're done in here)
		readonly Dictionary<string, ProductionItem> production = new Dictionary<string, ProductionItem>();

		void ProductionInit( string category )
		{
			production.Add( category, null );
		}

		public ProductionItem Producing( string category )
		{
			return production[ category ];
		}

		public void CancelProduction( string category )
		{
			var item = production[ category ];
			if( item == null ) return;
			self.Owner.GiveCash( item.TotalCost - item.RemainingCost ); // refund what's been paid so far.
			FinishProduction( category );
		}

		public void FinishProduction( string category )
		{
			production[ category ] = null;
		}

		public void BeginProduction( string group, ProductionItem item )
		{
			if( production[ group ] != null ) return;
			production[ group ] = item;
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
