#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
	public class SpriteDockSequence : DockSequence
	{
		readonly WithSpriteBody wsb;
		readonly WithDockingAnimationInfo wda;
		protected bool dockAnimPlayed;

		public SpriteDockSequence(DockManager dockManager, Dock dock)
			: base(dockManager, dock)
		{
			wsb = dockManager.Self.Trait<WithSpriteBody>();
			wda = dockManager.Self.Info.TraitInfoOrDefault<WithDockingAnimationInfo>();
		}

		public override void OnStateDock()
		{
			base.OnStateDock();

			if (wda != null)
				wsb.PlayCustomAnimation(DockManager.Self, wda.DockSequence, () => wsb.PlayCustomAnimationRepeating(DockManager.Self, wda.DockLoopSequence));

			dockAnimPlayed = true;
			dockingState = DockingState.Loop;
		}

		public override void OnStateUndock()
		{
			// If dock animation hasn't played, we didn't actually dock and have to skip the undock anim and notification
			if (!dockAnimPlayed)
			{
				base.OnStateUndock();
				return;
			}

			dockingState = DockingState.Wait;

			if (wda == null)
				base.OnStateUndock();
			else
				wsb.PlayCustomAnimationBackwards(DockManager.Self, wda.DockSequence, () => base.OnStateUndock());
		}
	}
}
