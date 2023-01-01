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

using System;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class DirectConnectLogic : ChromeLogic
	{
		static readonly Action DoNothing = () => { };

		[ObjectCreator.UseCtor]
		public DirectConnectLogic(Widget widget, Action onExit, Action openLobby, ConnectionTarget directConnectEndPoint)
		{
			var panel = widget;
			var ipField = panel.Get<TextFieldWidget>("IP");
			var portField = panel.Get<TextFieldWidget>("PORT");

			var text = Game.Settings.Player.LastServer;
			var last = text.LastIndexOf(':');
			if (last < 0)
			{
				ipField.Text = "localhost";
				portField.Text = "1234";
			}
			else
			{
				ipField.Text = text.Substring(0, last);
				portField.Text = text.Substring(last + 1);
			}

			var joinButton = panel.Get<ButtonWidget>("JOIN_BUTTON");

			joinButton.IsDisabled = () => string.IsNullOrEmpty(ipField.Text);

			joinButton.OnClick = () =>
			{
				var port = Exts.WithDefault(1234, () => Exts.ParseIntegerInvariant(portField.Text));

				Game.Settings.Player.LastServer = $"{ipField.Text}:{port}";
				Game.Settings.Save();

				ConnectionLogic.Connect(new ConnectionTarget(ipField.Text, port), "", () => { Ui.CloseWindow(); openLobby(); }, DoNothing);
			};

			panel.Get<ButtonWidget>("BACK_BUTTON").OnClick = () => { Ui.CloseWindow(); onExit(); };

			if (directConnectEndPoint != null)
			{
				// The connection window must be opened at the end of the tick for the widget hierarchy to
				// work out, but we also want to prevent the server browser from flashing visible for one tick.
				widget.Visible = false;
				Game.RunAfterTick(() =>
				{
					ConnectionLogic.Connect(directConnectEndPoint, "", () => { Ui.CloseWindow(); openLobby(); }, DoNothing);
					widget.Visible = true;
				});
			}
		}
	}
}
