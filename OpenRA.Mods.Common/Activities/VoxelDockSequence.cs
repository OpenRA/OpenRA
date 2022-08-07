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
	public class VoxelDockSequence : DockSequence
	{
		readonly WithVoxelUnloadBody body;
		readonly WithDockingOverlay spriteOverlay;

		public VoxelDockSequence(DockManager dockManager, Dock dock)
			: base(dockManager, dock)
		{
			body = dockManager.Self.Trait<WithVoxelUnloadBody>();
			spriteOverlay = dock.Self.TraitOrDefault<WithDockingOverlay>();
		}

		public override void OnStateDock()
		{
			base.OnStateDock();

			body.Docked = true;

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

		public override void OnStateUndock()
		{
			// If body.Docked wasn't set, we didn't actually dock and have to skip the undock overlay
			if (!body.Docked)
				base.OnStateUndock();
			else if (Dock.IsAliveAndInWorld && spriteOverlay != null && !spriteOverlay.Visible)
			{
				dockingState = DockingState.Wait;
				spriteOverlay.Visible = true;
				spriteOverlay.WithOffset.Animation.PlayBackwardsThen(spriteOverlay.Info.Sequence, () =>
				{
					body.Docked = false;
					spriteOverlay.Visible = false;

					base.OnStateUndock();
				});
			}
			else
			{
				body.Docked = false;
				base.OnStateUndock();
			}
		}
	}
}
