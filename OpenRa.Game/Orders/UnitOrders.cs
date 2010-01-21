using System.Drawing;
using OpenRa.FileFormats;
using OpenRa.GameRules;
using OpenRa.Graphics;
using OpenRa.Traits;

namespace OpenRa.Orders
{
	static class UnitOrders
	{
		public static void ProcessOrder( Order order )
		{
			switch( order.OrderString )
			{
			case "Chat":
				{
					Game.chat.AddLine(order.Player, order.TargetString);
					break;
				}
			case "AssignPlayer":
				{
					order.Player.World.LocalPlayer = order.Player;
					Game.chat.AddLine(order.Player, "is now YOU.");
					break;
				}
			case "StartGame":
				{
					Game.chat.AddLine(Color.White, "Server", "The game has started.");
					Game.StartGame();
					break;
				}
			case "SyncInfo":
				{
					Game.SyncLobbyInfo(order.TargetString);
					break;
				}
			case "FileChunk":
				{
					PackageDownloader.ReceiveChunk(order.TargetString);
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
