#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class ConstructionYardInfo : TraitInfo<ConstructionYard> { }

	public class ConstructionYard : IIssueOrder, IResolveOrder, IProvideCursor
	{
		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button == MouseButton.Left) return null;

			if (underCursor == self)
				return new Order("Deploy", self);

			return null;
		}

		public string CursorForOrderString(string s, Actor a, int2 location)
		{
			return (s == "Deploy") ? "deploy" : null;
		}
		
		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Deploy")
			{
				self.CancelActivity();
				self.QueueActivity(new UndeployMcv());
			}
		}
	}
}
