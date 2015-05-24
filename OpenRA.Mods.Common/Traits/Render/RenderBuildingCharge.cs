#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Used for tesla coil and obelisk.")]
	public class RenderBuildingChargeInfo : RenderBuildingInfo
	{
		[Desc("Sequence to use for building charge animation.")]
		[SequenceReference] public readonly string ChargeSequence = "active";

		public override object Create(ActorInitializer init) { return new RenderBuildingCharge(init, this); }
	}

	public class RenderBuildingCharge : RenderBuilding, INotifyCharging
	{
		RenderBuildingChargeInfo info;

		public RenderBuildingCharge(ActorInitializer init, RenderBuildingChargeInfo info)
			: base(init, info)
		{
			this.info = info;
		}

		public void Charging(Actor self, Target target)
		{
			PlayCustomAnim(self, info.ChargeSequence);
		}
	}
}
