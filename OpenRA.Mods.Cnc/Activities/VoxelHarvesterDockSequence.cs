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

using OpenRA.Mods.Cnc.Traits.Render;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Cnc.Activities
{
	public class VoxelHarvesterDockSequence : HarvesterDockSequence
	{
		readonly WithDockingOverlay spriteOverlay;
		protected bool dockAnimPlayed;

		public VoxelHarvesterDockSequence(Actor self, Actor refineryActor, Refinery refinery)
			: base(self, refineryActor, refinery)
		{
			spriteOverlay = RefineryActor.TraitOrDefault<WithDockingOverlay>();
		}

		public override void OnStateDock(Actor self)
		{
			dockAnimPlayed = true;

			if (spriteOverlay != null && !spriteOverlay.Visible)
			{
				spriteOverlay.Visible = true;
				spriteOverlay.WithOffset.Animation.PlayThen(spriteOverlay.Info.Sequence, () =>
				{
					dockingState = DockingState.Loop;
					spriteOverlay.Visible = false;
				});
			}
			else
				dockingState = DockingState.Loop;
		}

		public override void OnStateUndock(Actor self)
		{
			// If body.Docked wasn't set, we didn't actually dock and have to skip the undock overlay
			if (!dockAnimPlayed)
				dockingState = DockingState.Complete;
			else if (Refinery.IsEnabled && spriteOverlay != null && !spriteOverlay.Visible)
			{
				dockingState = DockingState.Wait;
				spriteOverlay.Visible = true;
				spriteOverlay.WithOffset.Animation.PlayBackwardsThen(spriteOverlay.Info.Sequence, () =>
				{
					dockingState = DockingState.Complete;
					spriteOverlay.Visible = false;
				});
			}
			else
			{
				dockingState = DockingState.Complete;
			}
		}
	}
}
