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
	// TODO: Refactor this trait into WithResupplyOverlay
	[Desc("Displays an overlay when the building is being repaired by the player.")]
	public class WithRepairOverlayInfo : PausableConditionalTraitInfo, Requires<RenderSpritesInfo>, Requires<BodyOrientationInfo>
	{
		[SequenceReference]
		[Desc("Sequence to use upon repair beginning.")]
		public readonly string StartSequence = null;

		[SequenceReference]
		[Desc("Sequence name to play once during repair intervals or repeatedly if a start sequence is set.")]
		public readonly string Sequence = "active";

		[SequenceReference]
		[Desc("Sequence to use after repairing has finished.")]
		public readonly string EndSequence = null;

		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		[PaletteReference(nameof(IsPlayerPalette))]
		[Desc("Custom palette name")]
		public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		public override object Create(ActorInitializer init) { return new WithRepairOverlay(init.Self, this); }
	}

	public class WithRepairOverlay : PausableConditionalTrait<WithRepairOverlayInfo>, INotifyDamageStateChanged, INotifyResupply
	{
		readonly Animation overlay;
		bool visible;
		bool repairing;

		public WithRepairOverlay(Actor self, WithRepairOverlayInfo info)
			: base(info)
		{
			var rs = self.Trait<RenderSprites>();
			var body = self.Trait<BodyOrientation>();

			overlay = new Animation(self.World, rs.GetImage(self), () => IsTraitPaused);
			overlay.PlayThen(info.Sequence, () => visible = false);

			var anim = new AnimationWithOffset(overlay,
				() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self.Orientation))),
				() => IsTraitDisabled || !visible,
				p => RenderUtils.ZOffsetFromCenter(self, p, 1));

			rs.Add(anim, info.Palette, info.IsPlayerPalette);
		}

		void INotifyDamageStateChanged.DamageStateChanged(Actor self, AttackInfo e)
		{
			overlay.ReplaceAnim(RenderSprites.NormalizeSequence(overlay, e.DamageState, overlay.CurrentSequence.Name));
		}

		void INotifyResupply.BeforeResupply(Actor self, Actor target, ResupplyType types)
		{
			repairing = types.HasFlag(ResupplyType.Repair);
			if (!repairing)
				return;

			if (Info.StartSequence != null)
			{
				visible = true;
				overlay.PlayThen(RenderSprites.NormalizeSequence(overlay, self.GetDamageState(), Info.StartSequence),
					() => overlay.PlayRepeating(RenderSprites.NormalizeSequence(overlay, self.GetDamageState(), Info.Sequence)));
			}
		}

		void INotifyResupply.ResupplyTick(Actor self, Actor target, ResupplyType types)
		{
			var wasRepairing = repairing;
			repairing = types.HasFlag(ResupplyType.Repair);

			if (repairing && Info.StartSequence == null && !visible)
			{
				visible = true;
				overlay.PlayThen(overlay.CurrentSequence.Name, () => visible = false);
			}

			if (!repairing && wasRepairing && Info.EndSequence != null)
			{
				visible = true;
				overlay.PlayThen(Info.EndSequence, () => visible = false);
			}
		}
	}
}
