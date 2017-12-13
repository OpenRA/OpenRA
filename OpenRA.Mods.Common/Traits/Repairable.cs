#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
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
	public class RepairableInfo : ConditionalTraitInfo
	{
		[Desc("Actors that this actor can dock to and get repaired by.")]
		[FieldLoader.Require]
		[ActorReference] public readonly HashSet<string> RepairActors = new HashSet<string> { };

		public override object Create(ActorInitializer init) { return new Repairable(this); }
	}

	public class Repairable : ConditionalTrait<RepairableInfo>
	{
		public Repairable(RepairableInfo info) : base(info) { }
	}
}
