using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.GameRules;
using OpenRa.Traits;

namespace OpenRa.Orders
{
	class RepairOrderGenerator : IOrderGenerator
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
				var underCursor = Game.world.FindUnits(loc, loc)
					.Where(a => a.Owner == Game.LocalPlayer
						&& a.traits.Contains<Building>()
						&& a.traits.Contains<Selectable>()).FirstOrDefault();

				var building = underCursor != null ? underCursor.Info.Traits.Get<BuildingInfo>() : null;

				if (building != null && building.Repairable && underCursor.Health < building.HP)
					yield return new Order("Repair", underCursor, null, int2.Zero, null);
			}
		}

		public void Tick()
		{
			if (!Game.Settings.RepairRequiresConyard)
				return;

			var hasFact = Game.world.Actors
				.Any(a => a.Owner == Game.LocalPlayer && a.traits.Contains<ConstructionYard>());
				
			if (!hasFact)
				Game.controller.CancelInputMode();
		}

		public void Render() {}

		public Cursor GetCursor(int2 xy, MouseInput mi)
		{
			mi.Button = MouseButton.Left;
			return OrderInner(xy, mi).Any() 
				? Cursor.Repair : Cursor.RepairBlocked;
		}
	}
}
