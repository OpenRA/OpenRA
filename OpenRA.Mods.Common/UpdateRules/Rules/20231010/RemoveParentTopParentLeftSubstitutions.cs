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
	public class RemoveParentTopParentLeftSubstitutions : UpdateRule
	{
		public override string Name => "Remove PARENT_TOP and PARENT_LEFT from integer expressions for widgets.";

		public override string Description =>
			"PARENT_TOP is replaced with 0 and PARENT_LEFT is replaced with 0 in integer expressions for width, height and position.";

		public override IEnumerable<string> UpdateChromeNode(ModData modData, MiniYamlNodeBuilder chromeNode)
		{
			var dimensionFields =
				chromeNode.ChildrenMatching("Width")
				.Concat(chromeNode.ChildrenMatching("Height"))
				.Concat(chromeNode.ChildrenMatching("X"))
				.Concat(chromeNode.ChildrenMatching("Y")).ToArray();

			foreach (var field in dimensionFields)
			{
				if (field.Value.Value == "PARENT_TOP" || field.Value.Value == "PARENT_LEFT")
				{
					chromeNode.RemoveNode(field);
				}
				else if (field.Value.Value.Contains("PARENT_TOP"))
				{
					field.ReplaceValue(field.Value.Value.Replace("PARENT_TOP", "0"));
				}
				else if (field.Value.Value.Contains("PARENT_LEFT"))
				{
					field.ReplaceValue(field.Value.Value.Replace("PARENT_LEFT", "0"));
				}
			}

			yield break;
		}
	}
}
