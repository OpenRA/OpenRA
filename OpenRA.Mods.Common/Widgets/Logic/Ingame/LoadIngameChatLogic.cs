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
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class LoadIngameChatLogic : ChromeLogic
	{
		public class LoadIngameChatLogicDynamicWidgets : DynamicWidgets
		{
			public override ISet<string> WindowWidgetIds { get; } = EmptySet;
			public override IReadOnlyDictionary<string, string> ParentWidgetIdForChildWidgetId { get; } =
				new Dictionary<string, string>
				{
					{ "CHAT_PANEL", "CHAT_ROOT" },
				};
		}

		readonly LoadIngameChatLogicDynamicWidgets dynamicWidgets = new();

		[ObjectCreator.UseCtor]
		public LoadIngameChatLogic(Widget widget, World world)
		{
			dynamicWidgets.LoadWidget(widget, "CHAT_PANEL", new WidgetArgs() { { "isMenuChat", false } });
		}
	}
}
