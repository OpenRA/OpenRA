#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.Graphics;
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

		public IEnumerable<Order> Order(World world, CPos xy, MouseInput mi)
		{
			if (mi.Button == MouseButton.Left)
			{
				world.CancelInputMode();
				yield break;
			}

			var queued =  mi.Modifiers.HasModifier(Modifiers.Shift);
			if (world.LocalPlayer.Shroud.IsExplored(xy))
				yield return new Order("ChronoshiftSelf", self, queued) { TargetLocation = xy };
		}

		public void Tick( World world ) { }
		public IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }
		public void RenderAfterWorld( WorldRenderer wr, World world )
		{
			wr.DrawSelectionBox(self, Color.White);
		}

		public string GetCursor(World world, CPos xy, MouseInput mi)
		{
			if (!world.LocalPlayer.Shroud.IsExplored(xy))
				return "move-blocked";

			var movement = self.TraitOrDefault<IPositionable>();
			return (movement.CanEnterCell(xy)) ? "chrono-target" : "move-blocked";
		}
	}
}
