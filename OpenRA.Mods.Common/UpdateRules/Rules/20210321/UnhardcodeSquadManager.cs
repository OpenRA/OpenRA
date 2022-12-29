#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
	public class UnhardcodeSquadManager : UpdateRule
	{
		bool anyAdded;

		public override string Name => "SquadManagerBotModule got new fields to configure ground attacks and defensive actions.";

		public override string Description => "AirUnitsTypes and ProtectionTypes were added.";

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (anyAdded)
				yield return "Two new fields were added to SquadManagerBotModule - AirUnitsTypes and ProtectionTypes.\n" +
				             "Add any relevant attack aircraft or any actors worth defending there.";

			anyAdded = false;
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var squadManager in actorNode.ChildrenMatching("SquadManagerBotModule"))
			{
				squadManager.AddNode(new MiniYamlNode("AirUnitsTypes", ""));
				squadManager.AddNode(new MiniYamlNode("ProtectionTypes", ""));
				anyAdded = true;
			}

			yield break;
		}
	}
}
