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
	public class RenameWidgetSubstitutions : UpdateRule
	{
		public override string Name => "Rename *_RIGHT to *_WIDTH and *_BOTTOM to *_HEIGHT in integer expressions for widgets.";

		public override string Description =>
			"PARENT_RIGHT -> PARENT_WIDTH, PARENT_BOTTOM -> PARENT_HEIGHT, " +
			"WINDOW_RIGHT -> WINDOW_WIDTH, WINDOW_BOTTOM -> WINDOW_HEIGHT " +
			"in integer expressions for width, height and position.";

		public override IEnumerable<string> UpdateChromeNode(ModData modData, MiniYamlNodeBuilder chromeNode)
		{
			var dimensionFields =
				chromeNode.ChildrenMatching("Width")
				.Concat(chromeNode.ChildrenMatching("Height"))
				.Concat(chromeNode.ChildrenMatching("X"))
				.Concat(chromeNode.ChildrenMatching("Y")).ToArray();

			foreach (var field in dimensionFields)
			{
				field.ReplaceValue(field.Value.Value.Replace("PARENT_RIGHT", "PARENT_WIDTH"));
				field.ReplaceValue(field.Value.Value.Replace("PARENT_BOTTOM", "PARENT_HEIGHT"));
				field.ReplaceValue(field.Value.Value.Replace("WINDOW_RIGHT", "WINDOW_WIDTH"));
				field.ReplaceValue(field.Value.Value.Replace("WINDOW_BOTTOM", "WINDOW_HEIGHT"));
			}

			yield break;
		}
	}
}
