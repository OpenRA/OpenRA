#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Periodically plays an idle animation, replacing the default body animation.")]
	public class WithIdleAnimationInfo : ConditionalTraitInfo, Requires<WithSpriteBodyInfo>
	{
		[SequenceReference, Desc("Sequence names to use.")]
		public readonly string[] Sequences = { "active" };

		public readonly int Interval = 750;

		[Desc("Which sprite body to play the animation on.")]
		public readonly string Body = "body";

		public override object Create(ActorInitializer init) { return new WithIdleAnimation(init.Self, this); }
	}

	public class WithIdleAnimation : ConditionalTrait<WithIdleAnimationInfo>, ITick
	{
		readonly WithSpriteBody wsb;
		int ticks;

		public WithIdleAnimation(Actor self, WithIdleAnimationInfo info)
			: base(info)
		{
			wsb = self.TraitsImplementing<WithSpriteBody>().Single(w => w.Info.Name == Info.Body);
			ticks = info.Interval;
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (--ticks <= 0)
			{
				wsb.PlayCustomAnimation(self, Info.Sequences.Random(Game.CosmeticRandom));
				ticks = Info.Interval;
			}
		}

		protected override void TraitDisabled(Actor self)
		{
			wsb.CancelCustomAnimation(self);
		}
	}
}
