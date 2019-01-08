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
	public class RefactorResourceLevelAnimating : UpdateRule
	{
		public override string Name { get { return "Streamlined traits animating player resource level"; } }
		public override string Description
		{
			get
			{
				return "Replaced WithSiloAnimation with WithResourceLevelSpriteBody and\n" +
					"renamed WithResources to WithResourceLevelOverlay.";
			}
		}

		readonly List<string> locations = new List<string>();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (locations.Any())
				yield return "WithSiloAnimation has been replaced by WithResourceLevelSpriteBody.\n" +
					"You may need to disable/remove any previous (including inherited) *SpriteBody traits\n" +
					"on the following actors:\n" +
					UpdateUtils.FormatMessageList(locations);

			locations.Clear();
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var wr in actorNode.ChildrenMatching("WithResources"))
				wr.RenameKey("WithResourceLevelOverlay");

			var siloAnims = actorNode.ChildrenMatching("WithSiloAnimation");
			foreach (var sa in siloAnims)
			{
				// If it's a trait removal, we only rename it.
				if (sa.IsRemoval())
				{
					sa.RenameKey("WithResourceLevelSpriteBody");
					continue;
				}

				var sequence = sa.LastChildMatching("Sequence");
				var body = sa.LastChildMatching("Body");

				if (sequence == null)
				{
					var newSequenceNode = new MiniYamlNode("Sequence", "stages");
					sa.AddNode(newSequenceNode);
				}

				if (body != null)
					sa.RemoveNode(body);

				sa.RenameKey("WithResourceLevelSpriteBody");
				locations.Add("{0} ({1})".F(actorNode.Key, sa.Location.Filename));
			}

			yield break;
		}
	}
}
