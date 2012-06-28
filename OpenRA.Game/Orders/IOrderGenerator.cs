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
using OpenRA.Graphics;

namespace OpenRA
{
	public interface IOrderGenerator
	{
		IEnumerable<Order> Order(World world, CPos xy, MouseInput mi);
		void Tick(World world);
		void RenderBeforeWorld(WorldRenderer wr, World world);
		void RenderAfterWorld(WorldRenderer wr, World world);
		string GetCursor(World world, CPos xy, MouseInput mi);
	}
}
