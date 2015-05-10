#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Default trait for rendering sprite-based actors.")]
	public class WithSpriteBodyInfo : UpgradableTraitInfo, ITraitInfo, IRenderActorPreviewSpritesInfo, IQuantizeBodyOrientationInfo,
		Requires<RenderSpritesInfo>
	{
		[Desc("Animation to play when the actor is created.")]
		public readonly string StartSequence = null;

		[Desc("Animation to play when the actor is idle.")]
		public readonly string Sequence = "idle";

		public virtual object Create(ActorInitializer init) { return new WithSpriteBody(init, this); }

		public virtual IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, RenderSpritesInfo rs, string image, int facings, PaletteReference p)
		{
			var anim = new Animation(init.World, image);
			anim.PlayRepeating(RenderSprites.NormalizeSequence(anim, init.GetDamageState(), Sequence));

			yield return new SpriteActorPreview(anim, WVec.Zero, 0, p, rs.Scale);
		}

		public virtual int QuantizedBodyFacings(ActorInfo ai, SequenceProvider sequenceProvider, string race)
		{
			return 1;
		}
	}

	public class WithSpriteBody : UpgradableTrait<WithSpriteBodyInfo>, ISpriteBody
	{
		public readonly Animation DefaultAnimation;

		public WithSpriteBody(ActorInitializer init, WithSpriteBodyInfo info)
			: this(init, info, () => 0) { }

		protected WithSpriteBody(ActorInitializer init, WithSpriteBodyInfo info, Func<int> baseFacing)
			: base(info)
		{
			var rs = init.Self.Trait<RenderSprites>();

			DefaultAnimation = new Animation(init.World, rs.GetImage(init.Self), baseFacing);
			rs.Add(new AnimationWithOffset(DefaultAnimation, null, () => IsTraitDisabled));

			if (Info.StartSequence != null)
				PlayCustomAnimation(init.Self, Info.StartSequence,
					() => DefaultAnimation.PlayRepeating(NormalizeSequence(init.Self, Info.Sequence)));
			else
				DefaultAnimation.PlayRepeating(NormalizeSequence(init.Self, Info.Sequence));
		}

		public string NormalizeSequence(Actor self, string sequence)
		{
			return RenderSprites.NormalizeSequence(DefaultAnimation, self.GetDamageState(), sequence);
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
			DefaultAnimation.PlayThen(name,
				() => PlayCustomAnimationRepeating(self, name));
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
	}
}
