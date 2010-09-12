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
using System.Linq;
using OpenRA.Graphics;

namespace OpenRA.Traits
{
	class RallyPointInfo : ITraitInfo, ITraitPrerequisite<RenderSimpleInfo>
	{
		public readonly int[] RallyPoint = { 1, 3 };

		public object Create(ActorInitializer init) { return new RallyPoint(init.self); }
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
			if (self.Owner == self.World.LocalPlayer && self.World.Selection.Actors.Contains(self))
				yield return Util.Centered(self,
					anim.Image, Util.CenterOfCell(rallyPoint));
		}

		public int OrderPriority(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			return 0;
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
