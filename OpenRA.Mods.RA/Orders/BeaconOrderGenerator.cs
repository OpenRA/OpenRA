#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;

namespace OpenRA.Mods.RA.Orders
{
	public class BeaconOrderGenerator : IOrderGenerator
	{
		public IEnumerable<Order> Order(World world, CPos xy, MouseInput mi)
		{
			if (mi.Button != MouseButton.Left)
				world.CancelInputMode();

			if (world.Map.IsInMap(xy))
			{
				world.CancelInputMode();
				yield return new Order("PlaceBeacon", world.LocalPlayer.PlayerActor, false) { TargetLocation = xy };
			}
		}

		public virtual void Tick(World world) { }
		public IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }
		public void RenderAfterWorld(WorldRenderer wr, World world) { }
		public string GetCursor(World world, CPos xy, MouseInput mi) { return world.Map.IsInMap(xy) ? "ability" : "generic-blocked"; }
	}
}
