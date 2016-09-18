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
	[Desc("Replaces the building animation when it rearms a unit.")]
	public class WithRearmAnimationInfo : ITraitInfo, Requires<WithSpriteBodyInfo>
	{
		[Desc("Sequence name to use")]
		[SequenceReference] public readonly string Sequence = "active";

		public readonly bool PauseOnLowPower = false;

		public object Create(ActorInitializer init) { return new WithRearmAnimation(init.Self, this); }
	}

	public class WithRearmAnimation : INotifyRearm, INotifyBuildComplete, INotifySold
	{
		readonly WithRearmAnimationInfo info;
		readonly WithSpriteBody spriteBody;
		bool buildComplete;

		public WithRearmAnimation(Actor self, WithRearmAnimationInfo info)
		{
			this.info = info;
			spriteBody = self.TraitOrDefault<WithSpriteBody>();
		}

		void INotifyRearm.Rearming(Actor self, Actor target)
		{
			if (buildComplete && spriteBody != null && !(info.PauseOnLowPower && self.IsDisabled()))
				spriteBody.PlayCustomAnimation(self, info.Sequence, () => spriteBody.CancelCustomAnimation(self));
		}

		public void BuildingComplete(Actor self)
		{
			buildComplete = true;
		}

		public void Selling(Actor self)
		{
			buildComplete = false;
		}

		public void Sold(Actor self) { }
	}
}