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
	public class ProductionTabsWidgetAddTabButtonCollection : UpdateRule
	{
		public override string Name => "Change name of Button field of ProductionTabsWidget and add ArrowButton if necessary.";

		public override string Description =>
			"Change the field name from Button to TabButton and add ArrowButton, if Button field was set.";

		public override IEnumerable<string> UpdateChromeNode(ModData modData, MiniYamlNode chromeNode)
		{
			if (!chromeNode.KeyMatches("ProductionTabs"))
				yield break;

			string buttonCollection = null;
			foreach (var field in chromeNode.ChildrenMatching("Button"))
			{
				field.RenameKey("TabButton");
				buttonCollection = field.Value.Value;
			}

			if (buttonCollection != null)
				chromeNode.AddNode(new MiniYamlNode("ArrowButton", buttonCollection));
		}
	}
}
