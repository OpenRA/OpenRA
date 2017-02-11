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

using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Render overlay that varies the animation frame based on the AttackCharges trait's charge level.")]
	class WithChargeOverlayInfo : ITraitInfo, Requires<WithSpriteBodyInfo>, Requires<RenderSpritesInfo>
	{
		[SequenceReference]
		[Desc("Sequence to use for the charge levels.")]
		public readonly string Sequence = "active";

		[Desc("Custom palette name")]
		[PaletteReference("IsPlayerPalette")] public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		public object Create(ActorInitializer init) { return new WithChargeOverlay(init.Self, this); }
	}

	class WithChargeOverlay : INotifyBuildComplete, INotifySold, INotifyDamageStateChanged
	{
		readonly WithChargeOverlayInfo info;
		readonly Animation overlay;
		readonly RenderSprites rs;
		readonly WithSpriteBody wsb;

		bool buildComplete;

		public WithChargeOverlay(Actor self, WithChargeOverlayInfo info)
		{
			this.info = info;
			rs = self.Trait<RenderSprites>();
			wsb = self.Trait<WithSpriteBody>();

			var attackCharges = self.Trait<AttackCharges>();
			var attackChargesInfo = (AttackChargesInfo)attackCharges.Info;

			overlay = new Animation(self.World, rs.GetImage(self));
			overlay.PlayFetchIndex(wsb.NormalizeSequence(self, info.Sequence),
				() => int2.Lerp(0, overlay.CurrentSequence.Length, attackCharges.ChargeLevel, attackChargesInfo.ChargeLevel + 1));

			rs.Add(new AnimationWithOffset(overlay, null, () => !buildComplete, 1024),
				info.Palette, info.IsPlayerPalette);
		}

		void INotifyBuildComplete.BuildingComplete(Actor self)
		{
			buildComplete = true;
		}

		void INotifyDamageStateChanged.DamageStateChanged(Actor self, AttackInfo e)
		{
			overlay.ReplaceAnim(RenderSprites.NormalizeSequence(overlay, e.DamageState, info.Sequence));
		}

		void INotifySold.Selling(Actor self) { buildComplete = false; }
		void INotifySold.Sold(Actor self) { }
	}
}
