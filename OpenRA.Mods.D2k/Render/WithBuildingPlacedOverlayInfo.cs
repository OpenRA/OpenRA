#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Traits;
using OpenRA.Mods.RA.Buildings;

namespace OpenRA.Mods.RA.Render
{
	public class WithBuildingPlacedOverlayInfo : ITraitInfo, Requires<RenderSpritesInfo>, Requires<IBodyOrientationInfo>
	{
		[Desc("Sequence name to use")]
		public readonly string Sequence = "crane-overlay";

		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

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
					() => !buildComplete));
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