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
	public class AddSupportPowerBlockedCursor : UpdateRule
	{
		public override string Name => "Add SpawnActorPower/GrantExternalConditionPower BlockedCursor.";

		public override string Description =>
			"SpawnActorPower and GrantExternalConditionPower field BlockedCursor changed its default value.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNodeBuilder actorNode)
		{
			foreach (var power in new List<string> { "SpawnActorPower", "GrantExternalConditionPower" })
			{
				foreach (var spawnActorPower in actorNode.ChildrenMatching(power))
				{
					var cursor = spawnActorPower.LastChildMatching("BlockedCursor");
					if (cursor != null && !cursor.IsRemoval())
						yield break;
					var blockedCursorNode = new MiniYamlNodeBuilder("BlockedCursor", new MiniYamlBuilder("move-blocked"));
					spawnActorPower.AddNode(blockedCursorNode);
				}
			}
		}
	}
}
