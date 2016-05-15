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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Plays an animation replacing the default body animation when resources are spawned.")]
	public class WithResourceSeedingAnimationInfo : UpgradableTraitInfo, Requires<WithSpriteBodyInfo>
	{
		[SequenceReference, Desc("Sequence names to use.")]
		public readonly string[] Sequences = { "active" };

		[Desc("Time (in ms) to wait before playing.")]
		public readonly int Delay = 0;

		public override object Create(ActorInitializer init) { return new WithResourceSeedingAnimation(init.Self, this); }
	}

	public class WithResourceSeedingAnimation : UpgradableTrait<WithResourceSeedingAnimationInfo>, INotifyResourceSeeded, INotifyBuildComplete
	{
		readonly WithSpriteBody wsb;
		bool buildComplete;

		public WithResourceSeedingAnimation(Actor self, WithResourceSeedingAnimationInfo info)
			: base(info)
		{
			wsb = self.Trait<WithSpriteBody>();
			buildComplete = !self.Info.HasTraitInfo<BuildingInfo>(); // always render instantly for units
		}

		void INotifyResourceSeeded.OnResourceSeeded(Actor self)
		{
			if (!buildComplete || IsTraitDisabled)
				return;

			Game.RunAfterDelay(Info.Delay, () => {
				wsb.PlayCustomAnimation(self, Info.Sequences.Random(Game.CosmeticRandom),
					() => wsb.CancelCustomAnimation(self));
			});
		}

		public void BuildingComplete(Actor self)
		{
			buildComplete = true;
		}
	}
}
