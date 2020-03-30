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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Place a different building when PlaceBuilding's ToggleVariantKey hotkey is pressed while the PlaceBuildingOrderGenerator is active.")]
	public class PlaceBuildingVariantsInfo : TraitInfo<PlaceBuildingVariants>, Requires<BuildingInfo>, Requires<BuildableInfo>
	{
		[FieldLoader.Require]
		[ActorReference(typeof(BuildingInfo))]
		[Desc("Variant actors that can be cycled between when placing a structure.")]
		public readonly string[] Actors = null;

		public override object Create(ActorInitializer init) { return new PlaceBuildingVariants(); }
	}

	public class PlaceBuildingVariants { }
}
