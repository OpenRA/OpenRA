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
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class SpriteHarvesterDockSequence : HarvesterDockSequence
	{
		readonly RenderUnit ru;

		public SpriteHarvesterDockSequence(Actor self, Actor refinery, int dockAngle, bool isDragRequired, WVec dragOffset, int dragLength)
			: base(self, refinery, dockAngle, isDragRequired, dragOffset, dragLength)
		{
			ru = self.Trait<RenderUnit>();
		}

		public override Activity OnStateDock(Actor self)
		{
			ru.PlayCustomAnimation(self, "dock", () => ru.PlayCustomAnimRepeating(self, "dock-loop"));
			dockingState = State.Loop;
			return this;
		}

		public override Activity OnStateUndock(Actor self)
		{
			ru.PlayCustomAnimBackwards(self, "dock", () => dockingState = State.Complete);
			dockingState = State.Wait;
			return this;
		}
	}
}