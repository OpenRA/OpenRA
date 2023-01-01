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

using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Traits
{
	public class EngineerRepairType { }

	[Desc("Eligible for instant repair.")]
	public class EngineerRepairableInfo : ConditionalTraitInfo
	{
		[Desc("Actors with these Types under EngineerRepair trait can repair me.")]
		public readonly BitSet<EngineerRepairType> Types = default;

		public override object Create(ActorInitializer init) { return new EngineerRepairable(this); }
	}

	public class EngineerRepairable : ConditionalTrait<EngineerRepairableInfo>
	{
		public EngineerRepairable(EngineerRepairableInfo info)
			: base(info) { }
	}
}
