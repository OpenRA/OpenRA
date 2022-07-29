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

using OpenRA.Mods.Cnc.Traits.Render;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Cnc.Activities
{
	public class VoxelHarvesterDockSequence : HarvesterDockSequence
	{
		readonly WithVoxelUnloadBody body;
		readonly WithDockingOverlay spriteOverlay;

		public VoxelHarvesterDockSequence(Harvester self, IAcceptResources refinery, WAngle dockAngle, bool isDragRequired, in WVec dragOffset, int dragLength)
			: base(self, refinery, dockAngle, isDragRequired, dragOffset, dragLength)
		{
			body = self.Self.Trait<WithVoxelUnloadBody>();
			spriteOverlay = refinery.Self.TraitOrDefault<WithDockingOverlay>();
		}

		public override void OnStateDock(Actor self)
		{
			body.Docked = true;
			foreach (var trait in self.TraitsImplementing<INotifyHarvesterAction>())
				trait.Docked();
			foreach (var nd in Proc.Self.TraitsImplementing<INotifyDocking>())
				nd.Docked(Proc, self);

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
			if (!body.Docked)
				dockingState = DockingState.Complete;
			else if (Proc.IsAliveAndInWorld && spriteOverlay != null && !spriteOverlay.Visible)
			{
				dockingState = DockingState.Wait;
				spriteOverlay.Visible = true;
				spriteOverlay.WithOffset.Animation.PlayBackwardsThen(spriteOverlay.Info.Sequence, () =>
				{
					dockingState = DockingState.Complete;
					body.Docked = false;
					spriteOverlay.Visible = false;

					foreach (var trait in self.TraitsImplementing<INotifyHarvesterAction>())
						trait.Undocked();

					if (Proc.IsAliveAndInWorld)
						foreach (var nd in Proc.Self.TraitsImplementing<INotifyDocking>())
							nd.Undocked(Proc, self);
				});
			}
			else
			{
				dockingState = DockingState.Complete;
				body.Docked = false;

				foreach (var trait in self.TraitsImplementing<INotifyHarvesterAction>())
					trait.Undocked();

				if (Proc.IsAliveAndInWorld)
					foreach (var nd in Proc.Self.TraitsImplementing<INotifyDocking>())
						nd.Undocked(Proc, self);
			}
		}
	}
}
