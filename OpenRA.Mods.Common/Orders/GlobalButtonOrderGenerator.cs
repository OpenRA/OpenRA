#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Orders
{
	public abstract class GlobalButtonOrderGenerator : IOrderGenerator
	{
		public virtual IEnumerable<Order> Order(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if ((mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Down) || (mi.Button == MouseButton.Right && mi.Event == MouseInputEvent.Up))
				return OrderInner(world, cell, worldPixel, mi);

			return Enumerable.Empty<Order>();
		}

		void IOrderGenerator.Tick(World world) { Tick(world); }
		IEnumerable<IRenderable> IOrderGenerator.Render(WorldRenderer wr, World world) { return Render(wr, world); }
		IEnumerable<IRenderable> IOrderGenerator.RenderAboveShroud(WorldRenderer wr, World world) { return RenderAboveShroud(wr, world); }
		IEnumerable<IRenderable> IOrderGenerator.RenderAnnotations(WorldRenderer wr, World world) { return RenderAnnotations(wr, world); }
		string IOrderGenerator.GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi) { return GetCursor(world, cell, worldPixel, mi); }
		void IOrderGenerator.Deactivate() { }
		bool IOrderGenerator.HandleKeyPress(KeyInput e) { return false; }

		protected virtual void Tick(World world)
		{
			if (world.LocalPlayer != null &&
				world.LocalPlayer.WinState != WinState.Undefined)
				world.CancelInputMode();
		}

		protected virtual IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }
		protected virtual IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world) { yield break; }
		protected virtual IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World world) { yield break; }
		protected abstract string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi);
		protected abstract IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi);
	}
}
