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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Renders an overlay when the actor is taking heavy damage.")]
	public class WithDamageOverlayInfo : TraitInfo, Requires<RenderSpritesInfo>
	{
		public readonly string Image = null;

		[SequenceReference(nameof(Image))]
		public readonly string StartSequence = null;

		[SequenceReference(nameof(Image))]
		[FieldLoader.Require]
		public readonly string LoopSequence = null;

		[SequenceReference(nameof(Image))]
		public readonly string EndSequence = null;

		[Desc("How often is LoopSequence repeated. Negative means infinitely (or until an invalid damage state is reached).")]
		public readonly int LoopCount = 0;

		[PaletteReference(nameof(IsPlayerPalette))]
		[Desc("Custom palette name.")]
		public readonly string Palette = null;

		[Desc("Custom palette is a player palette BaseName.")]
		public readonly bool IsPlayerPalette = false;

		[Desc("Damage types that this should be used for (defined on the warheads).",
			"Leave empty to disable all filtering.")]
		public readonly BitSet<DamageType> DamageTypes = default(BitSet<DamageType>);

		[Desc("Trigger when Undamaged, Light, Medium, Heavy, Critical or Dead.")]
		public readonly DamageState MinimumDamageState = DamageState.Heavy;
		public readonly DamageState MaximumDamageState = DamageState.Dead;

		public override object Create(ActorInitializer init) { return new WithDamageOverlay(init.Self, this); }
	}

	public class WithDamageOverlay : INotifyDamage, INotifyDamageStateChanged
	{
		readonly WithDamageOverlayInfo info;
		readonly Animation anim;
		readonly bool hasStartSequence;
		readonly bool hasEndSequence;
		readonly bool isLoopingInfinitely;

		bool isSmoking;

		public WithDamageOverlay(Actor self, WithDamageOverlayInfo info)
		{
			this.info = info;

			hasStartSequence = !string.IsNullOrEmpty(info.StartSequence);
			hasEndSequence = !string.IsNullOrEmpty(info.EndSequence);
			isLoopingInfinitely = info.LoopCount < 0;

			var rs = self.Trait<RenderSprites>();

			anim = new Animation(self.World, info.Image != null ? info.Image : rs.GetImage(self));
			rs.Add(new AnimationWithOffset(anim, null, () => !isSmoking),
				info.Palette, info.IsPlayerPalette);
		}

		void INotifyDamageStateChanged.DamageStateChanged(Actor self, AttackInfo e)
		{
			if (!isSmoking || !isLoopingInfinitely)
				return;

			if (e.DamageState >= info.MinimumDamageState && e.DamageState <= info.MaximumDamageState)
				return;

			if (hasEndSequence)
				anim.PlayThen(info.LoopSequence,
					() => anim.PlayThen(info.EndSequence,
						() => isSmoking = false));
			else
				anim.PlayThen(info.LoopSequence,
					() => isSmoking = false);
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (!info.DamageTypes.IsEmpty && !e.Damage.DamageTypes.Overlaps(info.DamageTypes))
				return;

			if (isSmoking)
				return;

			// Getting healed
			if (e.Damage.Value < 0)
				return;

			if (e.DamageState < info.MinimumDamageState || e.DamageState > info.MaximumDamageState)
				return;

			isSmoking = true;
			if (isLoopingInfinitely)
			{
				if (hasStartSequence)
					anim.PlayThen(info.StartSequence,
						() => anim.PlayRepeating(info.LoopSequence));
				else
					anim.PlayRepeating(info.LoopSequence);
			}
			else if (!hasStartSequence && !hasEndSequence)
				anim.PlayNTimesThen(info.LoopSequence, 1 + info.LoopCount,
					() => isSmoking = false);
			else if (!hasEndSequence)
				anim.PlayThen(info.StartSequence,
					() => anim.PlayNTimesThen(info.LoopSequence, 1 + info.LoopCount,
						() => isSmoking = false));
			else if (!hasStartSequence)
				anim.PlayNTimesThen(info.LoopSequence, 1 + info.LoopCount,
					() => anim.PlayThen(info.EndSequence,
						() => isSmoking = false));
			else
				anim.PlayThen(info.StartSequence,
					() => anim.PlayNTimesThen(info.LoopSequence, 1 + info.LoopCount,
						() => anim.PlayThen(info.EndSequence,
							() => isSmoking = false)));
		}
	}
}
