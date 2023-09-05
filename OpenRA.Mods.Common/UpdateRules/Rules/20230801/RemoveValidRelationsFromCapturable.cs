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
	public class RemoveValidRelationsFromCapturable : UpdateRule
	{
		public override string Name => "Remove ValidRelations property from Capturable.";

		public override string Description => "ValidRelations has been moved from Capturable to Captures to match weapon definitions.";

		readonly List<string> locations = new();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (locations.Any())
				yield return Description + "\n" +
					"ValidRelations have been removed from:\n" +
					UpdateUtils.FormatMessageList(locations);

			locations.Clear();
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNodeBuilder actorNode)
		{
			foreach (var capturable in actorNode.ChildrenMatching("Capturable"))
			{
				if (capturable.RemoveNodes("ValidRelations") > 0)
					locations.Add($"{actorNode.Key}: {capturable.Key} ({actorNode.Location.Filename})");
			}

			yield break;
		}
	}
}
