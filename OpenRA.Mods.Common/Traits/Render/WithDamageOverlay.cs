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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Renders an overlay when the actor is taking heavy damage.")]
	public class WithDamageOverlayInfo : TraitInfo, Requires<RenderSpritesInfo>
	{
		public readonly string Image = "smoke_m";

		[SequenceReference(nameof(Image))]
		public readonly string IdleSequence = "idle";

		[SequenceReference(nameof(Image))]
		public readonly string LoopSequence = "loop";

		[SequenceReference(nameof(Image))]
		public readonly string EndSequence = "end";

		[PaletteReference(nameof(IsPlayerPalette))]
		[Desc("Custom palette name.")]
		public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName.")]
		public readonly bool IsPlayerPalette = false;

		[Desc("Damage types that this should be used for (defined on the warheads).",
			"Leave empty to disable all filtering.")]
		public readonly BitSet<DamageType> DamageTypes = default;

		[Desc("Trigger when Undamaged, Light, Medium, Heavy, Critical or Dead.")]
		public readonly DamageState MinimumDamageState = DamageState.Heavy;
		public readonly DamageState MaximumDamageState = DamageState.Dead;

		public override object Create(ActorInitializer init) { return new WithDamageOverlay(init.Self, this); }
	}

	public class WithDamageOverlay : INotifyDamage
	{
		readonly WithDamageOverlayInfo info;
		readonly Animation anim;

		bool isSmoking;

		public WithDamageOverlay(Actor self, WithDamageOverlayInfo info)
		{
			this.info = info;

			var rs = self.Trait<RenderSprites>();

			anim = new Animation(self.World, info.Image);
			rs.Add(new AnimationWithOffset(anim, null, () => !isSmoking),
				info.Palette, info.IsPlayerPalette);
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (!info.DamageTypes.IsEmpty && !e.Damage.DamageTypes.Overlaps(info.DamageTypes))
				return;

			if (isSmoking) return;
			if (e.Damage.Value < 0) return;	/* getting healed */
			if (e.DamageState < info.MinimumDamageState) return;
			if (e.DamageState > info.MaximumDamageState) return;

			isSmoking = true;
			anim.PlayThen(info.IdleSequence,
				() => anim.PlayThen(info.LoopSequence,
					() => anim.PlayThen(info.EndSequence,
						() => isSmoking = false)));
		}
	}
}
