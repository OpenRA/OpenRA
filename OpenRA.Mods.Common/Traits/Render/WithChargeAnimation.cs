#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Render trait that varies the animation frame based on the AttackCharges trait's charge level.")]
	class WithChargeAnimationInfo : ConditionalTraitInfo, Requires<WithSpriteBodyInfo>, Requires<AttackChargesInfo>
	{
		[SequenceReference]
		[Desc("Sequence to use for the charge levels.")]
		public readonly string Sequence = "active";

		[Desc("Which sprite body to play the animation on.")]
		public readonly string Body = "body";

		public override object Create(ActorInitializer init) { return new WithChargeAnimation(init.Self, this); }
	}

	class WithChargeAnimation : ConditionalTrait<WithChargeAnimationInfo>
	{
		readonly WithSpriteBody wsb;
		readonly AttackCharges attackCharges;

		public WithChargeAnimation(Actor self, WithChargeAnimationInfo info)
			: base(info)
		{
			wsb = self.TraitsImplementing<WithSpriteBody>().Single(w => w.Info.Name == info.Body);
			attackCharges = self.Trait<AttackCharges>();
		}

		protected override void TraitEnabled(Actor self)
		{
			var attackChargesInfo = (AttackChargesInfo)attackCharges.Info;
			wsb.DefaultAnimation.PlayFetchIndex(wsb.NormalizeSequence(self, Info.Sequence),
				() => int2.Lerp(0, wsb.DefaultAnimation.CurrentSequence.Length, attackCharges.ChargeLevel, attackChargesInfo.ChargeLevel + 1));
		}
	}
}
