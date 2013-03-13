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
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class ConnectionLogic
	{
		Action onConnect, onRetry, onAbort;
		string host;
		int port;

		void ConnectionStateChanged(OrderManager om)
		{
			if (om.Connection.ConnectionState == ConnectionState.Connected)
			{
				CloseWindow();
				onConnect();
			}
			else if (om.Connection.ConnectionDropState == ConnectionDropState.VersionMismatch)
			{
				// Show connection version mismatch failed dialog
				CloseWindow();
				Ui.OpenWindow("CONNECTIONDROPSTATEVERSIONMISMATCH_PANEL", new WidgetArgs()
				{
					{ "onAbort", onAbort },
					{ "errorMessage", "Your game version is not matching with the server." }
				});
			}
			else if (om.Connection.ConnectionState == ConnectionState.NotConnected)
			{
				// Show connection failed dialog
				CloseWindow();
				Ui.OpenWindow("CONNECTIONFAILED_PANEL", new WidgetArgs()
				{
					{ "onAbort", onAbort },
					{ "onRetry", onRetry },
					{ "host", host },
					{ "port", port }
				});
			}
		}

		void CloseWindow()
		{
			Game.ConnectionStateChanged -= ConnectionStateChanged;
			Ui.CloseWindow();
		}

		[ObjectCreator.UseCtor]
		public ConnectionLogic(Widget widget, string host, int port, Action onConnect, Action onRetry, Action onAbort)
		{
			this.host = host;
			this.port = port;
			this.onConnect = onConnect;
			this.onRetry = onRetry;
			this.onAbort = onAbort;

			Game.ConnectionStateChanged += ConnectionStateChanged;

			var panel = widget;
			panel.Get<ButtonWidget>("ABORT_BUTTON").OnClick = () => { CloseWindow(); onAbort(); };

			widget.Get<LabelWidget>("CONNECTING_DESC").GetText = () =>
				"Connecting to {0}:{1}...".F(host, port);
		}

		public static void Connect(string host, int port, Action onConnect, Action onAbort)
		{
			Game.JoinServer(host, port);
			Ui.OpenWindow("CONNECTING_PANEL", new WidgetArgs()
			{
				{ "host", host },
				{ "port", port },
				{ "onConnect", onConnect },
				{ "onAbort", onAbort },
				{ "onRetry", () => Connect(host, port, onConnect, onAbort) }
			});
		}
	}

	public class ConnectionFailedLogic
	{
		[ObjectCreator.UseCtor]
		public ConnectionFailedLogic(Widget widget, string host, int port, Action onRetry, Action onAbort)
		{
			var panel = widget;
			panel.Get<ButtonWidget>("ABORT_BUTTON").OnClick = () => { Ui.CloseWindow(); onAbort(); };
			panel.Get<ButtonWidget>("RETRY_BUTTON").OnClick = () => { Ui.CloseWindow(); onRetry(); };

			widget.Get<LabelWidget>("CONNECTING_DESC").GetText = () =>
				"Could not connect to {0}:{1}".F(host, port);
		}
	}

	public class ConnectionDroppedLogic
	{
		[ObjectCreator.UseCtor]
		public ConnectionDroppedLogic(Widget widget, string errorMessage,  Action onAbort)
		{
			var panel = widget;
			panel.Get<ButtonWidget>("ABORT_BUTTON").OnClick = () => { Ui.CloseWindow(); onAbort(); };

			widget.Get<LabelWidget>("CONNECTING_DESC").GetText = () =>
			   errorMessage;
		}
	}
}