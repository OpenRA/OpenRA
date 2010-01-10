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
			if (mi.Button == MouseButton.Right)
				Game.controller.CancelInputMode();

			return OrderInner(xy, mi);
		}

		IEnumerable<Order> OrderInner(int2 xy, MouseInput mi)
		{
			if (mi.Button == MouseButton.Left)
			{
				var loc = mi.Location + Game.viewport.Location;
				var underCursor = Game.FindUnits(loc, loc)
					.Where(a => a.Owner == Game.LocalPlayer
						&& a.traits.Contains<Building>()
						&& a.LegacyInfo.Selectable).FirstOrDefault();

				var building = underCursor != null ? underCursor.LegacyInfo as LegacyBuildingInfo : null;

				if (building != null && !building.Unsellable)
					yield return new Order("Sell", underCursor, null, int2.Zero, null);
			}
		}

		public void Tick() {}
		public void Render() {}

		public Cursor GetCursor(int2 xy, MouseInput mi)
		{
			mi.Button = MouseButton.Left;
			return OrderInner(xy, mi).Any()
				? Cursor.Sell : Cursor.SellBlocked;
		}
	}
}
