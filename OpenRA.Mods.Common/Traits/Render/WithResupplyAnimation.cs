#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Replaces the default animation when actor resupplies a unit.")]
	public class WithResupplyAnimationInfo : ConditionalTraitInfo, Requires<WithSpriteBodyInfo>
	{
		[Desc("Sequence name to use")]
		[SequenceReference] public readonly string Sequence = "active";

		[Desc("Which sprite body to play the animation on.")]
		public readonly string Body = "body";

		[Desc("Events leading to the animation getting played. Possible values currently are: Rearm, Repair.")]
		public readonly ResupplyType PlayAnimationOn = ResupplyType.Rearm | ResupplyType.Repair;

		public override object Create(ActorInitializer init) { return new WithResupplyAnimation(init.Self, this); }
	}

	public class WithResupplyAnimation : ConditionalTrait<WithResupplyAnimationInfo>, INotifyResupply, ITick
	{
		readonly WithSpriteBody wsb;
		bool animPlaying;
		bool repairing;
		bool rearming;

		public WithResupplyAnimation(Actor self, WithResupplyAnimationInfo info)
			: base(info)
		{
			wsb = self.TraitsImplementing<WithSpriteBody>().Single(w => w.Info.Name == Info.Body);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (!animPlaying
				&& ((repairing && Info.PlayAnimationOn.HasFlag(ResupplyType.Repair))
					|| (rearming && Info.PlayAnimationOn.HasFlag(ResupplyType.Rearm))))
			{
				wsb.PlayCustomAnimationRepeating(self, Info.Sequence);
				animPlaying = true;
			}
			else if (animPlaying
				&& (!repairing || !Info.PlayAnimationOn.HasFlag(ResupplyType.Repair))
				&& (!rearming || !Info.PlayAnimationOn.HasFlag(ResupplyType.Rearm)))
			{
				wsb.CancelCustomAnimation(self);
				animPlaying = false;
			}
		}

		void INotifyResupply.BeforeResupply(Actor self, Actor target, ResupplyType types)
		{
			repairing = types.HasFlag(ResupplyType.Repair);
			rearming = types.HasFlag(ResupplyType.Rearm);
		}

		void INotifyResupply.ResupplyTick(Actor self, Actor target, ResupplyType types)
		{
			repairing = types.HasFlag(ResupplyType.Repair);
			rearming = types.HasFlag(ResupplyType.Rearm);
		}

		protected override void TraitDisabled(Actor self)
		{
			// Cancel immediately instead of waiting for the next tick
			repairing = rearming = animPlaying = false;
			wsb.CancelCustomAnimation(self);
		}
	}
}
