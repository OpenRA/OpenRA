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

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Renders an overlay when the actor is taking heavy damage.")]
	public class WithDamageOverlayInfo : TraitInfo, Requires<RenderSpritesInfo>, IRulesetLoaded
	{
		public readonly string Image = "smoke_m";

		[SequenceReference(nameof(Image))]
		public readonly string IdleSequence = "idle";

		[SequenceReference(nameof(Image))]
		public readonly string LoopSequence = "loop";

		[SequenceReference(nameof(Image))]
		public readonly string EndSequence = "end";

		[Desc("Position relative to the body orientation.")]
		public readonly WVec Offset = WVec.Zero;

		[Desc("How many times should " + nameof(LoopSequence) + " be played? A range can be provided to be randomly chosen from.")]
		public readonly int[] LoopCount = { 2 };

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

		public IEnumerable<string> GetSequences(MersenneTwister random)
		{
			if (!string.IsNullOrEmpty(IdleSequence))
				yield return IdleSequence;

			if (!string.IsNullOrEmpty(LoopSequence))
			{
				var loopCount = LoopCount.Length == 2 ? random.Next(LoopCount[0], LoopCount[1] + 1) : LoopCount[0];
				for (var i = 0; i < loopCount; i++)
					yield return LoopSequence;
			}

			if (!string.IsNullOrEmpty(EndSequence))
				yield return EndSequence;
		}

		public override object Create(ActorInitializer init) { return new WithDamageOverlay(init.Self, this); }

		public void RulesetLoaded(Ruleset rules, ActorInfo info)
		{
			if (Offset != WVec.Zero && !info.HasTraitInfo<BodyOrientationInfo>())
				throw new YamlException("Specifying WithDamageOverlay.Offset requires the BodyOrientation trait on the actor.");
		}
	}

	public class WithDamageOverlay : INotifyDamage, INotifyCreated
	{
		readonly WithDamageOverlayInfo info;
		readonly Animation anim;

		bool isPlayingAnimation;

		public WithDamageOverlay(Actor self, WithDamageOverlayInfo info)
		{
			this.info = info;
			anim = new Animation(self.World, info.Image);
		}

		void INotifyCreated.Created(Actor self)
		{
			var rs = self.Trait<RenderSprites>();
			var body = self.TraitOrDefault<BodyOrientation>();

			WVec AnimationOffset() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self.Orientation)));
			rs.Add(new AnimationWithOffset(anim, info.Offset == WVec.Zero || body == null ? null : AnimationOffset, () => !isPlayingAnimation));
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (!info.DamageTypes.IsEmpty && !e.Damage.DamageTypes.Overlaps(info.DamageTypes))
				return;

			if (isPlayingAnimation) return;
			if (e.Damage.Value < 0) return; /* getting healed */
			if (e.DamageState < info.MinimumDamageState) return;
			if (e.DamageState > info.MaximumDamageState) return;

			PlayAnimation(info.GetSequences(self.World.LocalRandom).GetEnumerator());
		}

		void PlayAnimation(IEnumerator<string> sequences)
		{
			if (sequences.MoveNext())
			{
				isPlayingAnimation = true;
				anim.PlayThen(sequences.Current, () => PlayAnimation(sequences));
			}
			else
				isPlayingAnimation = false;
		}
	}
}
