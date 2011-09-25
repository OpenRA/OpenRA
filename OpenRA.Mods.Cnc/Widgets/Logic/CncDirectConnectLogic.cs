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
using System.Linq;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class CncDirectConnectLogic
	{
		[ObjectCreator.UseCtor]
		public CncDirectConnectLogic([ObjectCreator.Param] Widget widget,
		                             [ObjectCreator.Param] Action onExit,
		                             [ObjectCreator.Param] Action openLobby)
		{
			var panel = widget.GetWidget("DIRECTCONNECT_PANEL");
			var ipField = panel.GetWidget<TextFieldWidget>("IP");
			var portField = panel.GetWidget<TextFieldWidget>("PORT");

			var last = Game.Settings.Player.LastServer.Split(':').ToArray();
			ipField.Text = last.Length > 1 ? last[0] : "localhost";
			portField.Text = last.Length > 2 ? last[1] : "1234";

			panel.GetWidget<ButtonWidget>("JOIN_BUTTON").OnClick = () =>
			{
				int port;
				if (!int.TryParse(portField.Text, out port))
					port = 1234;

				Game.Settings.Player.LastServer = "{0}:{1}".F(ipField.Text, port);
				Game.Settings.Save();

				Widget.CloseWindow();
				CncConnectingLogic.Connect(ipField.Text, port, openLobby, onExit);
			};

			panel.GetWidget<ButtonWidget>("BACK_BUTTON").OnClick = () => { Widget.CloseWindow(); onExit(); };
		}
	}
}
