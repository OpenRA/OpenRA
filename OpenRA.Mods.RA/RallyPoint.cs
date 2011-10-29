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
using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class RallyPointInfo : ITraitInfo, Requires<RenderSimpleInfo>
	{
		public readonly int[] RallyPoint = { 1, 3 };

		public object Create(ActorInitializer init) { return new RallyPoint(init.self); }
	}

	public class RallyPoint : IIssueOrder, IResolveOrder, ISync
	{
		[Sync] public int2 rallyPoint;

		public RallyPoint(Actor self)
		{
			var info = self.Info.Traits.Get<RallyPointInfo>();
			rallyPoint = self.Location + new int2(info.RallyPoint[0], info.RallyPoint[1]);
			self.World.AddFrameEndTask(w => w.Add(new Effects.RallyPoint(self)));
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new RallyPointOrderTargeter(); }
		}

		public Order IssueOrder( Actor self, IOrderTargeter order, Target target, bool queued )
		{
			if( order.OrderID == "SetRallyPoint" )
				return new Order(order.OrderID, self, false) { TargetLocation = Traits.Util.CellContaining(target.CenterLocation) };

			return null;
		}

		public void ResolveOrder( Actor self, Order order )
		{
			if( order.OrderString == "SetRallyPoint" )
				rallyPoint = order.TargetLocation;
		}

		class RallyPointOrderTargeter : IOrderTargeter
		{
			public string OrderID { get { return "SetRallyPoint"; } }
			public int OrderPriority { get { return 0; } }

			public bool CanTargetActor(Actor self, Actor target, bool forceAttack, bool forceQueued, ref string cursor)
			{
				return false;
			}

			public bool CanTargetLocation(Actor self, int2 location, List<Actor> actorsAtLocation, bool forceAttack, bool forceQueued, ref string cursor)
			{
				if (self.World.Map.IsInMap(location))
				{
					cursor = "ability";
					return true;
				}
				return false;
			}

			public bool IsQueued { get { return false; } } // unused
		}
	}
}
