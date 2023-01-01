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
	[Desc("Used by Mobile. Required for subterranean actors. Attach these to the world actor. You can have multiple variants by adding @suffixes.")]
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	public class SubterraneanLocomotorInfo : LocomotorInfo
	{
		[Desc("Pathfinding cost for submerging or reemerging.")]
		public readonly short SubterraneanTransitionCost = 0;

		[Desc("The terrain types that this actor can transition on. Leave empty to allow any.")]
		public readonly HashSet<string> SubterraneanTransitionTerrainTypes = new HashSet<string>();

		[Desc("Can this actor transition on slopes?")]
		public readonly bool SubterraneanTransitionOnRamps = false;

		[Desc("Depth at which the subterranean condition is applied.")]
		public readonly WDist SubterraneanTransitionDepth = new WDist(-1024);

		public override bool DisableDomainPassabilityCheck => true;

		public override object Create(ActorInitializer init) { return new SubterraneanLocomotor(init.Self, this); }
	}

	public class SubterraneanLocomotor : Locomotor
	{
		public SubterraneanLocomotor(Actor self, SubterraneanLocomotorInfo info)
			: base(self, info) { }
	}
}
