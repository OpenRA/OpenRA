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

namespace OpenRA.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Checks for pause related desyncs. Attach this to the world actor.")]
	public class DebugPauseStateInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new DebugPauseState(init.World); }
	}

	public class DebugPauseState : ISync
	{
		readonly World world;
		[Sync]
		public bool Paused => world.Paused;

		public DebugPauseState(World world) { this.world = world; }
	}
}
