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
		public readonly int[] LoopCount = [1, 3];

		[Desc("Initial delay before animation is enabled",
			"Two values indicate a random delay range.")]
		public readonly int[] InitialDelay = [0];

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

		public override object Create(ActorInitializer init) { return new WithDamageOverlay(init.Self, this); }
		public override void RulesetLoaded(Ruleset rules, ActorInfo info)
		{
			if (Offset != WVec.Zero && !info.HasTraitInfo<BodyOrientationInfo>())
				throw new YamlException("Specifying WithDamageOverlay.Offset requires the BodyOrientation trait on the actor.");
			base.RulesetLoaded(rules, info);
		}
	}

	public class WithDamageOverlay : ConditionalTrait<WithDamageOverlayInfo>, INotifyDamage, ITick
	{
		readonly WithDamageOverlayInfo info;
		readonly Animation anim;

		bool isPlayingAnimation;
		int loopCount;

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
			base.Created(self);
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
					StartAnimation(self);
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
				StartAnimation(self);
		}

		protected override void TraitDisabled(Actor self)
		{
			isPlayingAnimation = false;
		}

		void StartAnimation(Actor self)
		{
			delay = -1;
			loopCount = Util.RandomInRange(self.World.SharedRandom, info.LoopCount);
			isPlayingAnimation = true;

			if (!string.IsNullOrEmpty(info.StartSequence))
				anim.PlayThen(info.StartSequence, () => PlayAnimation());
			else
				PlayAnimation();
		}

		void PlayAnimation(int animationState = -1)
		{
			if (!isPlayingAnimation)
				return;

			animationState++;
			if (animationState < loopCount && !string.IsNullOrEmpty(info.LoopSequence))
				anim.PlayThen(info.LoopSequence, () => PlayAnimation(animationState));
			else
			{
				if (!string.IsNullOrEmpty(info.EndSequence))
					anim.PlayThen(info.EndSequence, () => isPlayingAnimation = false);
				else
					isPlayingAnimation = false;
			}
		}
	}
}
