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
	public class RenameSpins : UpdateRule
	{
		public override string Name => "FallsToEarth.Spins has been refactored to MaximumSpinSpeed.";

		public override string Description => "The FallsToEarth.Spins property has been refactored to MaximumSpinSpeed.";

		readonly List<string> locations = new List<string>();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (locations.Count > 0)
				yield return "The Spins property has been refactored to MaximumSpinSpeed.\n" +
				             "MaximumSpinSpeed defaults to 'unlimited', while disabling is done by setting it to 0.\n" +
				             "You may want to set a custom MaximumSpinSpeed limiting value in the following places:\n" +
				             UpdateUtils.FormatMessageList(locations);

			locations.Clear();
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var fallsToEarth in actorNode.ChildrenMatching("FallsToEarth"))
			{
				var spinsNode = fallsToEarth.LastChildMatching("Spins");
				if (spinsNode != null)
				{
					var spins = spinsNode.NodeValue<bool>();
					if (!spins)
						fallsToEarth.AddNode("MaximumSpinSpeed", "0");

					fallsToEarth.RemoveNode(spinsNode);
					locations.Add($"{actorNode.Key} ({actorNode.Location.Filename})");
				}
			}

			yield break;
		}
	}
}
