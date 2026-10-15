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
	public class MoveTileSizeToTileSets : UpdateRule
	{
		public override string Name => "Move MapGrid.TileSize to the tileset definitions.";

		public override string Description =>
			"The TileSize property was moved from the MapGrid section of mod.yaml to the General section of each tileset.";

		string tileSize;
		bool copied;
		bool reported;

		public override IEnumerable<string> BeforeUpdate(ModData modData)
		{
			tileSize = null;
			if (modData.Manifest.GlobalModData.TryGetValue("MapGrid", out var mapGrid))
				tileSize = mapGrid.NodeWithKeyOrDefault("TileSize")?.Value.Value;

			yield break;
		}

		public override IEnumerable<string> UpdateTilesetNode(ModData modData, MiniYamlNodeBuilder tilesetNode)
		{
			if (tileSize != null && tilesetNode.Key == "General" && tilesetNode.LastChildMatching("TileSize") == null)
			{
				tilesetNode.AddNode("TileSize", tileSize);
				copied = true;
			}

			yield break;
		}

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (tileSize == null || reported)
				yield break;

			reported = true;
			if (copied)
				yield return "TileSize has been copied from the MapGrid section of mod.yaml to the tileset definitions.\n" +
					"Remove TileSize from the MapGrid section of mod.yaml.";
			else
				yield return "TileSize is now defined by the tileset definitions. Remove TileSize from the MapGrid section of mod.yaml.";
		}
	}
}
