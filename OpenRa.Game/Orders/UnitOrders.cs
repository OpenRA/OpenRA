using System.Drawing;
using OpenRa.FileFormats;
using OpenRa.Game.GameRules;
using OpenRa.Game.Graphics;
using OpenRa.Game.Traits;

namespace OpenRa.Game.Orders
{
	static class UnitOrders
	{
		public static void ProcessOrder( Order order )
		{
			switch( order.OrderString )
			{
			case "PlaceBuilding":
				{
					Game.world.AddFrameEndTask( _ =>
					{
						var queue = order.Player.PlayerActor.traits.Get<ProductionQueue>();
						var unit = Rules.NewUnitInfo[ order.TargetString ];
						var producing = queue.CurrentItem(unit.Category);
						if( producing == null || producing.Item != order.TargetString || producing.RemainingTime != 0 )
							return;

						Game.world.Add( new Actor( order.TargetString, order.TargetLocation - Footprint.AdjustForBuildingSize( unit.Traits.Get<BuildingInfo>() ), order.Player ) );
						if (order.Player == Game.LocalPlayer)
						{
							Sound.Play("placbldg.aud");
							Sound.Play("build5.aud");
						}

						queue.FinishProduction(unit.Category);
					} );
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
			case "StartGame":
				{
					Game.chat.AddLine(Color.White, "Server", "The game has started.");
					Game.orderManager.StartGame();
					break;
				}
			case "ChangeMap":
				{
					Game.chat.AddLine(Color.White, "Server", "Changing map to {0}".F(order.TargetString));
					Game.ChangeMap(order.TargetString);
					break;
				}
			case "SyncInfo":
				{
	//				Game.chat.AddLine(Color.White, "Server", "Synchronizing lobby info...");
					Game.SyncLobbyInfo(order.TargetString);
					break;
				}

			default:
				{
					foreach (var t in order.Subject.traits.WithInterface<IResolveOrder>())
						t.ResolveOrder(order.Subject, order);
					break;
				}
			}
		}
	}
}
