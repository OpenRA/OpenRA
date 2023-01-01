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
	[Desc("This actor requires another actor with 'GivesBuildableArea' trait around to be placed.")]
	public class RequiresBuildableAreaInfo : TraitInfo<RequiresBuildableArea>, Requires<BuildingInfo>
	{
		[FieldLoader.Require]
		[Desc("Types of buildable are this actor requires.")]
		public readonly HashSet<string> AreaTypes = new HashSet<string>();

		[Desc("Maximum range from the actor with 'GivesBuildableArea' this can be placed at.")]
		public readonly int Adjacent = 2;
	}

	public class RequiresBuildableArea { }
}
