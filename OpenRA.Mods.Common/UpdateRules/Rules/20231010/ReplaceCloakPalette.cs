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
using System.Linq;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class ReplaceCloakPalette : UpdateRule, IBeforeUpdateActors
	{
		public override string Name => "Change default Cloak style from Palette to Alpha.";

		public override string Description =>
			"Cloak has gained several new rendering modes\n" +
			"and its default behaviour has changed from using a palette to native alpha.";

		readonly List<(string, string)> actorsWithDefault = [];
		IEnumerable<string> IBeforeUpdateActors.BeforeUpdateActors(ModData modData, List<MiniYamlNodeBuilder> resolvedActors)
		{
			foreach (var actor in resolvedActors)
				foreach (var cloak in actor.ChildrenMatching("Cloak"))
					if (cloak.LastChildMatching("Palette", false) == null)
						actorsWithDefault.Add((actor.Key, cloak.Key));

			yield break;
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNodeBuilder actorNode)
		{
			foreach (var cloak in actorNode.ChildrenMatching("Cloak"))
			{
				if (actorsWithDefault.Any(pair => pair.Item1 == actorNode.Key && pair.Item2 == cloak.Key))
				{
					cloak.AddNode("CloakedPalette", "cloak");
					cloak.AddNode("CloakStyle", "Palette");
					yield break;
				}

				var palette = cloak.LastChildMatching("Palette", false);
				if (palette != null)
				{
					if (string.IsNullOrEmpty(palette.Value.Value))
					{
						cloak.RemoveNode(palette);
						cloak.AddNode("CloakStyle", "None");
					}
					else
					{
						palette.RenameKey("CloakedPalette");
						cloak.AddNode("CloakStyle", "Palette");
					}
				}
			}
		}
	}
}
