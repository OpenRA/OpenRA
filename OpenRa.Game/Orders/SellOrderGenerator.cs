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
			if (!mi.IsFake && mi.Button == MouseButton.Right)
			{
				Game.controller.CancelInputMode();
				yield break;
			}

			var loc = mi.Location + Game.viewport.Location;
			var underCursor = Game.FindUnits(loc, loc)
				.Where(a => a.Owner == Game.LocalPlayer
					&& a.traits.Contains<Building>()
					&& a.Info.Selectable).FirstOrDefault();

			var building = underCursor != null ? underCursor.Info as BuildingInfo : null;

			if (building == null || building.Unsellable)
			{
				yield return new Order("NoSell", Game.LocalPlayer.PlayerActor, null, int2.Zero, null);
				yield break;
			}

			yield return new Order("Sell", underCursor, null, int2.Zero, null);
		}

		public void Tick() {}
		public void Render() {}
	}
}
