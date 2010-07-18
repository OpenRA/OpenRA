#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

namespace OpenRA.Mods.RA.Render
{
	class RenderBuildingChargeInfo : RenderBuildingInfo
	{
		public readonly string ChargeAudio = "tslachg2.aud";
		public override object Create(ActorInitializer init) { return new RenderBuildingCharge(init.self); }
	}

	/* used for tesla */
	public class RenderBuildingCharge : RenderBuilding
	{
		public RenderBuildingCharge(Actor self)
			: base(self)
		{
		}

		public void PlayCharge(Actor self)
		{
			Sound.Play(self.Info.Traits.Get<RenderBuildingChargeInfo>().ChargeAudio, self.CenterLocation);
			anim.PlayThen(GetPrefix(self) + "active", 
				() => anim.PlayRepeating(GetPrefix(self) + "idle"));
		}
	}
}
