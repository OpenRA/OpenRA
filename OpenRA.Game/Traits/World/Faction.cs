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

using System.Collections.Generic;

namespace OpenRA.Traits
{
	[Desc("Attach this to the `World` actor.")]
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	public class FactionInfo : TraitInfo<Faction>
	{
		[Desc("This is the name exposed to the players.")]
		public readonly string Name = null;

		[Desc("This is the internal name for owner checks.")]
		public readonly string InternalName = null;

		[Desc("Pick a random faction as the player's faction out of this list.")]
		public readonly HashSet<string> RandomFactionMembers = new HashSet<string>();

		[Desc("The side that the faction belongs to. For example, England belongs to the 'Allies' side.")]
		public readonly string Side = null;

		public readonly string Description = null;

		public readonly bool Selectable = true;
	}

	public class Faction { /* we're only interested in the Info */ }
}
