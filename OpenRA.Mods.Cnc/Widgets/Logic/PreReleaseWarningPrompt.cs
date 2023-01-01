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

using OpenRA.Mods.Common.Widgets;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class PreReleaseWarningPrompt : ChromeLogic
	{
		static bool promptAccepted;

		[ObjectCreator.UseCtor]
		public PreReleaseWarningPrompt(Widget widget, World world, ModData modData)
		{
			if (!promptAccepted && modData.Manifest.Metadata.Version != "{DEV_VERSION}")
				widget.Get<ButtonWidget>("CONTINUE_BUTTON").OnClick = () => ShowMainMenu(world);
			else
				ShowMainMenu(world);
		}

		void ShowMainMenu(World world)
		{
			promptAccepted = true;
			Ui.ResetAll();
			Game.LoadWidget(world, "MAINMENU", Ui.Root, new WidgetArgs());
		}
	}
}
