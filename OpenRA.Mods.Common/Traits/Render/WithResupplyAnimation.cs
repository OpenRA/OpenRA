#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
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
	[Desc("Replaces the default animation when actor resupplies a unit.")]
	public class WithResupplyAnimationInfo : ConditionalTraitInfo, Requires<WithSpriteBodyInfo>
	{
		[Desc("Sequence name to use")]
		[SequenceReference] public readonly string Sequence = "active";

		public override object Create(ActorInitializer init) { return new WithResupplyAnimation(init.Self, this); }
	}

	public class WithResupplyAnimation : ConditionalTrait<WithResupplyAnimationInfo>, INotifyRepair, INotifyRearm, INotifyBuildComplete, INotifySold, ITick
	{
		readonly WithSpriteBody spriteBody;
		bool buildComplete;
		bool animPlaying;
		bool repairing;
		bool rearming;

		public WithResupplyAnimation(Actor self, WithResupplyAnimationInfo info)
			: base(info)
		{
			spriteBody = self.TraitOrDefault<WithSpriteBody>();
		}

		void ITick.Tick(Actor self)
		{
			if (!buildComplete || spriteBody == null || IsTraitDisabled)
				return;

			if (!animPlaying && (repairing || rearming))
			{
				spriteBody.PlayCustomAnimationRepeating(self, Info.Sequence);
				animPlaying = true;
			}
			else if (animPlaying && !repairing && !rearming)
			{
				spriteBody.CancelCustomAnimation(self);
				animPlaying = false;
			}
		}

		void INotifyRepair.BeforeRepair(Actor self, Actor target)
		{
			repairing = true;
		}

		void INotifyRepair.RepairTick(Actor self, Actor target) { }

		void INotifyRepair.AfterRepair(Actor self, Actor target)
		{
			repairing = false;
		}

		void INotifyRearm.RearmingStarted(Actor self, Actor target)
		{
			rearming = true;
		}

		void INotifyRearm.Rearming(Actor self, Actor target) { }

		void INotifyRearm.RearmingFinished(Actor self, Actor target)
		{
			rearming = false;
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