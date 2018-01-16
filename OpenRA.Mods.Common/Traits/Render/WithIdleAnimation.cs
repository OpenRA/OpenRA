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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Periodically plays an idle animation, replacing the default body animation.")]
	public class WithIdleAnimationInfo : ConditionalTraitInfo, Requires<WithSpriteBodyInfo>
	{
		[SequenceReference, Desc("Sequence names to use.")]
		public readonly string[] Sequences = { "active" };

		public readonly int Interval = 750;

		public override object Create(ActorInitializer init) { return new WithIdleAnimation(init.Self, this); }
	}

	public class WithIdleAnimation : ConditionalTrait<WithIdleAnimationInfo>, ITick, INotifyBuildComplete, INotifySold
	{
		readonly WithSpriteBody wsb;
		bool buildComplete;
		int ticks;

		public WithIdleAnimation(Actor self, WithIdleAnimationInfo info)
			: base(info)
		{
			wsb = self.Trait<WithSpriteBody>();
			buildComplete = !self.Info.HasTraitInfo<BuildingInfo>(); // always render instantly for units
			ticks = info.Interval;
		}

		void ITick.Tick(Actor self)
		{
			if (!buildComplete || IsTraitDisabled)
				return;

			if (--ticks <= 0)
			{
				wsb.PlayCustomAnimation(self, Info.Sequences.Random(Game.CosmeticRandom));
				ticks = Info.Interval;
			}
		}

		void INotifyBuildComplete.BuildingComplete(Actor self)
		{
			buildComplete = true;
		}

		void INotifySold.Selling(Actor self)
		{
			buildComplete = false;
		}

		void INotifySold.Sold(Actor self) { }
	}
}
