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
	public class ReplaceCloakPalette : UpdateRule
	{
		public override string Name => "Change default Cloak style from Palette to Alpha.";

		public override string Description =>
			"Cloak has gained several new rendering modes\n" +
			"and its default behaviour has changed from using a palette to native alpha.";

		readonly List<string> locations = new();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (locations.Count > 0)
				yield return "Cloak no longer defaults to replacing the actor's palette.\n" +
					"If you wish to keep the previous behavior you wish to change the\n" +
					"Cloak definitions on the following actor definitions:\n" +
					UpdateUtils.FormatMessageList(locations);

			locations.Clear();
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNodeBuilder actorNode)
		{
			foreach (var cloak in actorNode.ChildrenMatching("Cloak"))
			{
				cloak.RemoveNodes("Palette");
				cloak.RemoveNodes("IsPlayerPalette");
				locations.Add($"{actorNode.Key} ({actorNode.Location.Filename})");
			}

			yield break;
		}
	}
}
