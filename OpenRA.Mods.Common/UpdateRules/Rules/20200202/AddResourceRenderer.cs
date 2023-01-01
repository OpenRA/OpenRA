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
	public class AddResourceRenderer : UpdateRule
	{
		public override string Name => "Add ResourceRenderer trait";

		public override string Description => "The rendering parts of ResourceLayer have been moved to a new trait";

		readonly List<string> locations = new List<string>();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (locations.Count > 0)
				yield return "[D2k]ResourceRenderer has been added.\n" +
					"You need to adjust the field RenderTypes on trait [D2k]ResourceRenderer\n" +
					"on the following actors:\n" +
					UpdateUtils.FormatMessageList(locations);

			locations.Clear();
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			if (actorNode.ChildrenMatching("ResourceLayer").Any() && !actorNode.ChildrenMatching("ResourceRenderer").Any())
			{
				locations.Add($"{actorNode.Key} ({actorNode.Location.Filename})");
				var resourceRenderer = new MiniYamlNode("ResourceRenderer", "");
				resourceRenderer.AddNode("RenderTypes", "");
				actorNode.AddNode(resourceRenderer);
			}

			if (actorNode.ChildrenMatching("D2kResourceLayer").Any() && !actorNode.ChildrenMatching("D2kResourceRenderer").Any())
			{
				actorNode.RenameChildrenMatching("D2kResourceLayer", "ResourceLayer");

				locations.Add($"{actorNode.Key} ({actorNode.Location.Filename})");
				var resourceRenderer = new MiniYamlNode("D2kResourceRenderer", "");
				resourceRenderer.AddNode("RenderTypes", "");
				actorNode.AddNode(resourceRenderer);
			}

			yield break;
		}
	}
}
