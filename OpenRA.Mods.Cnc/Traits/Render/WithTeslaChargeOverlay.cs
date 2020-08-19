#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits.Render
{
	[Desc("Rendered together with AttackCharge.")]
	public class WithTeslaChargeOverlayInfo : TraitInfo, Requires<RenderSpritesInfo>
	{
		[SequenceReference]
		[Desc("Sequence name to use")]
		public readonly string Sequence = "active";

		[PaletteReference(nameof(IsPlayerPalette))]
		[Desc("Custom palette name")]
		public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		public override object Create(ActorInitializer init) { return new WithTeslaChargeOverlay(init, this); }
	}

	public class WithTeslaChargeOverlay : INotifyTeslaCharging, INotifyDamageStateChanged, INotifySold
	{
		readonly Animation overlay;
		readonly RenderSprites renderSprites;
		readonly WithTeslaChargeOverlayInfo info;

		bool charging;

		public WithTeslaChargeOverlay(ActorInitializer init, WithTeslaChargeOverlayInfo info)
		{
			this.info = info;

			renderSprites = init.Self.Trait<RenderSprites>();

			overlay = new Animation(init.World, renderSprites.GetImage(init.Self));

			renderSprites.Add(new AnimationWithOffset(overlay, null, () => !charging),
				info.Palette, info.IsPlayerPalette);
		}

		void INotifyTeslaCharging.Charging(Actor self, in Target target)
		{
			charging = true;
			overlay.PlayThen(RenderSprites.NormalizeSequence(overlay, self.GetDamageState(), info.Sequence), () => charging = false);
		}

		void INotifyDamageStateChanged.DamageStateChanged(Actor self, AttackInfo e)
		{
			overlay.ReplaceAnim(RenderSprites.NormalizeSequence(overlay, e.DamageState, info.Sequence));
		}

		void INotifySold.Sold(Actor self) { }
		void INotifySold.Selling(Actor self)
		{
			charging = false;
		}
	}
}
