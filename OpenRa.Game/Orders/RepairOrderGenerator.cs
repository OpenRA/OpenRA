#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Orders
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

			var hasFact = world.Queries.OwnedBy[world.LocalPlayer]
				.WithTrait<ConstructionYard>()
				.Any();
				
			if (!hasFact)
				Game.controller.CancelInputMode();
		}

		public void Render( World world ) {}

		public string GetCursor(World world, int2 xy, MouseInput mi)
		{
			mi.Button = MouseButton.Left;
			return OrderInner(world, xy, mi).Any() 
				? "repair" : "repair-blocked";
		}
	}
}
