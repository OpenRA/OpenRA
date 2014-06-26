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
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Used to waypoint units after production or repair is finished.")]
	public class RallyPointInfo : ITraitInfo
	{
		public readonly int[] RallyPoint = { 1, 3 };
		public readonly string IndicatorPalettePrefix = "player";

		public object Create(ActorInitializer init) { return new RallyPoint(init.self, this); }
	}

	public class RallyPoint : IIssueOrder, IResolveOrder, ISync
	{
		[Sync] public CPos rallyPoint;
		public int nearEnough = 1;

		public RallyPoint(Actor self, RallyPointInfo info)
		{
			rallyPoint = self.Location + new CVec(info.RallyPoint[0], info.RallyPoint[1]);
			self.World.AddFrameEndTask(w => w.Add(new Effects.RallyPoint(self, info.IndicatorPalettePrefix)));
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new RallyPointOrderTargeter(); }
		}

		public Order IssueOrder( Actor self, IOrderTargeter order, Target target, bool queued )
		{
			if (order.OrderID == "SetRallyPoint")
				return new Order(order.OrderID, self, false) { TargetLocation = target.CenterPosition.ToCPos(), SuppressVisualFeedback = true };

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

			public bool CanTarget(Actor self, Target target, List<Actor> othersAtTarget, TargetModifiers modifiers, ref string cursor)
			{
				if (target.Type != TargetType.Terrain)
					return false;

				var location = target.CenterPosition.ToCPos();
				if (self.World.Map.Contains(location))
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
