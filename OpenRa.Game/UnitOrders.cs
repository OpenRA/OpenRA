using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.Game.GameRules;
using OpenRa.Game.Traits;
using IjwFramework.Types;

namespace OpenRa.Game
{
	static class UnitOrders
	{
		public static void ProcessOrder( Order order )
		{
			switch( order.OrderString )
			{
			case "Move":
			case "Attack":
			case "DeployMcv":
			case "DeliverOre":
			case "Harvest":
			case "SetRallyPoint":
				{
					foreach( var t in order.Subject.traits.WithInterface<IOrder>() )
						t.ResolveOrder( order.Subject, order );
					break;
				}
			case "PlaceBuilding":
				{
					Game.world.AddFrameEndTask( _ =>
					{
						var building = (UnitInfo.BuildingInfo)Rules.UnitInfo[ order.TargetString ];
						var producing = order.Player.Producing(Rules.UnitCategory[order.TargetString]);
						if( producing == null || producing.Item != order.TargetString || producing.RemainingTime != 0 )
							return;

						Log.Write( "Player \"{0}\" builds {1}", order.Player.PlayerName, building.Name );

						Game.world.Add( new Actor( building.Name, order.TargetLocation - GameRules.Footprint.AdjustForBuildingSize( building ), order.Player ) );

						order.Player.FinishProduction(Rules.UnitCategory[building.Name]);
					} );
					break;
				}
			case "StartProduction":
				{
					string group = Rules.UnitCategory[ order.TargetString ];
					var ui = Rules.UnitInfo[ order.TargetString ];
					var time = ui.Cost
						* .8f /* Game.BuildSpeed */						/* todo: country-specific build speed bonus */
						* ( 25 * 60 ) /* frames per min */				/* todo: build acceleration, if we do that */
						/ 1000;

					time = .08f * time;						/* temporary hax so we can build stuff fast for test */

					if (!Rules.TechTree.BuildableItems(order.Player, group).Contains(order.TargetString))
						return;	/* you can't build that!! */

					bool hasPlayedSound = false;

					order.Player.BeginProduction(group,
						new ProductionItem(order.TargetString, (int)time, ui.Cost,
							() => Game.world.AddFrameEndTask(
								_ =>
								{
									var isBuilding = group == "Building" || group == "Defense";
									if (!hasPlayedSound && order.Player == Game.LocalPlayer)
									{
										Game.PlaySound(isBuilding ? "conscmp1.aud" : "unitrdy1.aud", false);
										hasPlayedSound = true;
									}
									if (!isBuilding)
										Game.BuildUnit(order.Player, order.TargetString);
								})));
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
			case "Chat":
				{
					Game.chat.AddLine(Pair.New(order.Player.PlayerName + ":", order.TargetString));
					break;
				}
			default:
				throw new NotImplementedException();
			}
		}
	}
}
