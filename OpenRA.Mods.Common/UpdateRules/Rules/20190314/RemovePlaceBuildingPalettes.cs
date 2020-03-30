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

using System.Collections.Generic;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class RemovePlaceBuildingPalettes : UpdateRule
	{
		public override string Name { get { return "Remove Palette and LineBuildSegmentPalette from PlaceBuilding"; } }
		public override string Description
		{
			get
			{
				return "The Palette and LineBuildSegmentPalette fields have been moved from PlaceBuilding,\n" +
					"to the *PlaceBuildingPreview traits.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			// Repairable isn't conditional or otherwise supports multiple traits, so LastChildMatching should be fine.
			foreach (var placeBuilding in actorNode.ChildrenMatching("PlaceBuilding"))
			{
				placeBuilding.RemoveNodes("Palette");
				placeBuilding.RemoveNodes("LineBuildSegmentPalette");
			}

			yield break;
		}
	}
}
