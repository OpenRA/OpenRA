#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;

namespace OpenRA.Traits
{
	class RallyPointInfo : ITraitInfo, ITraitPrerequisite<RenderSimpleInfo>
	{
		public readonly int[] RallyPoint = { 1, 3 };

		public object Create(Actor self) { return new RallyPoint(self); }
	}

	public class RallyPoint : IRender, IIssueOrder, IResolveOrder, ITick
	{
		[Sync]
		public int2 rallyPoint;
		public Animation anim;

		public RallyPoint(Actor self)
		{
			var info = self.Info.Traits.Get<RallyPointInfo>();
			rallyPoint = self.Location + new int2(info.RallyPoint[0], info.RallyPoint[1]);
			anim = new Animation("flagfly");
			anim.PlayRepeating("idle");
		}

		public IEnumerable<Renderable> Render(Actor self)
		{
			if (self.Owner == self.World.LocalPlayer && Game.controller.selection.Actors.Contains(self))
				yield return Util.Centered(self,
					anim.Image, Util.CenterOfCell(rallyPoint));
		}

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button == MouseButton.Left || underCursor != null) return null;
			return new Order("SetRallyPoint", self, xy);
		}

		public void ResolveOrder( Actor self, Order order )
		{
			if( order.OrderString == "SetRallyPoint" )
				rallyPoint = order.TargetLocation;
		}

		public void Tick(Actor self) { anim.Tick(); }
	}
}
