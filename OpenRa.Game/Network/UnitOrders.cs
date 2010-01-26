using System.Drawing;
using System.Linq;
using OpenRa.FileFormats;
using OpenRa.GameRules;
using OpenRa.Graphics;
using OpenRa.Traits;

namespace OpenRa.Network
{
	static class UnitOrders
	{
		public static void ProcessOrder( World world, int clientId, Order order )
		{
			switch( order.OrderString )
			{
			case "Chat":
				{
					var player = world.players.Values.Where( p => p.Index == clientId ).Single();
					Game.chat.AddLine(player, order.TargetString);
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
