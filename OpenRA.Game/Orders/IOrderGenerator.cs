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

namespace OpenRA
{
	public interface IOrderGenerator
	{
		IEnumerable<Order> Order( World world, int2 xy, MouseInput mi );
		void Tick( World world );
		void Render( World world );
		string GetCursor( World world, int2 xy, MouseInput mi );
	}
}
