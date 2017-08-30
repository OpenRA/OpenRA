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

using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Displays an overlay when the building is being repaired by the player.")]
	public class WithRepairOverlayInfo : PausableConditionalTraitInfo, Requires<RenderSpritesInfo>, Requires<BodyOrientationInfo>
	{
		[Desc("Sequence to use upon repair beginning.")]
		[SequenceReference("Image")] public readonly string StartSequence = null;

		[Desc("Sequence name to play once during repair intervals or repeatedly if a start sequence is set.")]
		[SequenceReference] public readonly string Sequence = "active";

		[Desc("Sequence to use after repairing has finished.")]
		[SequenceReference("Image")] public readonly string EndSequence = null;

		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		[Desc("Custom palette name")]
		[PaletteReference("IsPlayerPalette")] public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		public override object Create(ActorInitializer init) { return new WithRepairOverlay(init.Self, this); }
	}

	public class WithRepairOverlay : PausableConditionalTrait<WithRepairOverlayInfo>, INotifyDamageStateChanged, INotifyBuildComplete, INotifySold, INotifyRepair
	{
		readonly Animation overlay;
		bool buildComplete;
		bool visible;

		public WithRepairOverlay(Actor self, WithRepairOverlayInfo info)
			: base(info)
		{
			var rs = self.Trait<RenderSprites>();
			var body = self.Trait<BodyOrientation>();

			buildComplete = !self.Info.HasTraitInfo<BuildingInfo>(); // always render instantly for units
			overlay = new Animation(self.World, rs.GetImage(self), () => IsTraitPaused);
			overlay.PlayThen(info.Sequence, () => visible = false);

			var anim = new AnimationWithOffset(overlay,
				() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self, self.Orientation))),
				() => IsTraitDisabled || !visible || !buildComplete,
				p => RenderUtils.ZOffsetFromCenter(self, p, 1));

			rs.Add(anim, info.Palette, info.IsPlayerPalette);
		}

		void INotifyBuildComplete.BuildingComplete(Actor self)
		{
			buildComplete = true;
		}

		void INotifySold.Sold(Actor self) { }
		void INotifySold.Selling(Actor self)
		{
			buildComplete = false;
		}

		void INotifyDamageStateChanged.DamageStateChanged(Actor self, AttackInfo e)
		{
			overlay.ReplaceAnim(RenderSprites.NormalizeSequence(overlay, e.DamageState, overlay.CurrentSequence.Name));
		}

		void INotifyRepair.BeforeRepair(Actor self, Actor host)
		{
			if (Info.StartSequence != null)
			{
				visible = true;
				overlay.PlayThen(RenderSprites.NormalizeSequence(overlay, self.GetDamageState(), Info.StartSequence),
					() => overlay.PlayRepeating(RenderSprites.NormalizeSequence(overlay, self.GetDamageState(), Info.Sequence)));
			}
		}

		void INotifyRepair.RepairTick(Actor self, Actor host)
		{
			if (Info.StartSequence == null)
			{
				visible = true;
				overlay.PlayThen(overlay.CurrentSequence.Name, () => visible = false);
			}
		}

		void INotifyRepair.AfterRepair(Actor self, Actor target)
		{
			if (Info.EndSequence != null)
			{
				visible = true;
				overlay.PlayThen(Info.EndSequence, () => visible = false);
			}
		}
	}
}