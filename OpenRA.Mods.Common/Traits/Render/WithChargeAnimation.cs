#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Render trait that varies the animation frame based on the AttackCharges trait's charge level.")]
	class WithChargeAnimationInfo : ITraitInfo, Requires<WithSpriteBodyInfo>, Requires<AttackChargesInfo>
	{
		[SequenceReference]
		[Desc("Sequence to use for the charge levels.")]
		public readonly string Sequence = "active";

		public object Create(ActorInitializer init) { return new WithChargeAnimation(init.Self, this); }
	}

	class WithChargeAnimation : INotifyBuildComplete
	{
		readonly WithChargeAnimationInfo info;
		readonly WithSpriteBody wsb;
		readonly AttackCharges attackCharges;

		public WithChargeAnimation(Actor self, WithChargeAnimationInfo info)
		{
			this.info = info;
			wsb = self.Trait<WithSpriteBody>();
			attackCharges = self.Trait<AttackCharges>();
		}

		void INotifyBuildComplete.BuildingComplete(Actor self)
		{
			var attackChargesInfo = (AttackChargesInfo)attackCharges.Info;
			wsb.DefaultAnimation.PlayFetchIndex(wsb.NormalizeSequence(self, info.Sequence),
				() => int2.Lerp(0, wsb.DefaultAnimation.CurrentSequence.Length, attackCharges.ChargeLevel, attackChargesInfo.ChargeLevel + 1));
		}
	}
}
