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

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class CncConnectingLogic
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
			else if (om.Connection.ConnectionState == ConnectionState.NotConnected)
			{
				// Show connection failed dialog
				CloseWindow();
				Widget.OpenWindow("CONNECTIONFAILED_PANEL", new WidgetArgs()
				{
					{ "onAbort", onAbort },
					{ "onRetry", onRetry },
					{ "host", host },
					{ "port", port }
				});
			}
		}

		public void CloseWindow()
		{
			Game.ConnectionStateChanged -= ConnectionStateChanged;
			Widget.CloseWindow();
		}

		[ObjectCreator.UseCtor]
		public CncConnectingLogic([ObjectCreator.Param] Widget widget,
								  [ObjectCreator.Param] string host,
								  [ObjectCreator.Param] int port,
								  [ObjectCreator.Param] Action onConnect,
								  [ObjectCreator.Param] Action onRetry,
								  [ObjectCreator.Param] Action onAbort)
		{
			this.host = host;
			this.port = port;
			this.onConnect = onConnect;
			this.onRetry = onRetry;
			this.onAbort = onAbort;

			Game.ConnectionStateChanged += ConnectionStateChanged;

			var panel = widget.GetWidget("CONNECTING_PANEL");
			panel.GetWidget<ButtonWidget>("ABORT_BUTTON").OnClick = () => { CloseWindow(); onAbort(); };

			widget.GetWidget<LabelWidget>("CONNECTING_DESC").GetText = () =>
				"Connecting to {0}:{1}...".F(host, port);
		}

		public static void Connect(string host, int port, Action onConnect, Action onAbort)
		{
			Game.JoinServer(host, port);
			Widget.OpenWindow("CONNECTING_PANEL", new WidgetArgs()
			{
				{ "host", host },
				{ "port", port },
				{ "onConnect", onConnect },
				{ "onAbort", onAbort },
				{ "onRetry", () => Connect(host, port, onConnect, onAbort) }
			});
		}
	}

	public class CncConnectionFailedLogic
	{
		[ObjectCreator.UseCtor]
		public CncConnectionFailedLogic([ObjectCreator.Param] Widget widget,
								  		[ObjectCreator.Param] string host,
										[ObjectCreator.Param] int port,
										[ObjectCreator.Param] Action onRetry,
								  		[ObjectCreator.Param] Action onAbort)
		{
			var panel = widget.GetWidget("CONNECTIONFAILED_PANEL");
			panel.GetWidget<ButtonWidget>("ABORT_BUTTON").OnClick = () => { Widget.CloseWindow(); onAbort(); };
			panel.GetWidget<ButtonWidget>("RETRY_BUTTON").OnClick = () => { Widget.CloseWindow(); onRetry(); };

			widget.GetWidget<LabelWidget>("CONNECTING_DESC").GetText = () =>
				"Could not connect to {0}:{1}".F(host, port);
		}
	}}
