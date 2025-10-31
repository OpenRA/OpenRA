#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;

namespace OpenRA.Orders
{
	public interface IOrderGenerator
	{
		IEnumerable<Order> Order(World world, CPos cell, int2 worldPixel, MouseInput mi);
		void Tick(World world);
		IEnumerable<IRenderable> Render(WorldRenderer wr, World world);
		IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world);
		IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World world);
		string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi);
		void Deactivate();
		bool HandleKeyPress(KeyInput e);
		void SelectionChanged(World world, IEnumerable<Actor> selected);
	}
}
