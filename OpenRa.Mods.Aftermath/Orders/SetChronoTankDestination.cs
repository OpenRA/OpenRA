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
using System.Drawing;
using OpenRa.Traits;

namespace OpenRa.Mods.Aftermath.Orders
{
	class SetChronoTankDestination : IOrderGenerator
	{
		public readonly Actor self;

		public SetChronoTankDestination(Actor self)
		{
			this.self = self;
		}

		public IEnumerable<Order> Order(World world, int2 xy, MouseInput mi)
		{
			if (mi.Button == MouseButton.Left)
			{
				Game.controller.CancelInputMode();
				yield break;
			}

			yield return new Order("ChronoshiftSelf", self, xy);
		}

		public void Tick( World world ) { }
		public void Render( World world )
		{
			world.WorldRenderer.DrawSelectionBox(self, Color.White, true);
		}

		public string GetCursor(World world, int2 xy, MouseInput mi)
		{
			if (!world.LocalPlayer.Shroud.IsExplored(xy))
				return "move-blocked";

			var movement = self.traits.GetOrDefault<IMovement>();
			return (movement.CanEnterCell(xy)) ? "chrono-target" : "move-blocked";
		}
	}
}
