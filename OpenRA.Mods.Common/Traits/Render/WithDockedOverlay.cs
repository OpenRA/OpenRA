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

using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Rendered when a harvester is docked.")]
	public class WithDockedOverlayInfo : ITraitInfo, Requires<RenderSpritesInfo>, Requires<BodyOrientationInfo>
	{
		[Desc("Sequence name to use")]
		[SequenceReference] public readonly string Sequence = "docking-overlay";

		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		[Desc("Custom palette name")]
		[PaletteReference("IsPlayerPalette")] public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		public object Create(ActorInitializer init) { return new WithDockedOverlay(init.Self, this); }
	}

	public class WithDockedOverlay : INotifyDocking, INotifyBuildComplete, INotifySold
	{
		readonly WithDockedOverlayInfo info;
		readonly AnimationWithOffset anim;
		bool buildComplete;
		bool docked;

		public WithDockedOverlay(Actor self, WithDockedOverlayInfo info)
		{
			this.info = info;

			var rs = self.Trait<RenderSprites>();
			var body = self.Trait<BodyOrientation>();

			buildComplete = !self.Info.HasTraitInfo<BuildingInfo>(); // always render instantly for units

			var overlay = new Animation(self.World, rs.GetImage(self));
			overlay.Play(info.Sequence);

			anim = new AnimationWithOffset(overlay,
				() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self, self.Orientation))),
				() => !buildComplete || !docked);

			rs.Add(anim, info.Palette, info.IsPlayerPalette);
		}

		void PlayDockingOverlay()
		{
			if (docked)
				anim.Animation.PlayThen(info.Sequence, PlayDockingOverlay);
		}

		public void BuildingComplete(Actor self)
		{
			self.World.AddFrameEndTask(w => w.Add(new DelayedAction(120, () =>
					buildComplete = true)));
		}

		public void Sold(Actor self) { }
		public void Selling(Actor self)
		{
			buildComplete = false;
		}

		public void Docked(Actor self, Actor harvester) { docked = true; PlayDockingOverlay(); }
		public void Undocked(Actor self, Actor harvester) { docked = false; }
	}
}