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
	public class TextNotificationsDisplayWidgetRemoveTime : UpdateRule
	{
		public override string Name => "Change name and unit of RemoveTime field of TextNotificationsDisplayWidget.";

		public override string Description =>
			"Change the field name from RemoveTime to DisplayDurationMs and convert the value from ticks to milliseconds";

		public override IEnumerable<string> UpdateChromeNode(ModData modData, MiniYamlNode chromeNode)
		{
			if (!chromeNode.KeyMatches("TextNotificationsDisplay"))
				yield break;

			foreach (var field in chromeNode.ChildrenMatching("RemoveTime"))
			{
				field.RenameKey("DisplayDurationMs");

				var durationMilliseconds = field.NodeValue<int>() * 40;
				field.ReplaceValue(FieldSaver.FormatValue(durationMilliseconds));
			}
		}
	}
}
