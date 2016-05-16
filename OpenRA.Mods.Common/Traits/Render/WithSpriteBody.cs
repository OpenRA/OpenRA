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

using System;
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Default trait for rendering sprite-based actors.")]
	public class WithSpriteBodyInfo : UpgradableTraitInfo, IRenderActorPreviewSpritesInfo, Requires<RenderSpritesInfo>
	{
		[Desc("Animation to play when the actor is created."), SequenceReference]
		public readonly string StartSequence = null;

		[Desc("Animation to play when the actor is idle."), SequenceReference]
		public readonly string Sequence = "idle";

		[Desc("Pause animation when actor is disabled.")]
		public readonly bool PauseAnimationWhenDisabled = false;

		public override object Create(ActorInitializer init) { return new WithSpriteBody(init, this); }

		public virtual IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, RenderSpritesInfo rs, string image, int facings, PaletteReference p)
		{
			if (UpgradeMinEnabledLevel > 0)
				yield break;

			var anim = new Animation(init.World, image);
			anim.PlayRepeating(RenderSprites.NormalizeSequence(anim, init.GetDamageState(), Sequence));

			yield return new SpriteActorPreview(anim, () => WVec.Zero, () => 0, p, rs.Scale);
		}
	}

	public class WithSpriteBody : UpgradableTrait<WithSpriteBodyInfo>, INotifyDamageStateChanged, INotifyBuildComplete
	{
		public readonly Animation DefaultAnimation;

		public WithSpriteBody(ActorInitializer init, WithSpriteBodyInfo info)
			: this(init, info, () => 0) { }

		protected WithSpriteBody(ActorInitializer init, WithSpriteBodyInfo info, Func<int> baseFacing)
			: base(info)
		{
			var rs = init.Self.Trait<RenderSprites>();

			Func<bool> paused = null;
			if (info.PauseAnimationWhenDisabled)
				paused = () => init.Self.IsDisabled() &&
				DefaultAnimation.CurrentSequence.Name == NormalizeSequence(init.Self, Info.Sequence);

			DefaultAnimation = new Animation(init.World, rs.GetImage(init.Self), baseFacing, paused);
			rs.Add(new AnimationWithOffset(DefaultAnimation, null, () => IsTraitDisabled));

			if (info.StartSequence != null)
				PlayCustomAnimation(init.Self, info.StartSequence,
					() => PlayCustomAnimationRepeating(init.Self, info.Sequence));
			else
				DefaultAnimation.PlayRepeating(NormalizeSequence(init.Self, info.Sequence));
		}

		public string NormalizeSequence(Actor self, string sequence)
		{
			return RenderSprites.NormalizeSequence(DefaultAnimation, self.GetDamageState(), sequence);
		}

		// TODO: Get rid of INotifyBuildComplete in favor of using the upgrade system
		public virtual void BuildingComplete(Actor self)
		{
			DefaultAnimation.PlayRepeating(NormalizeSequence(self, Info.Sequence));
		}

		public void PlayCustomAnimation(Actor self, string name, Action after = null)
		{
			DefaultAnimation.PlayThen(NormalizeSequence(self, name), () =>
			{
				DefaultAnimation.Play(NormalizeSequence(self, Info.Sequence));
				if (after != null)
					after();
			});
		}

		public void PlayCustomAnimationRepeating(Actor self, string name)
		{
			var sequence = NormalizeSequence(self, name);
			DefaultAnimation.PlayThen(sequence, () => PlayCustomAnimationRepeating(self, sequence));
		}

		public void PlayCustomAnimationBackwards(Actor self, string name, Action after = null)
		{
			DefaultAnimation.PlayBackwardsThen(NormalizeSequence(self, name), () =>
			{
				DefaultAnimation.PlayRepeating(NormalizeSequence(self, Info.Sequence));
				if (after != null)
					after();
			});
		}

		public void CancelCustomAnimation(Actor self)
		{
			DefaultAnimation.PlayRepeating(NormalizeSequence(self, Info.Sequence));
		}

		public virtual void DamageStateChanged(Actor self, AttackInfo e)
		{
			if (DefaultAnimation.CurrentSequence != null)
				DefaultAnimation.ReplaceAnim(NormalizeSequence(self, DefaultAnimation.CurrentSequence.Name));
		}
	}
}
