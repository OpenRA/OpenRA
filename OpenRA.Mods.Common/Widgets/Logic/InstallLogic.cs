#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
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
		public InstallLogic(Widget widget, Dictionary<string, string> installData, Action continueLoading)
		{
			var panel = widget.Get("INSTALL_PANEL");
			var args = new WidgetArgs()
			{
				{ "afterInstall", () => { Ui.CloseWindow(); continueLoading(); } },
				{ "installData", installData },
				{ "continueLoading", continueLoading }
			};

			panel.Get<ButtonWidget>("DOWNLOAD_BUTTON").OnClick = () =>
				Ui.OpenWindow("INSTALL_DOWNLOAD_PANEL", args);

			if (installData.ContainsKey("FilesToCopy") && !string.IsNullOrEmpty(installData["FilesToCopy"]) &&
				installData.ContainsKey("FilesToExtract") && !string.IsNullOrEmpty(installData["FilesToExtract"]))
			{
				args = new WidgetArgs(args)
				{
					{ "filesToCopy", installData["FilesToCopy"].Split(',') },
					{ "filesToExtract", installData["FilesToExtract"].Split(',') },
				};
			}
			panel.Get<ButtonWidget>("INSTALL_BUTTON").OnClick = () =>
				Ui.OpenWindow("INSTALL_FROMCD_PANEL", args);

			panel.Get<ButtonWidget>("BACK_BUTTON").OnClick = () =>
			{
				Game.Settings.Game.PreviousMod = Game.modData.Manifest.Mod.Id;
				Game.InitializeWithMod("modchooser", null);
			};
		}
	}
}
