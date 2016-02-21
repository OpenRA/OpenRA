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

namespace OpenRA
{
	public interface IOrderGenerator
	{
		IEnumerable<Order> Order(World world, CPos cell, int2 worldPixel, MouseInput mi);
		void Tick(World world);
		IEnumerable<IRenderable> Render(WorldRenderer wr, World world);
		IEnumerable<IRenderable> RenderAfterWorld(WorldRenderer wr, World world);
		string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi);
	}
}
