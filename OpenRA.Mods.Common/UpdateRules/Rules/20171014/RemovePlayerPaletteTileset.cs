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
	public class RemovePlayerPaletteTileset : UpdateRule
	{
		public override string Name { get { return "Replace 'PlayerPaletteFromCurrentTileset'"; } }
		public override string Description
		{
			get
			{
				return "The trait 'PlayerPaletteFromCurrentTileset' has been removed.\n" +
					"Use 'PaletteFromFile' with a Tileset filter.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var ppfct in actorNode.ChildrenMatching("PlayerPaletteFromCurrentTileset"))
			{
				ppfct.AddNode("Filename", "");
				ppfct.AddNode("Tileset", "");
				ppfct.RenameKey("PaletteFromFile");
				yield return ppfct.Location + ": The trait 'PlayerPaletteFromCurrentTileset'\n" +
					"has been replaced by 'PaletteFromFile'. The trait has been renamed for you,\n" +
					"but you will need to update the definition to specify the correct filename and tileset filters.";
			}

			yield break;
		}
	}
}
