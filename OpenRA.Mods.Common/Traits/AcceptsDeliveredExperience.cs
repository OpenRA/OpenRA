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
	[Desc("Tag trait for actors with `DeliversExperience`.")]
	public class AcceptsDeliveredExperienceInfo : TraitInfo, Requires<GainsExperienceInfo>
	{
		[Desc("Accepted `DeliversExperience` types. Leave empty to accept all types.")]
		public readonly HashSet<string> ValidTypes = new HashSet<string>();

		[Desc("Player relationships the owner of the delivering actor needs.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Ally;

		public override object Create(ActorInitializer init) { return new AcceptsDeliveredExperience(); }
	}

	public class AcceptsDeliveredExperience
	{
		public AcceptsDeliveredExperience() { }
	}
}
