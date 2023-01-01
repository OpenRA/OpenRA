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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("Used by Mobile. Required for jumpjet actors. Attach these to the world actor. You can have multiple variants by adding @suffixes.")]
	public class JumpjetLocomotorInfo : LocomotorInfo
	{
		[Desc("Pathfinding cost for taking off or landing.")]
		public readonly short JumpjetTransitionCost = 0;

		[Desc("The terrain types that this actor can transition on. Leave empty to allow any.")]
		public readonly HashSet<string> JumpjetTransitionTerrainTypes = new HashSet<string>();

		[Desc("Can this actor transition on slopes?")]
		public readonly bool JumpjetTransitionOnRamps = true;

		public override bool DisableDomainPassabilityCheck => true;

		public override object Create(ActorInitializer init) { return new JumpjetLocomotor(init.Self, this); }
	}

	public class JumpjetLocomotor : Locomotor
	{
		public JumpjetLocomotor(Actor self, JumpjetLocomotorInfo info)
			: base(self, info) { }
	}
}
