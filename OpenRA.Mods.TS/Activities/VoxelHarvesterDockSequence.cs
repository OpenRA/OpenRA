#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.TS.Traits;

namespace OpenRA.Mods.TS.Activities
{
	public class VoxelHarvesterDockSequence : HarvesterDockSequence
	{
		readonly WithVoxelUnloadBody body;

		public VoxelHarvesterDockSequence(Actor self, Actor refinery, int dockAngle, bool isDragRequired, WVec dragOffset, int dragLength)
			: base(self, refinery, dockAngle, isDragRequired, dragOffset, dragLength)
		{
			body = self.Trait<WithVoxelUnloadBody>();
		}

		public override Activity OnStateDock(Actor self)
		{
			body.Docked = true;
			dockingState = State.Loop;
			return this;
		}

		public override Activity OnStateUndock(Actor self)
		{
			body.Docked = false;
			dockingState = State.Complete;
			return this;
		}
	}
}