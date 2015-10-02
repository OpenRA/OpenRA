#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Used to waypoint units after production or repair is finished.")]
	public class RallyPointInfo : ITraitInfo
	{
		public readonly string Image = "rallypoint";
		[SequenceReference("Image")] public readonly string FlagSequence = "flag";
		[SequenceReference("Image")] public readonly string CirclesSequence = "circles";

		[Desc("Custom indicator palette name")]
		[PaletteReference("IsPlayerPalette")] public readonly string Palette = "player";

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = true;

		public readonly CVec Offset = new CVec(1, 3);

		public object Create(ActorInitializer init) { return new RallyPoint(init.Self, this); }
	}

	public class RallyPoint : IIssueOrder, IResolveOrder, ISync
	{
		[Sync] public CPos Location;
		public RallyPointInfo Info;

		public RallyPoint(Actor self, RallyPointInfo info)
		{
			Info = info;
			Location = self.Location + info.Offset;
			var palette = info.IsPlayerPalette ? info.Palette + self.Owner.InternalName : info.Palette;
			self.World.AddFrameEndTask(w => w.Add(new RallyPointIndicator(self, palette)));
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new RallyPointOrderTargeter(); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "SetRallyPoint")
				return new Order(order.OrderID, self, false) { TargetLocation = self.World.Map.CellContaining(target.CenterPosition), SuppressVisualFeedback = true };

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "SetRallyPoint")
				Location = order.TargetLocation;
		}

		class RallyPointOrderTargeter : IOrderTargeter
		{
			public string OrderID { get { return "SetRallyPoint"; } }
			public int OrderPriority { get { return 0; } }
			public bool OverrideSelection { get { return true; } }

			public bool CanTarget(Actor self, Target target, List<Actor> othersAtTarget, TargetModifiers modifiers, ref string cursor)
			{
				if (target.Type != TargetType.Terrain)
					return false;

				var location = self.World.Map.CellContaining(target.CenterPosition);
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
