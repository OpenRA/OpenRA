using System.Collections.Generic;

namespace OpenRa.Game
{
	class LocalOrderSource : IOrderSource
	{
		Dictionary<int, List<Order>> orders = new Dictionary<int, List<Order>>();

		public List<Order> OrdersForFrame(int currentFrame)
		{
			// TODO: prune `orders` based on currentFrame.
			if (!orders.ContainsKey(currentFrame))
				return new List<Order>();
			return orders[currentFrame];
		}

		public void SendLocalOrders(int localFrame, List<Order> localOrders)
		{
			if (localFrame == 0) return;
			orders[localFrame] = localOrders;
		}

		public bool IsReadyForFrame(int frameNumber)
		{
			return true;
		}
	}
}
