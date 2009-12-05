using System;
using System.Drawing;
using System.Linq;
using OpenRa.Game.GameRules;
using OpenRa.Game.Traits;

namespace OpenRa.Game
{
	static class UnitOrders
	{
		public static void ProcessOrder( Order order )
		{
			if (!order.Validate())
			{
				/* todo: log this if we care */
				return;
			}

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
						var building = (BuildingInfo)Rules.UnitInfo[ order.TargetString ];
						var producing = order.Player.Producing(Rules.UnitCategory[order.TargetString]);
						if( producing == null || producing.Item != order.TargetString || producing.RemainingTime != 0 )
							return;

						Log.Write( "Player \"{0}\" builds {1}", order.Player.PlayerName, building.Name );

						Game.world.Add( new Actor( building, order.TargetLocation - GameRules.Footprint.AdjustForBuildingSize( building ), order.Player ) );
						if (order.Player == Game.LocalPlayer)
						{
							Sound.Play("placbldg.aud");
							Sound.Play("build5.aud");
						}

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
										Sound.Play(isBuilding ? "conscmp1.aud" : "unitrdy1.aud");
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
					Game.chat.AddLine(order.Player, order.TargetString);
					break;
				}
			case "ToggleReady":
				{
					Game.chat.AddLine(order.Player, "is " + order.TargetString );
					break;
				}
			case "AssignPlayer":
				{
					Game.LocalPlayer = order.Player;
					Game.chat.AddLine(order.Player, "is now YOU.");
					break;
				}
			case "SetName":
				{
					Game.chat.AddLine(order.Player, "is now known as " + order.TargetString);
					order.Player.PlayerName = order.TargetString;
					break;
				}
			case "SetRace":
				{
					order.Player.Race = order.TargetString == "0" ? Race.Soviet : Race.Allies;
					Game.chat.AddLine(order.Player, "is now playing {0}".F(order.Player.Race));
					break;
				}
			case "SetLag":
				{
					int lag = int.Parse(order.TargetString);
					if (Game.orderManager.GameStarted)
					{
						Game.chat.AddLine(Color.White, "Server", "Failed to change lag to {0} frames".F(lag));
						return;
					}

					Game.orderManager.FramesAhead = lag;
					Game.chat.AddLine(Color.White, "Server", "Order lag is now {0} frames.".F(lag));
					break;
				}
			case "SetPalette":
				{
					int palette = int.Parse(order.TargetString);
					Game.chat.AddLine(order.Player, "has changed color to {0}".F(palette));
					order.Player.Palette = palette;
					break;
				}
			case "StartGame":
				{
					Game.chat.AddLine(Color.White, "Server", "The game has started.");
					Game.orderManager.StartGame();
					break;
				}

			default:
				throw new NotImplementedException();
			}
		}
	}
}
