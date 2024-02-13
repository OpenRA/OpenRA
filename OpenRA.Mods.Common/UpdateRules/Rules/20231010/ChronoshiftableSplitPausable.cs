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
	public class ChronoshiftableSplitPausable : UpdateRule
	{
		public override string Name => "Chronoshiftable is now pausable.";

		public override string Description => "PauseOnCondition is now used to pause the return of Chronoshiftable units.";

		readonly List<string> locations = new();

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNodeBuilder actorNode)
		{
			foreach (var node in actorNode.ChildrenMatching("Chronoshiftable"))
				foreach (var subNode in node.ChildrenMatching("RequiresCondition").ToArray())
					if (!string.IsNullOrEmpty(subNode.Value.Value))
					{
						var value = subNode.Value.Value;
						var startsWithNotPrefix = value.StartsWith('!');
						var withoutNotPrefix = startsWithNotPrefix ? value[1..].TrimStart() : value;
						var complex = withoutNotPrefix.ToCharArray().Any(c => " ~!%^&*()+=[]{}|:;'\"<>?,/".Contains(c));
						if (complex)
							value = "!(" + value + ")";
						else if (startsWithNotPrefix)
							value = withoutNotPrefix;
						else
							value = "!" + value;
						node.AddNode("PauseOnCondition", value);
						locations.Add($"{actorNode.Key}: {node.Key} ({actorNode.Location.Name})");
					}

			yield break;
		}

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (locations.Count > 0)
				yield return
					"You must check the PauseOnCondition and RequiresCondition for the following traits :\n" +
					UpdateUtils.FormatMessageList(locations);

			locations.Clear();
		}
	}
}
