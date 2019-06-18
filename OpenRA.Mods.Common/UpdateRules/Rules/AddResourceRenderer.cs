#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
		public override string Name
		{
			get { return "Add ResourceRenderer trait"; }
		}

		public override string Description
		{
			get { return "The rendering parts of ResourceLayer have been moved to a new trait"; }
		}

		readonly List<string> locations = new List<string>();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (locations.Any())
				yield return "[D2k]ResourceRenderer has been added.\n" +
					"You need to adjust the the field RenderTypes on trait [D2k]ResourceRenderer\n" +
					"on the following actors:\n" +
					UpdateUtils.FormatMessageList(locations);

			locations.Clear();
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			if (actorNode.ChildrenMatching("ResourceLayer").Any() && !actorNode.ChildrenMatching("ResourceRenderer").Any())
			{
				locations.Add("{0} ({1})".F(actorNode.Key, actorNode.Location.Filename));
				var resourceRenderer = new MiniYamlNode("ResourceRenderer", "");
				resourceRenderer.AddNode("RenderTypes", "");
				actorNode.AddNode(resourceRenderer);
			}

			if (actorNode.ChildrenMatching("D2kResourceLayer").Any() && !actorNode.ChildrenMatching("D2kResourceRenderer").Any())
			{
				actorNode.RenameChildrenMatching("D2kResourceLayer", "ResourceLayer");

				locations.Add("{0} ({1})".F(actorNode.Key, actorNode.Location.Filename));
				var resourceRenderer = new MiniYamlNode("D2kResourceRenderer", "");
				resourceRenderer.AddNode("RenderTypes", "");
				actorNode.AddNode(resourceRenderer);
			}

			yield break;
		}
	}
}
