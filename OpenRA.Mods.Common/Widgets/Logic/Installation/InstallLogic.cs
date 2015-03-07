#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class InstallLogic : Widget
	{
		[ObjectCreator.UseCtor]
		public InstallLogic(Widget widget, Action continueLoading)
		{
			var installData = Game.ModData.Manifest.Get<ContentInstaller>();
			var panel = widget.Get("INSTALL_PANEL");
			var widgetArgs = new WidgetArgs()
			{
				{ "afterInstall", () => { Ui.CloseWindow(); continueLoading(); } },
				{ "continueLoading", continueLoading },
				{ "mirrorListUrl", installData.PackageMirrorList },
			};

			panel.Get<ButtonWidget>("DOWNLOAD_BUTTON").OnClick = () =>
				Ui.OpenWindow("INSTALL_DOWNLOAD_PANEL", widgetArgs);

			panel.Get<ButtonWidget>("INSTALL_BUTTON").OnClick = () =>
				Ui.OpenWindow("INSTALL_FROMCD_PANEL", widgetArgs);

			panel.Get<ButtonWidget>("BACK_BUTTON").OnClick = () =>
			{
				Game.Settings.Game.PreviousMod = Game.ModData.Manifest.Mod.Id;
				Game.InitializeMod("modchooser", null);
			};
		}
	}
}
