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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("How much the unit is worth.")]
	public class ValuedInfo : TraitInfo<Valued>
	{
		[FieldLoader.Require]
		[Desc("Used in production, but also for bounties so remember to set it > 0 even for NPCs.")]
		public readonly int Cost = 0;
	}

	public class Valued { }
}
