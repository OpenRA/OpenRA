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
	public class WithDamageOverlayInfo : ConditionalTraitInfo, Requires<RenderSpritesInfo>
	{
		public readonly string Image = "smoke_m";

		[SequenceReference(nameof(Image))]
		public readonly string StartSequence = "";

		[SequenceReference(nameof(Image))]
		public readonly string LoopSequence = "loop";

		[SequenceReference(nameof(Image))]
		public readonly string EndSequence = "";

		[Desc("Position relative to the body orientation.")]
		public readonly WVec Offset = WVec.Zero;

		[Desc("How many times should " + nameof(LoopSequence),
			" be played? A range can be provided to be randomly chosen from.")]
		public readonly int[] LoopCount = { 2 };

		[Desc("Initial delay before animation is enabled",
			"Two values indicate a random delay range.")]
		public readonly int[] InitialDelay = { 0 };

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

		[Desc("Trigger when Undamaged, Light, Medium, Heavy, Critical or Dead.")]
		public readonly DamageState MaximumDamageState = DamageState.Dead;

		public IEnumerable<string> GetSequences(MersenneTwister random)
		{
			if (!string.IsNullOrEmpty(StartSequence))
				yield return StartSequence;

			if (!string.IsNullOrEmpty(LoopSequence))
			{
				var loopCount = Util.RandomInRange(random, LoopCount);
				for (var i = 0; i < loopCount; i++)
					yield return LoopSequence;
			}

			if (!string.IsNullOrEmpty(EndSequence))
				yield return EndSequence;
		}

		public override object Create(ActorInitializer init) { return new WithDamageOverlay(init.Self, this); }
		public override void RulesetLoaded(Ruleset rules, ActorInfo info)
		{
			if (Offset != WVec.Zero && !info.HasTraitInfo<BodyOrientationInfo>())
				throw new YamlException("Specifying WithDamageOverlay.Offset requires the BodyOrientation trait on the actor.");
		}
	}

	public class WithDamageOverlay : ConditionalTrait<WithDamageOverlayInfo>, INotifyDamage, ITick
	{
		readonly WithDamageOverlayInfo info;
		readonly Animation anim;
		IEnumerator<string> sequences;

		bool isPlayingAnimation;

		int delay = -1;

		public WithDamageOverlay(Actor self, WithDamageOverlayInfo info)
			: base(info)
		{
			this.info = info;
			anim = new Animation(self.World, info.Image);
		}

		protected override void Created(Actor self)
		{
			var rs = self.Trait<RenderSprites>();
			var body = self.TraitOrDefault<BodyOrientation>();

			WVec AnimationOffset() => body.LocalToWorld(info.Offset.Rotate(body.QuantizeOrientation(self.Orientation)));
			rs.Add(new AnimationWithOffset(anim, info.Offset == WVec.Zero || body == null ? null : AnimationOffset, () => !isPlayingAnimation));
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (IsTraitDisabled
				|| e.DamageState < info.MinimumDamageState
				|| e.DamageState > info.MaximumDamageState)
			{
				isPlayingAnimation = false;
				return;
			}

			// Getting healed.
			if (e.Damage.Value < 0)
				return;

			if (!isPlayingAnimation && delay <= -1)
			{
				delay = Util.RandomInRange(self.World.SharedRandom, info.InitialDelay);
				if (delay <= 0)
					StartAnimation(self.World.LocalRandom);
			}
		}

		void ITick.Tick(Actor self)
		{
			if (delay < 0)
				return;

			// Actor DamgageState may have changed.
			if (self.GetDamageState() < info.MinimumDamageState || self.GetDamageState() > info.MaximumDamageState)
				delay = -1;
			else if (--delay <= 0)
				StartAnimation(self.World.LocalRandom);
		}

		protected override void TraitDisabled(Actor self)
		{
			isPlayingAnimation = false;
		}

		void StartAnimation(MersenneTwister random)
		{
			isPlayingAnimation = true;
			delay = -1;
			sequences?.Dispose();
			sequences = info.GetSequences(random).GetEnumerator();
			PlayAnimation();
		}

		void PlayAnimation()
		{
			if (sequences.MoveNext())
				anim.PlayThen(sequences.Current, () => PlayAnimation());
			else
			{
				isPlayingAnimation = false;
				sequences.Dispose();
			}
		}
	}
}
