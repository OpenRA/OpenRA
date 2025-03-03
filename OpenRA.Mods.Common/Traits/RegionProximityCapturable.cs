#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Actor can be captured by units entering a certain set of cells.")]
	public class RegionProximityCapturableInfo : ProximityCapturableBaseInfo
	{
		[Desc("Set of cell offsets (relative to the actor's Location) the " + nameof(ProximityCaptor) + " needs to be in to initiate the capture. ",
			"A 'Region' ActorInit can be used to override this value per actor. If either is empty or non-existent, ",
			"the immediately neighboring cells of the actor will be used.")]
		public readonly CVec[] Region = [];

		public override object Create(ActorInitializer init) { return new RegionProximityCapturable(init, this); }
	}

	public class RegionProximityCapturable : ProximityCapturableBase
	{
		readonly CVec[] offsets;
		CPos[] region;

		public RegionProximityCapturable(ActorInitializer init, RegionProximityCapturableInfo info)
			: base(init, info)
		{
			offsets = init.GetValue<RegionInit, CVec[]>(info, info.Region);
		}

		protected override int CreateTrigger(Actor self)
		{
			region = offsets.Select(o => o + self.Location).ToArray();

			if (region.Length == 0)
				region = Util.ExpandFootprint([self.Location], true).ToArray();

			return self.World.ActorMap.AddCellTrigger(region, ActorEntered, ActorLeft);
		}

		protected override void RemoveTrigger(Actor self, int trigger)
		{
			self.World.ActorMap.RemoveCellTrigger(trigger);
		}

		protected override void TickInner(Actor self) { }

		protected override IRenderable GetRenderable(Actor self, WorldRenderer wr)
		{
			return new BorderedRegionRenderable(region, self.Owner.Color, 1, Color.Black, 3);
		}
	}

	public class RegionInit : ValueActorInit<CVec[]>
	{
		public RegionInit(CVec[] value)
			: base(value) { }
	}
}
