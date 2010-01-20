using System.Collections.Generic;
using System.Linq;

namespace OpenRa.Orders
{
	class LocalOrderSource : IOrderSource
	{
		Dictionary<int, List<byte[]>> orders = new Dictionary<int, List<byte[]>>();

		public List<byte[]> OrdersForFrame(int currentFrame)
		{
			if (!orders.ContainsKey(currentFrame))
				return new List<byte[]>();

			var result = orders[currentFrame];
			orders.Remove(currentFrame);
			return result;
		}

		public void SendLocalOrders(int localFrame, List<Order> localOrders)
		{
			if (localFrame == 0) return;
			orders[localFrame] = localOrders.Select(o=>o.Serialize()).ToList();
		}

		public bool IsReadyForFrame(int frameNumber)
		{
			return true;
		}
	}
}
