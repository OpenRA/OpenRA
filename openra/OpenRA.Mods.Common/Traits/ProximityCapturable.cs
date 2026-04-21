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

using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Actor can be captured by units within a certain range.")]
	public class ProximityCapturableInfo : ProximityCapturableBaseInfo
	{
		[Desc("Maximum range at which a " + nameof(ProximityCaptor) + " actor can initiate the capture.")]
		public readonly WDist Range = WDist.FromCells(5);

		public override object Create(ActorInitializer init) { return new ProximityCapturable(init, this); }
	}

	public class ProximityCapturable : ProximityCapturableBase
	{
		public new readonly ProximityCapturableInfo Info;

		public ProximityCapturable(ActorInitializer init, ProximityCapturableInfo info)
			: base(init, info)
		{
			Info = info;
		}

		protected override int CreateTrigger(Actor self)
		{
			return self.World.ActorMap.AddProximityTrigger(self.CenterPosition, Info.Range, WDist.Zero, ActorEntered, ActorLeft);
		}

		protected override void RemoveTrigger(Actor self, int trigger)
		{
			self.World.ActorMap.RemoveProximityTrigger(trigger);
		}

		protected override void TickInner(Actor self)
		{
			self.World.ActorMap.UpdateProximityTrigger(trigger, self.CenterPosition, Info.Range, WDist.Zero);
		}

		protected override IRenderable GetRenderable(Actor self, WorldRenderer wr)
		{
			return new RangeCircleAnnotationRenderable(self.CenterPosition, Info.Range, 0, self.Owner.Color, 1, Color.Black, 3);
		}
	}
}
