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
	public class ConvertBoundsToWDist : UpdateRule
	{
		public override string Name => "Convert Interactable and Selection bounds from pixels to WDist.";

		public override string Description =>
			"The Bounds and DecorationBounds fields on Interactable and Selectable have been converted from pixels to WDist.\n" +
			"All bounds must be scaled by 1024 (Rectangular map grid) or 1448 (Isometric map grid) divided by your mod tile size.";

		readonly string[] traits = { "Interactable", "Selectable", "IsometricSelectable" };
		readonly string[] fields = { "Bounds", "DecorationBounds" };

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var grid = modData.Manifest.Get<MapGrid>();
			var tileSize = grid.TileSize;
			var tileScale = grid.Type == MapGridType.RectangularIsometric ? 1448 : 1024;

			foreach (var trait in traits)
			{
				foreach (var traitNode in actorNode.ChildrenMatching(trait))
				{
					foreach (var field in fields)
					{
						foreach (var fieldNode in traitNode.ChildrenMatching(field))
						{
							var value = fieldNode.NodeValue<int[]>();
							for (var i = 0; i < value.Length; i++)
								value[i] = value[i] * tileScale / (i % 2 == 1 ? tileSize.Height : tileSize.Width);

							fieldNode.ReplaceValue(FieldSaver.FormatValue(value));
						}
					}
				}
			}

			yield break;
		}
	}
}
