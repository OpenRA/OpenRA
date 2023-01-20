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

using System;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	public class WithDockingAnimationInfo : TraitInfo, Requires<WithSpriteBodyInfo>, Requires<HarvesterInfo>
	{
		[SequenceReference]
		[Desc("Displayed when docking to refinery.")]
		public readonly string DockSequence = "dock";

		[SequenceReference]
		[Desc("Looped while unloading at refinery.")]
		public readonly string DockLoopSequence = "dock-loop";

		public override object Create(ActorInitializer init) { return new WithDockingAnimation(init.Self, this); }
	}

	public class WithDockingAnimation : IDockClientBody
	{
		readonly WithDockingAnimationInfo info;
		readonly WithSpriteBody wsb;

		public WithDockingAnimation(Actor self, WithDockingAnimationInfo info)
		{
			this.info = info;
			wsb = self.Trait<WithSpriteBody>();
		}

		void IDockClientBody.PlayDockAnimation(Actor self, Action after)
		{
			wsb.PlayCustomAnimation(self, info.DockSequence, () => { wsb.PlayCustomAnimationRepeating(self, info.DockLoopSequence); after(); });
		}

		void IDockClientBody.PlayReverseDockAnimation(Actor self, Action after)
		{
			wsb.PlayCustomAnimationBackwards(self, info.DockSequence, () => after());
		}
	}
}
