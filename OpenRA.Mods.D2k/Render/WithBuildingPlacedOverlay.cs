#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Render
{
	[Desc("Rendered when the actor constructed a building.")]
	public class WithBuildingPlacedOverlayInfo : ITraitInfo, Requires<RenderSpritesInfo>, Requires<IBodyOrientationInfo>
	{
		[Desc("Sequence name to use")]
		public readonly string Sequence = "crane-overlay";

		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		[Desc("Custom palette name")]
		public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		public object Create(ActorInitializer init) { return new WithBuildingPlacedOverlay(init.self, this); }
	}

	public class WithBuildingPlacedOverlay : INotifyBuildComplete, INotifySold, INotifyDamageStateChanged, INotifyBuildingPlaced
	{
		Animation overlay;
		bool buildComplete;

		public WithBuildingPlacedOverlay(Actor self, WithBuildingPlacedOverlayInfo info)
		{
			var rs = self.Trait<RenderSprites>();
			var body = self.Trait<IBodyOrientation>();

			buildComplete = !self.HasTrait<Building>(); // always render instantly for units

			overlay = new Animation(self.World, rs.GetImage(self));
			overlay.Play(info.Sequence);
			rs.Add("crane_overlay_{0}".F(info.Sequence),
				new AnimationWithOffset(overlay,
					() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self, self.Orientation))),
					() => !buildComplete),
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

		public void BuildingPlaced(Actor self)
		{
			overlay.Play(overlay.CurrentSequence.Name);
		}
	}
}