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

using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class ReplaceWithColoredOverlayPalette : UpdateRule
	{
		public override string Name { get { return "WithColoredOverlay Palette changed to Color."; } }

		public override string Description
		{
			get
			{
				return "The Palette field has been removed from WithColoredOverlay. You must now specify the Color directly.";
			}
		}

		readonly List<string> locations = new List<string>();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (locations.Any())
				yield return "You must define new Color fields on the following traits:\n" +
				             UpdateUtils.FormatMessageList(locations);

			locations.Clear();
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var node in actorNode.ChildrenMatching("WithColoredOverlay"))
				if (node.RemoveNodes("Palette") > 0)
					locations.Add("{0}: {1} ({2})".F(actorNode.Key, node.Key, actorNode.Location.Filename));

			yield break;
		}
	}
}
