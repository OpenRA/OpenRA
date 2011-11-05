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
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class DirectConnectLogic
	{
		[ObjectCreator.UseCtor]
		public DirectConnectLogic(Widget widget, Action onExit, Action openLobby)
		{
			var panel = widget;
			var ipField = panel.GetWidget<TextFieldWidget>("IP");
			var portField = panel.GetWidget<TextFieldWidget>("PORT");

			var last = Game.Settings.Player.LastServer.Split(':');
			ipField.Text = last.Length > 1 ? last[0] : "localhost";
			portField.Text = last.Length > 2 ? last[1] : "1234";

			panel.GetWidget<ButtonWidget>("JOIN_BUTTON").OnClick = () =>
			{
				var port = Exts.WithDefault(1234, () => int.Parse(portField.Text));

				Game.Settings.Player.LastServer = "{0}:{1}".F(ipField.Text, port);
				Game.Settings.Save();

				Widget.CloseWindow();
				ConnectionLogic.Connect(ipField.Text, port, openLobby, onExit);
			};

			panel.GetWidget<ButtonWidget>("BACK_BUTTON").OnClick = () => { Widget.CloseWindow(); onExit(); };
		}
	}
}
