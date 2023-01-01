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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Render overlay that varies the animation frame based on the AttackCharges trait's charge level.")]
	class WithChargeOverlayInfo : PausableConditionalTraitInfo, Requires<WithSpriteBodyInfo>, Requires<RenderSpritesInfo>
	{
		[SequenceReference]
		[Desc("Sequence to use for the charge levels.")]
		public readonly string Sequence = "active";

		[PaletteReference(nameof(IsPlayerPalette))]
		[Desc("Custom palette name")]
		public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		public override object Create(ActorInitializer init) { return new WithChargeOverlay(init.Self, this); }
	}

	class WithChargeOverlay : PausableConditionalTrait<WithChargeOverlayInfo>, INotifyDamageStateChanged
	{
		readonly Animation overlay;

		public WithChargeOverlay(Actor self, WithChargeOverlayInfo info)
			: base(info)
		{
			var rs = self.Trait<RenderSprites>();
			var wsb = self.Trait<WithSpriteBody>();

			var attackCharges = self.Trait<AttackCharges>();
			var attackChargesInfo = (AttackChargesInfo)attackCharges.Info;

			overlay = new Animation(self.World, rs.GetImage(self), () => IsTraitPaused);
			overlay.PlayFetchIndex(wsb.NormalizeSequence(self, info.Sequence),
				() => int2.Lerp(0, overlay.CurrentSequence.Length, attackCharges.ChargeLevel, attackChargesInfo.ChargeLevel + 1));

			rs.Add(new AnimationWithOffset(overlay, null, () => IsTraitDisabled, 1024),
				info.Palette, info.IsPlayerPalette);
		}

		void INotifyDamageStateChanged.DamageStateChanged(Actor self, AttackInfo e)
		{
			overlay.ReplaceAnim(RenderSprites.NormalizeSequence(overlay, e.DamageState, Info.Sequence));
		}
	}
}
