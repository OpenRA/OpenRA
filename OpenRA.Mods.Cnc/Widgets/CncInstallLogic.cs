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
using System.ComponentModel;
using System.IO;
using System.Net;
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Zip;
using OpenRA.FileFormats;
using OpenRA.Network;
using OpenRA.Widgets;
using System.Threading;
using OpenRA.Mods.RA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets
{
	public class CncInstallLogic : IWidgetDelegate
	{
		[ObjectCreator.UseCtor]
		public CncInstallLogic([ObjectCreator.Param] Widget widget,
		                       [ObjectCreator.Param] Dictionary<string,string> installData,
		                       [ObjectCreator.Param] Action continueLoading)
		{
				var panel = widget.GetWidget("INSTALL_PANEL");
				var args = new Dictionary<string, object>()
	            {
					{ "continueLoading", continueLoading },
					{ "installData", installData }
				};
				
				panel.GetWidget<CncMenuButtonWidget>("DOWNLOAD_BUTTON").OnClick = () =>
					Widget.OpenWindow("INSTALL_DOWNLOAD_PANEL", args);
				
				panel.GetWidget<CncMenuButtonWidget>("INSTALL_BUTTON").OnClick = () =>
					Widget.OpenWindow("INSTALL_FROMCD_PANEL", args);
				
				//panel.GetWidget<CncMenuButtonWidget>("MODS_BUTTON").OnClick = ShowModDialog; 
				panel.GetWidget<CncMenuButtonWidget>("QUIT_BUTTON").OnClick = Game.Exit;
		}
	}
}
