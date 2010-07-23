#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Network;

namespace OpenRA.Widgets.Delegates
{
	public class ConnectionDialogsDelegate : IWidgetDelegate
	{
		public ConnectionDialogsDelegate()
		{
			var r = Widget.RootWidget;
			r.GetWidget("CONNECTION_BUTTON_ABORT").OnMouseUp = mi => {
				r.GetWidget("CONNECTION_BUTTON_ABORT").Parent.Visible = false;
				Game.Disconnect();
				return true;
			};
			r.GetWidget("CONNECTION_BUTTON_CANCEL").OnMouseUp = mi => {
				r.GetWidget("CONNECTION_BUTTON_CANCEL").Parent.Visible = false;
				Game.Disconnect();
				return true;
			};
			r.GetWidget("CONNECTION_BUTTON_RETRY").OnMouseUp = mi => {
				Game.JoinServer(Game.CurrentHost, Game.CurrentPort);
				return true;
			};

			r.GetWidget<LabelWidget>("CONNECTING_DESC").GetText = () =>
				"Connecting to {0}:{1}...".F(Game.CurrentHost, Game.CurrentPort);

			r.GetWidget<LabelWidget>("CONNECTION_FAILED_DESC").GetText = () =>
				"Could not connect to {0}:{1}".F(Game.CurrentHost, Game.CurrentPort);
			
			Game.ConnectionStateChanged += () =>
			{
				r.CloseWindow();
				switch( Game.orderManager.Connection.ConnectionState )
				{
					case ConnectionState.PreConnecting:
						r.OpenWindow("MAINMENU_BG");
						break;
					case ConnectionState.Connecting:
						r.OpenWindow("CONNECTING_BG");
						break;
					case ConnectionState.NotConnected:
						r.OpenWindow("CONNECTION_FAILED_BG");
						break;
					case ConnectionState.Connected:
						r.OpenWindow("SERVER_LOBBY");
						r.GetWidget("SERVER_LOBBY").GetWidget<ChatDisplayWidget>("CHAT_DISPLAY").ClearChat();
						break;
				}
			};
		}
	}
}
