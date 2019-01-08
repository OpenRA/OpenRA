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
	public class RemoveTerrainTypeIsWaterFlag : UpdateRule
	{
		public override string Name { get { return "Remove TerrainType IsWater flag"; } }
		public override string Description
		{
			get
			{
				return "The IsWater flag on terrain type definitions has been unused for some time.\n" +
					"This flag has now been removed from the tileset yaml.";
			}
		}

		public override IEnumerable<string> UpdateTilesetNode(ModData modData, MiniYamlNode tilesetNode)
		{
			if (tilesetNode.Key == "Terrain")
				foreach (var type in tilesetNode.Value.Nodes)
					type.Value.Nodes.RemoveAll(n => n.Key == "IsWater");

			yield break;
		}
	}
}
