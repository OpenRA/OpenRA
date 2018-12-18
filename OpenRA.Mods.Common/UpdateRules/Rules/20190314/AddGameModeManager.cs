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
using System.Linq;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class AddGameModeManager : UpdateRule
	{
		public override string Name { get { return "Add GameModeManager to Player actor."; } }
		public override string Description
		{
			get { return "A GameModeManager trait along with accompanying GameMode traits was added to the Player actor.";  }
		}

		List<string> removals = new List<string>();

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			if (actorNode.Key != "Player")
				yield break;

			var conquest = actorNode.LastChildMatching("ConquestVictoryConditions");
			if (conquest != null)
			{
				if (conquest.Key.StartsWith("-"))
				{
					actorNode.AddNode("-GameMode@conquest", null);
					removals.Add("{0} ({1})".F(actorNode.Key, conquest.Location.Filename));
				}
				else
				{
					conquest.AddNode(new MiniYamlNode("RequiresCondition", "conquest"));
					AddGameMode(actorNode, "conquest", "Conquest", "conquest");
				}
			}

			var koth = actorNode.LastChildMatching("StrategicVictoryConditions");
			if (koth != null)
			{
				if (koth.Key.StartsWith("-"))
				{
					actorNode.AddNode("-GameMode@koth", null);
					removals.Add("{0} ({1})".F(actorNode.Key, koth.Location.Filename));
				}
				else
				{
					koth.AddNode(new MiniYamlNode("RequiresCondition", "koth"));
					AddGameMode(actorNode, "koth", "King of the Hill", "koth");
				}
			}
		}

		void AddGameMode(MiniYamlNode actorNode, string intName, string name, string cond)
		{
			var node = new MiniYamlNode("GameMode@{0}".F(intName), "");
			node.AddNode("InternalName", intName);
			node.AddNode("Name", name);
			node.AddNode("Condition", cond);

			actorNode.AddNode(node);
		}

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (!removals.Any())
				yield break;

			yield return "Detected removals of *VictoryConditions traits.\n" +
				"Please review the following definitions, and if required\n" +
				"add an additional GameMode trait.\n" +
				UpdateUtils.FormatMessageList(removals, 1);
		}
	}
}
