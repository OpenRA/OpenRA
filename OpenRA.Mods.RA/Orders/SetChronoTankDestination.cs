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
using System.Drawing;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Orders
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
				world.CancelInputMode();
				yield break;
			}

			if (world.LocalPlayer.Shroud.IsExplored(xy))
				yield return new Order("ChronoshiftSelf", self, xy);
		}

		public void Tick( World world ) { }
		public void RenderAfterWorld( World world )
		{
			world.WorldRenderer.DrawSelectionBox(self, Color.White);
		}

		public void RenderBeforeWorld(World world) { }

		public string GetCursor(World world, int2 xy, MouseInput mi)
		{
			if (!world.LocalPlayer.Shroud.IsExplored(xy))
				return "move-blocked";

			var movement = self.TraitOrDefault<IMove>();
			return (movement.CanEnterCell(xy)) ? "chrono-target" : "move-blocked";
		}
	}
}
