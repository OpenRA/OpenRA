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
				Widget.CloseWindow();
				switch( Game.orderManager.Connection.ConnectionState )
				{
					case ConnectionState.PreConnecting:
						Widget.OpenWindow("MAINMENU_BG");
						break;
					case ConnectionState.Connecting:
						Widget.OpenWindow("CONNECTING_BG");
						break;
					case ConnectionState.NotConnected:
						Widget.OpenWindow("CONNECTION_FAILED_BG");
						break;
					case ConnectionState.Connected:
						Widget.OpenWindow("SERVER_LOBBY");
						
						var lobby = r.GetWidget("SERVER_LOBBY");
						lobby.GetWidget<ChatDisplayWidget>("CHAT_DISPLAY").ClearChat();
						lobby.GetWidget("CHANGEMAP_BUTTON").Visible = true;
						lobby.GetWidget("LOCKTEAMS_CHECKBOX").Visible = true;
						lobby.GetWidget("DISCONNECT_BUTTON").Visible = true;
						r.GetWidget("INGAME_ROOT").GetWidget<ChatDisplayWidget>("CHAT_DISPLAY").ClearChat();	
						break;
				}
			};
		}
	}
}
