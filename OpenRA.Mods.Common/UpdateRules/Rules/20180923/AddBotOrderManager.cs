#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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
	public class AddBotOrderManager : UpdateRule
	{
		public override string Name { get { return "Split bot order management from HackyAI to BotOrderManager"; } }
		public override string Description
		{
			get
			{
				return "The MinOrderQuotientPerTick property and all bot order handling have been moved from HackyAI\n" +
					"to the new BotOrderManager.";
			}
		}

		bool showMessage;
		bool messageShown;

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			var message = "You may want to manually change MinOrderQuotientPerTick on BotOrderManager,\n" +
				"if you were using a custom value on any AI.";

			if (showMessage && !messageShown)
				yield return message;

			messageShown = true;
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			if (actorNode.Key != "Player")
				yield break;

			var hackyAIs = actorNode.ChildrenMatching("HackyAI");
			if (!hackyAIs.Any())
				yield break;

			foreach (var hackyAINode in hackyAIs)
			{
				// We no longer support individual values for each AI,
				// and in practice the default of 5 has proven to be a solid middle-ground,
				// so just removing any custom value and notifying the modder about it should suffice.
				var minQuotient = hackyAINode.LastChildMatching("MinOrderQuotientPerTick");
				if (minQuotient != null)
				{
					hackyAINode.RemoveNode(minQuotient);
					if (!showMessage)
						showMessage = true;
				}
			}

			var botOrderManager = actorNode.LastChildMatching("BotOrderManager");
			if (botOrderManager == null)
			{
				var addBotOrderManager = new MiniYamlNode("BotOrderManager", "");
				actorNode.AddNode(addBotOrderManager);
			}

			yield break;
		}
	}
}
