#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;

namespace OpenRA.Mods.Common.Orders
{
	public class BeaconOrderGenerator : IOrderGenerator
	{
		public IEnumerable<Order> Order(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			world.CancelInputMode();

			if (mi.Button == MouseButton.Left)
				yield return new Order("PlaceBeacon", world.LocalPlayer.PlayerActor, false) { TargetLocation = cell, SuppressVisualFeedback = true };
		}

		public virtual void Tick(World world) { }
		public IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }
		public IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world) { yield break; }
		public string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			return "ability";
		}
	}
}
