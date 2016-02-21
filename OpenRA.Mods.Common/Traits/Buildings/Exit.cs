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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Where the unit should leave the building. Multiples are allowed if IDs are added: Exit@2, ...")]
	public class ExitInfo : TraitInfo<Exit>, Requires<IOccupySpaceInfo>
	{
		[Desc("Offset at which that the exiting actor is spawned relative to the center of the producing actor.")]
		public readonly WVec SpawnOffset = WVec.Zero;

		[Desc("Cell offset where the exiting actor enters the ActorMap relative to the topleft cell of the producing actor.")]
		public readonly CVec ExitCell = CVec.Zero;
		public readonly int Facing = -1;

		[Desc("AttackMove to a RallyPoint or stay where you are spawned.")]
		public readonly bool MoveIntoWorld = true;

		[Desc("Number of ticks to wait before moving into the world.")]
		public readonly int ExitDelay = 0;
	}

	public class Exit { }
}
