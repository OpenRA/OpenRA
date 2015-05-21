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
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Default trait for rendering sprite-based actors.")]
	class WithSpriteBodyInfo : UpgradableTraitInfo, ITraitInfo, Requires<RenderSpritesInfo>
	{
		[Desc("Animation to play when the actor is created.")]
		public readonly string StartSequence = null;

		[Desc("Animation to play when the actor is idle.")]
		public readonly string Sequence = "idle";

		public object Create(ActorInitializer init) { return new WithSpriteBody(init, this); }
	}

	class WithSpriteBody : UpgradableTrait<WithSpriteBodyInfo>, ISpriteBody
	{
		readonly Animation body;
		readonly WithSpriteBodyInfo info;

		public WithSpriteBody(ActorInitializer init, WithSpriteBodyInfo info)
			: base(info)
		{
			this.info = info;

			var rs = init.Self.Trait<RenderSprites>();
			body = new Animation(init.Self.World, rs.GetImage(init.Self));
			PlayCustomAnimation(init.Self, info.StartSequence, () => body.PlayRepeating(info.Sequence));
			rs.Add(new AnimationWithOffset(body, null, () => IsTraitDisabled));
		}

		public void PlayCustomAnimation(Actor self, string newAnimation, Action after)
		{
			body.PlayThen(newAnimation, () =>
			{
				body.Play(info.Sequence);
				if (after != null)
					after();
			});
		}

		public void PlayCustomAnimationRepeating(Actor self, string name)
		{
			body.PlayThen(name, () => PlayCustomAnimationRepeating(self, name));
		}

		public void PlayCustomAnimationBackwards(Actor self, string name, Action after)
		{
			body.PlayBackwardsThen(name, () =>
			{
				body.PlayRepeating(info.Sequence);
				if (after != null)
					after();
			});
		}
	}
}
