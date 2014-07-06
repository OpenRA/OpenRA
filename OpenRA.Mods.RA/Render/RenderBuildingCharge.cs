#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.Mods.RA.Render
{
	[Desc("Used for tesla coil and obelisk.")]
	public class RenderBuildingChargeInfo : RenderBuildingInfo
	{
		[Desc("Sound to play when building charges.")]
		public readonly string ChargeAudio = null;
		[Desc("Sequence to use for building charge animation.")]
		public readonly string ChargeSequence = "active";
		public override object Create(ActorInitializer init) { return new RenderBuildingCharge(init, this); }
	}

	public class RenderBuildingCharge : RenderBuilding
	{
		RenderBuildingChargeInfo info;

		public RenderBuildingCharge(ActorInitializer init, RenderBuildingChargeInfo info)
			: base(init, info)
		{
			this.info = info;
		}

		public void PlayCharge(Actor self)
		{
			Sound.Play(info.ChargeAudio, self.CenterPosition);
			PlayCustomAnim(self, info.ChargeSequence);
		}
	}
}
