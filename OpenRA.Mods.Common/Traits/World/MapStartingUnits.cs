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

using System;
using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Used by SpawnStartingUnits. Attach these to the world actor. You can have multiple variants by adding @suffixes.")]
	public class StartingUnitsInfo : TraitInfo<StartingUnits>
	{
		[Desc("Internal class ID.")]
		public readonly string Class = "none";

		[TranslationReference]
		[Desc("Exposed via the UI to the player.")]
		public readonly string ClassName = "Unlabeled";

		[Desc("Only available when selecting one of these factions.", "Leave empty for no restrictions.")]
		public readonly HashSet<string> Factions = new HashSet<string>();

		[Desc("The actor at the center, usually the mobile construction vehicle.")]
		[ActorReference]
		public readonly string BaseActor = null;

		[Desc("Offset from the spawn point, BaseActor will spawn at.")]
		public readonly CVec BaseActorOffset = CVec.Zero;

		[Desc("A group of units ready to defend or scout.")]
		[ActorReference]
		public readonly string[] SupportActors = Array.Empty<string>();

		[Desc("Inner radius for spawning support actors")]
		public readonly int InnerSupportRadius = 2;

		[Desc("Outer radius for spawning support actors")]
		public readonly int OuterSupportRadius = 4;

		[Desc("Initial facing of BaseActor. Leave undefined for random facings.")]
		public readonly WAngle? BaseActorFacing = new WAngle(512);

		[Desc("Initial facing of SupportActors. Leave undefined for random facings.")]
		public readonly WAngle? SupportActorsFacing = null;
	}

	public class StartingUnits { }
}
