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

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class CncInstallLogic
	{
		[ObjectCreator.UseCtor]
		public CncInstallLogic([ObjectCreator.Param] Widget widget,
							   [ObjectCreator.Param] Dictionary<string,string> installData,
							   [ObjectCreator.Param] Action continueLoading)
		{
			var panel = widget.GetWidget("INSTALL_PANEL");
			var args = new WidgetArgs()
			{
				{ "afterInstall", () => { Widget.CloseWindow(); continueLoading(); } },
				{ "installData", installData }
			};

			panel.GetWidget<ButtonWidget>("DOWNLOAD_BUTTON").OnClick = () =>
				Widget.OpenWindow("INSTALL_DOWNLOAD_PANEL", args);

			panel.GetWidget<ButtonWidget>("INSTALL_BUTTON").OnClick = () =>
				Widget.OpenWindow("INSTALL_FROMCD_PANEL", new WidgetArgs(args)
				{
					{ "filesToCopy", new[] { "CONQUER.MIX", "DESERT.MIX", "SCORES.MIX",
											 "SOUNDS.MIX", "TEMPERAT.MIX", "WINTER.MIX" } },
					{ "filesToExtract", new[] { "speech.mix", "tempicnh.mix", "transit.mix" } },
				});

			panel.GetWidget<ButtonWidget>("QUIT_BUTTON").OnClick = Game.Exit;

			panel.GetWidget<ButtonWidget>("MODS_BUTTON").OnClick = () =>
			{
				Widget.OpenWindow("MODS_PANEL", new WidgetArgs()
				{
					{ "onExit", () => {} },
					// Close this panel
					{ "onSwitch", Widget.CloseWindow },
				});
			};
		}
	}
}
