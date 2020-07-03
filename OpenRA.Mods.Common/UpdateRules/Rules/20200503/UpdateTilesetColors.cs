#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	class UpdateTilesetColors : UpdateRule
	{
		public override string Name { get { return "Rename Tileset LeftColor and RightColor"; } }
		public override string Description
		{
			get
			{
				return "The LeftColor and RightColor keys in tilesets have been renamed to MinColor and MaxColor to reflect their proper usage.";
			}
		}

		public override IEnumerable<string> UpdateTilesetNode(ModData modData, MiniYamlNode tilesetNode)
		{
			if (tilesetNode.Key == "Templates")
			{
				foreach (var templateNode in tilesetNode.Value.Nodes)
				{
					foreach (var tilesNode in templateNode.ChildrenMatching("Tiles"))
					{
						foreach (var node in tilesNode.Value.Nodes)
						{
							foreach (var leftNode in node.ChildrenMatching("LeftColor"))
								leftNode.RenameKey("MinColor");
							foreach (var leftNode in node.ChildrenMatching("RightColor"))
								leftNode.RenameKey("MaxColor");
						}
					}
				}
			}

			yield break;
		}
	}
}
