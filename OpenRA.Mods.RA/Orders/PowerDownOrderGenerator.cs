#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.RA.Orders
{
	class PowerDownOrderGenerator : IOrderGenerator
	{
		public IEnumerable<Order> Order(World world, int2 xy, MouseInput mi)
		{
			if (mi.Button == MouseButton.Right)
				world.CancelInputMode();

			return OrderInner(world, xy, mi);
		}

		IEnumerable<Order> OrderInner(World world, int2 xy, MouseInput mi)
		{
			if (mi.Button == MouseButton.Left)
			{
				var underCursor = world.FindUnitsAtMouse(mi.Location)
					.Where(a => a.Owner == world.LocalPlayer
						&& a.traits.Contains<CanPowerDown>())
						.FirstOrDefault();

				if (underCursor != null)
					yield return new Order("PowerDown", underCursor);
			}
		}

		public void Tick(World world) { }
		public void RenderAfterWorld(World world) { }
		public void RenderBeforeWorld(World world) { }

		public string GetCursor(World world, int2 xy, MouseInput mi)
		{
			mi.Button = MouseButton.Left;
			return OrderInner(world, xy, mi).Any()
				? "powerdown" : "powerdown-blocked";
		}
	}
}
