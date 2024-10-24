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
	public class WithDamageOverlayPropertyRename : UpdateRule, IBeforeUpdateActors
	{
		public override string Name => "Renamed `WithDamageOverlay`'s `IdleSequence` to `StartSequence`.";

		public override string Description => "WithDamageOverlay's property `IdleSequence` was renamed to `StartSequence`"
			+ " and functionality of WithDamageOverlay changed.";

		readonly List<(string, string)> hasIdleDefault = new();
		readonly List<(string, string)> hasEndDefault = new();

		public IEnumerable<string> BeforeUpdateActors(ModData modData, List<MiniYamlNodeBuilder> resolvedActors)
		{
			foreach (var actorNode in resolvedActors)
				foreach (var damage in actorNode.ChildrenMatching("WithDamageOverlay"))
					if (damage.LastChildMatching("IdleSequence", false) != null)
						hasIdleDefault.Add((actorNode.Key, damage.Key));

			foreach (var actorNode in resolvedActors)
				foreach (var damage in actorNode.ChildrenMatching("WithDamageOverlay"))
					if (damage.LastChildMatching("EndSequence", false) != null)
						hasEndDefault.Add((actorNode.Key, damage.Key));

			yield break;
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNodeBuilder actorNode)
		{
			foreach (var damage in actorNode.ChildrenMatching("WithDamageOverlay"))
			{
				var renamed = false;
				foreach (var start in damage.ChildrenMatching("IdleSequence"))
				{
					start.RenameKey("StartSequence");
					renamed = true;
				}

				if (!renamed && !hasIdleDefault.Contains((actorNode.Key, damage.Key)))
					damage.AddNode("StartSequence", "idle");

				damage.AddNode("LoopCount", 1);

				if (!hasEndDefault.Contains((actorNode.Key, damage.Key)))
					damage.AddNode("EndSequence", "end");
			}

			yield break;
		}
	}
}
