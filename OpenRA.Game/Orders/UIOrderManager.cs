using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA
{
	public class UIOrderManager
	{
		internal static readonly UIOrder[] NoOrders = new UIOrder[0];

		readonly Dictionary<uint, List<UIOrder>> orders = new Dictionary<uint, List<UIOrder>>();

		internal IEnumerable<UIOrder> GetUIOrders(Actor a)
		{
			var actorOrders = orders.FirstOrDefault((p) => p.Key == a.ActorID).Value;
			if (actorOrders == null)
				return NoOrders;

			actorOrders.RemoveAll(o => o.Resolved);
			return actorOrders;
		}

		public void IssueUIOrder(Actor a, string order)
		{
			Sync.AssertUnsynced("UI orders may not be issued from synced code");
			var orderList = orders.GetOrAdd(a.ActorID);
			orderList.Add(new UIOrder(order));
		}

		public void CancelUIOrder(Actor a, string order)
		{
			Sync.AssertUnsynced("UI orders may not be canceled from synced code");
			var orderList = orders.FirstOrDefault(o => o.Key == a.ActorID);
			if (orderList.Value != null)
				orderList.Value.RemoveAll(o => o.Order == order);
		}

		public bool OrderExists(Actor a, string order)
		{
			var orderList = orders.FirstOrDefault(o => o.Key == a.ActorID);
			if (orderList.Value == null)
				return false;

			return orderList.Value.Any(o => o.Order == order);
		}

		internal void Clear()
		{
			orders.Clear();
		}
	}

	public class UIOrder
	{
		public string Order { get; set; }
		public bool Resolved { get; private set; }

		public UIOrder(string order)
		{
			Order = order;
			Resolved = false;
		}

		public void Resolve() { Resolved = true; }
	}
}
