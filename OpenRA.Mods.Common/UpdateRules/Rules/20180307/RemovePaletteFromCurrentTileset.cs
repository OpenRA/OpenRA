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

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class RemovePaletteFromCurrentTileset : UpdateRule
	{
		public override string Name { get { return "Remove PaletteFromCurrentTileset trait"; } }
		public override string Description
		{
			get
			{
				return "The PaletteFromCurrentTileset trait and Palette field on TileSets have been removed.\n" +
					"Terrain palettes are now explicitly defined on the world actor.\n" +
					"Palette definitions are generated based on the Tileset metadata.";
			}
		}

		readonly Dictionary<string, string> tilesetPalettes = new Dictionary<string, string>();
		readonly List<Tuple<string, int[]>> paletteTraits = new List<Tuple<string, int[]>>();

		string BuildYaml(string palette, int[] shadow, string tileset, string filename)
		{
			return "PaletteFromFile@{0}:\n    Name: {1}\n    Tileset: {2}\n    Filename: {3}\n    ShadowIndex: {4}".F(
				palette + '-' + tileset.ToLower(), palette, tileset, filename, FieldSaver.FormatValue(shadow));
		}

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (tilesetPalettes.Any() && paletteTraits.Any())
				yield return "You must add the following to your palette definitions:\n"
					+ paletteTraits.Select(p => tilesetPalettes.Select(kv =>
						BuildYaml(p.Item1, p.Item2, kv.Key, kv.Value)).JoinWith("\n")).JoinWith("\n");

			paletteTraits.Clear();
			yield break;
		}

		public override IEnumerable<string> UpdateTilesetNode(ModData modData, MiniYamlNode tilesetNode)
		{
			if (tilesetNode.Key == "General")
			{
				var idNode = tilesetNode.LastChildMatching("Id");
				if (idNode == null)
					yield break;

				var paletteNode = tilesetNode.LastChildMatching("Palette");
				if (paletteNode != null)
					tilesetPalettes[idNode.Value.Value] = paletteNode.Value.Value;

				tilesetNode.RemoveNodes("Palette");
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var paletteNode in actorNode.ChildrenMatching("PaletteFromCurrentTileset"))
			{
				var name = "terrain";
				var shadow = new int[] { };

				var shadowNode = paletteNode.LastChildMatching("ShadowIndex");
				if (shadowNode != null)
					shadow = shadowNode.NodeValue<int[]>();

				var nameNode = paletteNode.LastChildMatching("Name");
				if (nameNode != null)
					name = nameNode.Value.Value;

				paletteTraits.Add(Tuple.Create(name, shadow));
			}

			actorNode.RemoveNodes("PaletteFromCurrentTileset");
			yield break;
		}
	}
}
