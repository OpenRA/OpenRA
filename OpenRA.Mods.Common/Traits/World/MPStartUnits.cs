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

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Used by SpawnMPUnits. Attach these to the world actor. You can have multiple variants by adding @suffixes.")]
	public class MPStartUnitsInfo : TraitInfo<MPStartUnits>
	{
		[Desc("Internal class ID.")]
		public readonly string Class = "none";

		[Desc("Exposed via the UI to the player.")]
		public readonly string ClassName = "Unlabeled";

		[Desc("Only available when selecting one of these factions.", "Leave empty for no restrictions.")]
		public readonly HashSet<string> Factions = new HashSet<string>();

		[Desc("The mobile construction vehicle.")]
		public readonly string BaseActor = null;

		[Desc("A group of units ready to defend or scout.")]
		public readonly string[] SupportActors = { };

		[Desc("Inner radius for spawning support actors")]
		public readonly int InnerSupportRadius = 2;

		[Desc("Outer radius for spawning support actors")]
		public readonly int OuterSupportRadius = 4;
	}

	public class MPStartUnits { }
}
