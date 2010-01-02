using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.GameRules;
using OpenRa.Game.Traits;

namespace OpenRa.Game.Orders
{
	class RepairOrderGenerator : IOrderGenerator
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

			if (building == null || !building.Repairable || underCursor.Health == building.Strength)
			{
				yield return new Order("NoRepair", Game.LocalPlayer.PlayerActor, null, int2.Zero, null);
				yield break;
			}

			yield return new Order("Repair", underCursor, null, int2.Zero, null);
		}

		public void Tick() {}

		public void Render() {}
	}
}
