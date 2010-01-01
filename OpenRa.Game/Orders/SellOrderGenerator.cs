using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.GameRules;
using OpenRa.Game.Traits;

namespace OpenRa.Game.Orders
{
	class SellOrderGenerator : IOrderGenerator
	{
		public IEnumerable<Order> Order(int2 xy, MouseInput mi)
		{
			var loc = mi.Location + Game.viewport.Location;
			var underCursor = Game.FindUnits(loc, loc)
				.Where( a => a.traits.Contains<Building>() ).FirstOrDefault();

			if (underCursor != null && !underCursor.Info.Selectable)
				underCursor = null;

			if (underCursor == null)
				yield break;

			var building = underCursor.traits.Get<Building>();
			if (building.unitInfo.Unsellable)
				yield break;

			if (underCursor.Owner != Game.LocalPlayer)
				yield break;

			yield return new Order("Sell", underCursor, null, int2.Zero, null);
		}

		public void Tick() {}
		public void Render() {}
	}
}
