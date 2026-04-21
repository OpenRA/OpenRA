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

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Render trait that varies the sprite body frame based on the AttackCharges trait's charge level.")]
	public class WithChargeSpriteBodyInfo : WithSpriteBodyInfo, Requires<AttackChargesInfo>
	{
		public override object Create(ActorInitializer init) { return new WithChargeSpriteBody(init, this); }

		public override IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, string image, int facings, PaletteReference p)
		{
			if (!EnabledByDefault)
				yield break;

			var anim = new Animation(init.World, image);
			anim.PlayFetchIndex(RenderSprites.NormalizeSequence(anim, init.GetDamageState(), Sequence), () => 0);

			yield return new SpriteActorPreview(anim, () => WVec.Zero, () => 0, p);
		}
	}

	public class WithChargeSpriteBody : WithSpriteBody
	{
		readonly AttackCharges attackCharges;

		public WithChargeSpriteBody(ActorInitializer init, WithChargeSpriteBodyInfo info)
			: base(init, info)
		{
			attackCharges = init.Self.Trait<AttackCharges>();
			ConfigureAnimation(init.Self);
		}

		void ConfigureAnimation(Actor self)
		{
			var attackChargesInfo = (AttackChargesInfo)attackCharges.Info;
			DefaultAnimation.PlayFetchIndex(NormalizeSequence(self, Info.Sequence),
				() => int2.Lerp(0, DefaultAnimation.CurrentSequence.Length, attackCharges.ChargeLevel, attackChargesInfo.ChargeLevel + 1));
		}

		protected override void TraitEnabled(Actor self)
		{
			// Do nothing - we just want to disable the default WithSpriteBody implementation
		}

		public override void CancelCustomAnimation(Actor self)
		{
			ConfigureAnimation(self);
		}
	}
}
