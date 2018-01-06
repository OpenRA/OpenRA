#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class DirectConnectLogic : ChromeLogic
	{
		static readonly Action DoNothing = () => { };

		[ObjectCreator.UseCtor]
		public DirectConnectLogic(Widget widget, Action onExit, Action openLobby, string directConnectHost, int directConnectPort)
		{
			var panel = widget;
			var ipField = panel.Get<TextFieldWidget>("IP");
			var portField = panel.Get<TextFieldWidget>("PORT");

			var last = Game.Settings.Player.LastServer.Split(':');
			ipField.Text = last.Length > 1 ? last[0] : "localhost";
			portField.Text = last.Length == 2 ? last[1] : "1234";

			panel.Get<ButtonWidget>("JOIN_BUTTON").OnClick = () =>
			{
				var port = Exts.WithDefault(1234, () => Exts.ParseIntegerInvariant(portField.Text));

				Game.Settings.Player.LastServer = "{0}:{1}".F(ipField.Text, port);
				Game.Settings.Save();

				ConnectionLogic.Connect(ipField.Text, port, "", () => { Ui.CloseWindow(); openLobby(); }, DoNothing);
			};

			panel.Get<ButtonWidget>("BACK_BUTTON").OnClick = () => { Ui.CloseWindow(); onExit(); };

			if (directConnectHost != null)
			{
				// The connection window must be opened at the end of the tick for the widget hierarchy to
				// work out, but we also want to prevent the server browser from flashing visible for one tick.
				widget.Visible = false;
				Game.RunAfterTick(() =>
				{
					ConnectionLogic.Connect(directConnectHost, directConnectPort, "", () => { Ui.CloseWindow(); openLobby(); }, DoNothing);
					widget.Visible = true;
				});
			}
		}
	}
}
