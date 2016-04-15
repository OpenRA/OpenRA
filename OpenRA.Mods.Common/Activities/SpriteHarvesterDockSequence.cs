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

using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;

namespace OpenRA.Mods.Common.Activities
{
	public class SpriteHarvesterDockSequence : HarvesterDockSequence
	{
		readonly WithSpriteBody wsb;
		readonly WithDockingAnimationInfo wda;

		public SpriteHarvesterDockSequence(Actor self, Actor refinery, int dockAngle, bool isDragRequired, WVec dragOffset, int dragLength)
			: base(self, refinery, dockAngle, isDragRequired, dragOffset, dragLength)
		{
			wsb = self.Trait<WithSpriteBody>();
			wda = self.Info.TraitInfo<WithDockingAnimationInfo>();
		}

		public override Activity OnStateDock(Actor self)
		{
			foreach (var trait in self.TraitsImplementing<INotifyHarvesterAction>())
				trait.Docked();

			wsb.PlayCustomAnimation(self, wda.DockSequence, () => wsb.PlayCustomAnimationRepeating(self, wda.DockLoopSequence));
			dockingState = State.Loop;
			return this;
		}

		public override Activity OnStateUndock(Actor self)
		{
			wsb.PlayCustomAnimationBackwards(self, wda.DockSequence,
				() =>
				{
					dockingState = State.Complete;
					foreach (var trait in self.TraitsImplementing<INotifyHarvesterAction>())
						trait.Undocked();
				});
			dockingState = State.Wait;

			return this;
		}
	}
}