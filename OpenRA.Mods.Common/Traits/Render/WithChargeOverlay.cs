#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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
	[Desc("Rendered together with AttackCharge.")]
	public class WithChargeOverlayInfo : ITraitInfo, Requires<RenderSpritesInfo>
	{
		[Desc("Sequence name to use")]
		[SequenceReference] public readonly string Sequence = "active";

		[Desc("Custom palette name")]
		[PaletteReference("IsPlayerPalette")] public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		public object Create(ActorInitializer init) { return new WithChargeOverlay(init, this); }
	}

	public class WithChargeOverlay : INotifyCharging, INotifyDamageStateChanged, INotifySold
	{
		readonly Animation overlay;
		readonly RenderSprites renderSprites;
		readonly WithChargeOverlayInfo info;

		bool charging;

		public WithChargeOverlay(ActorInitializer init, WithChargeOverlayInfo info)
		{
			this.info = info;

			renderSprites = init.Self.Trait<RenderSprites>();

			overlay = new Animation(init.World, renderSprites.GetImage(init.Self));

			renderSprites.Add(new AnimationWithOffset(overlay, null, () => !charging),
				info.Palette, info.IsPlayerPalette);
		}

		public void Charging(Actor self, Target target)
		{
			charging = true;
			overlay.PlayThen(RenderSprites.NormalizeSequence(overlay, self.GetDamageState(), info.Sequence), () => charging = false);
		}

		public void DamageStateChanged(Actor self, AttackInfo e)
		{
			overlay.ReplaceAnim(RenderSprites.NormalizeSequence(overlay, e.DamageState, info.Sequence));
		}

		public void Sold(Actor self) { }
		public void Selling(Actor self)
		{
			charging = false;
		}
	}
}