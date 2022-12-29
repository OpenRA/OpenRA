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
	public class UnhardcodeBaseBuilderBotModule : UpdateRule
	{
		bool anyAdded;

		public override string Name => "BaseBuilderBotModule got new fields to configure buildings that are defenses.";

		public override string Description => "DefenseTypes were added.";

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (anyAdded)
				yield return "A new field was added to BaseBuilderBotModule - DefenseTypes.\n" +
				             "Add any relevant base defense structures there.";

			anyAdded = false;
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var squadManager in actorNode.ChildrenMatching("BaseBuilderBotModule"))
			{
				squadManager.AddNode(new MiniYamlNode("DefenseTypes", ""));
				anyAdded = true;
			}

			yield break;
		}
	}
}
