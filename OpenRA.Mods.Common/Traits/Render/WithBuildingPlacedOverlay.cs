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
	[Desc("Rendered when the actor constructed a building.")]
	public class WithBuildingPlacedOverlayInfo : ITraitInfo, Requires<RenderSpritesInfo>, Requires<BodyOrientationInfo>
	{
		[Desc("Sequence name to use")]
		[SequenceReference] public readonly string Sequence = "crane-overlay";

		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		[Desc("Custom palette name")]
		[PaletteReference("IsPlayerPalette")] public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		public object Create(ActorInitializer init) { return new WithBuildingPlacedOverlay(init.Self, this); }
	}

	public class WithBuildingPlacedOverlay : INotifyBuildComplete, INotifySold, INotifyDamageStateChanged, INotifyBuildingPlaced, INotifyTransform
	{
		readonly Animation overlay;
		bool buildComplete;
		bool visible;

		public WithBuildingPlacedOverlay(Actor self, WithBuildingPlacedOverlayInfo info)
		{
			var rs = self.Trait<RenderSprites>();
			var body = self.Trait<BodyOrientation>();

			buildComplete = !self.Info.HasTraitInfo<BuildingInfo>(); // always render instantly for units

			overlay = new Animation(self.World, rs.GetImage(self));

			var anim = new AnimationWithOffset(overlay,
				() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self, self.Orientation))),
				() => !visible || !buildComplete);

			overlay.PlayThen(info.Sequence, () => visible = false);
			rs.Add(anim, info.Palette, info.IsPlayerPalette);
		}

		void INotifyBuildComplete.BuildingComplete(Actor self)
		{
			buildComplete = true;
			visible = false;
		}

		void INotifySold.Sold(Actor self) { }
		void INotifySold.Selling(Actor self)
		{
			buildComplete = false;
		}

		void INotifyTransform.BeforeTransform(Actor self)
		{
			buildComplete = false;
		}

		void INotifyTransform.OnTransform(Actor self) { }
		void INotifyTransform.AfterTransform(Actor self) { }

		void INotifyDamageStateChanged.DamageStateChanged(Actor self, AttackInfo e)
		{
			overlay.ReplaceAnim(RenderSprites.NormalizeSequence(overlay, e.DamageState, overlay.CurrentSequence.Name));
		}

		void INotifyBuildingPlaced.BuildingPlaced(Actor self)
		{
			visible = true;
			overlay.PlayThen(overlay.CurrentSequence.Name, () => visible = false);
		}
	}
}