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
using OpenRA.Mods.Common.Widgets;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class PreReleaseWarningPrompt : ChromeLogic
	{
		static bool promptAccepted;

		public class PreReleaseWarningPromptDynamicWidgets : DynamicWidgets
		{
			public override ISet<string> WindowWidgetIds { get; } = EmptySet;
			public override IReadOnlyDictionary<string, string> ParentWidgetIdForChildWidgetId { get; } = EmptyDictionary;
			public override IReadOnlyDictionary<string, string> OutOfTreeParentWidgetIdForChildWidgetId =>
				new Dictionary<string, string>
				{
					{ "MAINMENU", "" },
				};
		}

		readonly PreReleaseWarningPromptDynamicWidgets dynamicWidgets = new();

		[ObjectCreator.UseCtor]
		public PreReleaseWarningPrompt(Widget widget, ModData modData)
		{
			if (!promptAccepted && modData.Manifest.Metadata.Version != "{DEV_VERSION}")
				widget.Get<ButtonWidget>("CONTINUE_BUTTON").OnClick = ShowMainMenu;
			else
				ShowMainMenu();
		}

		void ShowMainMenu()
		{
			promptAccepted = true;
			Ui.ResetAll();
			dynamicWidgets.LoadWidgetOutOfTree(Ui.Root, "MAINMENU", new WidgetArgs());
		}
	}
}
