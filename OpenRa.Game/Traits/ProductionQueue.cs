using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits
{
	class ProductionQueue : IOrder
	{
		public ProductionQueue( Actor self )
		{

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

					order.Player.BeginProduction( group,
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
										Game.BuildUnit( order.Player, order.TargetString );
								} ) ) );
					break;
				}
			case "PauseProduction":
				{
					var producing = order.Player.Producing( Rules.UnitCategory[ order.TargetString ] );
					if( producing != null && producing.Item == order.TargetString )
						producing.Paused = ( order.TargetLocation.X != 0 );
					break;
				}
			case "CancelProduction":
				{
					var producing = order.Player.Producing( Rules.UnitCategory[ order.TargetString ] );
					if( producing != null && producing.Item == order.TargetString )
						order.Player.CancelProduction( Rules.UnitCategory[ order.TargetString ] );
					break;
				}
			}
		}
	}
}
