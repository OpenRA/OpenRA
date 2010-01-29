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
		public IEnumerable<Order> Order(World world, int2 xy, MouseInput mi)
		{
			if (mi.Button == MouseButton.Right)
				Game.controller.CancelInputMode();

			return OrderInner(world, xy, mi);
		}

		IEnumerable<Order> OrderInner(World world, int2 xy, MouseInput mi)
		{
			if (mi.Button == MouseButton.Left)
			{
				var underCursor = world.FindUnitsAtMouse(mi.Location)
					.Where(a => a.Owner == world.LocalPlayer
						&& a.traits.Contains<Building>()
						&& a.traits.Contains<Selectable>()).FirstOrDefault();

				var building = underCursor != null ? underCursor.Info.Traits.Get<BuildingInfo>() : null;

				if (building != null && building.Repairable && underCursor.Health < building.HP)
					yield return new Order("Repair", underCursor);
			}
		}

		public void Tick( World world )
		{
			if (!Game.Settings.RepairRequiresConyard)
				return;

			var hasFact = world.Actors
				.Any(a => a.Owner == world.LocalPlayer && a.traits.Contains<ConstructionYard>());
				
			if (!hasFact)
				Game.controller.CancelInputMode();
		}

		public void Render( World world ) {}

		public Cursor GetCursor(World world, int2 xy, MouseInput mi)
		{
			mi.Button = MouseButton.Left;
			return OrderInner(world, xy, mi).Any() 
				? Cursor.Repair : Cursor.RepairBlocked;
		}
	}
}
