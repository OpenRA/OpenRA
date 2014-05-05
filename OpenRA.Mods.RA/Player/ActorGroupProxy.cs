#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using System.Globalization;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class ActorGroupProxyInfo : TraitInfo<ActorGroupProxy> { }

	class ActorGroupProxy : IResolveOrder
	{
		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "CreateGroup")
			{
				/* create a group */
				var actors = order.TargetString.Split(',')
					.Select(id => uint.Parse(id, NumberStyles.Any, NumberFormatInfo.InvariantInfo))
					.Select(id => self.World.Actors.FirstOrDefault(a => a.ActorID == id))
						.Where(a => a != null);

				new Group(actors);
			}
		}
	}
}
