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

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class RemovePlaceBuildingPalette : UpdateRule
	{
		public override string Name => "*PlaceBuildingPreview palette overrides have been removed.";

		public override string Description =>
			"The palette overrides on the ActorPreviewPlaceBuildingPreview, FootprintPlaceBuildingPreview\n" +
			"SequencePlaceBuildingPreview, and D2kActorPreviewPlaceBuildingPreview traits have been removed.\n" +
			"New Alpha and LineBuildSegmentAlpha properties have been added in their place.";

		readonly List<string> locations = new List<string>();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (locations.Count > 0)
				yield return "The *Palette fields have been removed from the *PlaceBuildingPreview traits.\n" +
				             "You may wish to inspect the following definitions and define new Alpha or\n" +
				             "LineBuildSegmentAlpha properties as appropriate to recreate transparency effects:\n" +
				             UpdateUtils.FormatMessageList(locations);

			locations.Clear();
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var removed = 0;
			foreach (var node in actorNode.ChildrenMatching("ActorPreviewPlaceBuildingPreview"))
			{
				removed += node.RemoveNodes("OverridePalette");
				removed += node.RemoveNodes("OverridePaletteIsPlayerPalette");
				removed += node.RemoveNodes("LineBuildSegmentPalette");
			}

			foreach (var node in actorNode.ChildrenMatching("D2kActorPreviewPlaceBuildingPreview"))
			{
				removed += node.RemoveNodes("OverridePalette");
				removed += node.RemoveNodes("OverridePaletteIsPlayerPalette");
				removed += node.RemoveNodes("LineBuildSegmentPalette");
			}

			foreach (var node in actorNode.ChildrenMatching("FootprintPlaceBuildingPreview"))
				removed += node.RemoveNodes("LineBuildSegmentPalette");

			foreach (var node in actorNode.ChildrenMatching("SequencePlaceBuildingPreview"))
			{
				removed += node.RemoveNodes("SequencePalette");
				removed += node.RemoveNodes("SequencePaletteIsPlayerPalette");
				removed += node.RemoveNodes("LineBuildSegmentPalette");
			}

			if (removed > 0)
				locations.Add($"{actorNode.Key} ({actorNode.Location.Filename})");

			yield break;
		}
	}
}
