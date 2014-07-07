#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	[Desc("Renders a decorative animation on units and buildings.")]
	public class WithIdleOverlayInfo : ITraitInfo, IRenderPlaceBuildingPreviewInfo, Requires<RenderSpritesInfo>, Requires<IBodyOrientationInfo>
	{
		[Desc("Sequence name to use")]
		public readonly string Sequence = "idle-overlay";

		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		[Desc("Custom palette name")]
		public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		public readonly bool PauseOnLowPower = false;

		public IEnumerable<IRenderable> RenderPreview(WorldRenderer wr, ActorInfo ai, Player owner)
		{
			var rs = ai.Traits.Get<RenderSpritesInfo>();
			var palette = rs.Palette ?? (owner != null ? rs.PlayerPalette + owner.InternalName : null);
			var anim = new Animation(wr.world, RenderSprites.GetImage(ai), () => 0);
			anim.PlayRepeating(Sequence);

			return anim.Render(WPos.Zero, wr.Palette(palette));
		}

		public object Create(ActorInitializer init) { return new WithIdleOverlay(init.self, this); }
	}

	public class WithIdleOverlay : INotifyDamageStateChanged, INotifyBuildComplete, INotifySold
	{
		Animation overlay;
		bool buildComplete;

		public WithIdleOverlay(Actor self, WithIdleOverlayInfo info)
		{
			var rs = self.Trait<RenderSprites>();
			var body = self.Trait<IBodyOrientation>();
			var disabled = self.TraitsImplementing<IDisable>();

			buildComplete = !self.HasTrait<Building>(); // always render instantly for units
			overlay = new Animation(self.World, rs.GetImage(self));
			overlay.PlayRepeating(info.Sequence);
			rs.Add("idle_overlay_{0}".F(info.Sequence),
				new AnimationWithOffset(overlay,
					() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self, self.Orientation))),
					() => !buildComplete,
					() => info.PauseOnLowPower && disabled.Any(d => d.Disabled),
					p => WithTurret.ZOffsetFromCenter(self, p, 1)),
				info.Palette, info.IsPlayerPalette);
		}

		public void BuildingComplete(Actor self)
		{
			buildComplete = true;
		}

		public void Sold(Actor self) { }
		public void Selling(Actor self)
		{
			buildComplete = false;
		}

		public void DamageStateChanged(Actor self, AttackInfo e)
		{
			overlay.ReplaceAnim(RenderSprites.NormalizeSequence(overlay, e.DamageState, overlay.CurrentSequence.Name));
		}
	}
}