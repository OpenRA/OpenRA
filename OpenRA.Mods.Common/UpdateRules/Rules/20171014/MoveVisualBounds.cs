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

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class MoveVisualBounds : UpdateRule
	{
		public override string Name { get { return "Move 'SelectionDecorations.VisualBounds' to 'Selectable.Bounds'"; } }
		public override string Description
		{
			get
			{
				return "'SelectionDecorations.VisualBounds' was moved to 'Selectable.Bounds'.\n" +
					"'AutoRenderSize' and 'CustomRenderSize' were renamed to 'Interactable'.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var autoSelectionSize = actorNode.LastChildMatching("AutoSelectionSize");
			if (autoSelectionSize != null)
				actorNode.AddNode("Interactable", "");

			var customSelectionSize = actorNode.LastChildMatching("CustomSelectionSize");
			if (customSelectionSize != null)
			{
				var bounds = customSelectionSize.LastChildMatching("CustomBounds");
				var customRenderSize = new MiniYamlNode("Interactable", "");
				if (bounds != null)
					customRenderSize.AddNode("Bounds", bounds.NodeValue<int[]>());

				actorNode.AddNode(customRenderSize);
			}

			var sd = actorNode.LastChildMatching("SelectionDecorations");
			if (sd != null)
			{
				var boundsNode = sd.LastChildMatching("VisualBounds");
				if (boundsNode != null)
				{
					boundsNode.RenameKey("DecorationBounds");
					sd.RemoveNode(boundsNode);
					var selectable = actorNode.LastChildMatching("Selectable");
					if (selectable == null)
					{
						selectable = new MiniYamlNode("Selectable", new MiniYaml(""));
						actorNode.AddNode(selectable);
					}

					selectable.AddNode(boundsNode);
				}
			}

			if (actorNode.LastChildMatching("-Selectable") != null && actorNode.LastChildMatching("Interactable") == null)
				actorNode.AddNode("Interactable", "");

			actorNode.RemoveNodes("CustomSelectionSize");
			actorNode.RemoveNodes("AutoSelectionSize");
			yield break;
		}
	}
}
