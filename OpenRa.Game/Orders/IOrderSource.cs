using System.Collections.Generic;

namespace OpenRa.Game.Orders
{
	interface IOrderSource
	{
		void SendLocalOrders(int localFrame, List<Order> localOrders);
		List<Order> OrdersForFrame(int currentFrame);
		bool IsReadyForFrame(int frameNumber);
	}
}
