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

using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;

namespace OpenRA.Mods.Common.Activities
{
	public class SpriteHarvesterDockSequence : HarvesterDockSequence
	{
		readonly WithSpriteBody wsb;
		readonly WithDockingAnimationInfo wda;
		protected bool dockAnimPlayed;

		public SpriteHarvesterDockSequence(Actor self, Actor refinery, WAngle dockAngle, bool isDragRequired, in WVec dragOffset, int dragLength)
			: base(self, refinery, dockAngle, isDragRequired, dragOffset, dragLength)
		{
			wsb = self.Trait<WithSpriteBody>();
			wda = self.Info.TraitInfoOrDefault<WithDockingAnimationInfo>();
		}

		public override void OnStateDock(Actor self)
		{
			foreach (var trait in self.TraitsImplementing<INotifyHarvesterAction>())
				trait.Docked();

			foreach (var nd in Refinery.TraitsImplementing<INotifyDocking>())
				nd.Docked(Refinery, self);

			if (wda != null)
				wsb.PlayCustomAnimation(self, wda.DockSequence, () => wsb.PlayCustomAnimationRepeating(self, wda.DockLoopSequence));

			dockAnimPlayed = true;
			dockingState = DockingState.Loop;
		}

		public override void OnStateUndock(Actor self)
		{
			// If dock animation hasn't played, we didn't actually dock and have to skip the undock anim and notification
			if (!dockAnimPlayed)
			{
				dockingState = DockingState.Complete;
				return;
			}

			dockingState = DockingState.Wait;

			if (wda == null)
				NotifyUndock(self);
			else
				wsb.PlayCustomAnimationBackwards(self, wda.DockSequence, () => NotifyUndock(self));
		}

		void NotifyUndock(Actor self)
		{
			dockingState = DockingState.Complete;
			foreach (var trait in self.TraitsImplementing<INotifyHarvesterAction>())
				trait.Undocked();

			if (Refinery.IsInWorld && !Refinery.IsDead)
				foreach (var nd in Refinery.TraitsImplementing<INotifyDocking>())
					nd.Undocked(Refinery, self);
		}
	}
}
