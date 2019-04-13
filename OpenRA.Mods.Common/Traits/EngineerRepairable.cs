#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class EngineerRepairType { }

	[Desc("Eligible for instant repair.")]
	class EngineerRepairableInfo : TraitInfo<EngineerRepairable>
	{
		[Desc("Actors with these Types under EngineerRepair trait can repair me.")]
		public readonly BitSet<EngineerRepairType> Types = default(BitSet<EngineerRepairType>);
	}

	class EngineerRepairable { }
}
