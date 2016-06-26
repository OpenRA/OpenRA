#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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
	public class ConnectionLogic : ChromeLogic
	{
		Action onConnect, onAbort;
		Action<string> onRetry;

		void ConnectionStateChanged(OrderManager om)
		{
			if (om.Connection.ConnectionState == ConnectionState.Connected)
			{
				CloseWindow();
				onConnect();
			}
			else if (om.Connection.ConnectionState == ConnectionState.NotConnected)
			{
				CloseWindow();
				Ui.OpenWindow("CONNECTIONFAILED_PANEL", new WidgetArgs()
				{
					{ "orderManager", om },
					{ "onAbort", onAbort },
					{ "onRetry", onRetry }
				});
			}
		}

		void CloseWindow()
		{
			Game.ConnectionStateChanged -= ConnectionStateChanged;
			Ui.CloseWindow();
		}

		[ObjectCreator.UseCtor]
		public ConnectionLogic(Widget widget, string host, int port, Action onConnect, Action onAbort, Action<string> onRetry)
		{
			this.onConnect = onConnect;
			this.onAbort = onAbort;
			this.onRetry = onRetry;

			Game.ConnectionStateChanged += ConnectionStateChanged;

			var panel = widget;
			panel.Get<ButtonWidget>("ABORT_BUTTON").OnClick = () => { CloseWindow(); onAbort(); };

			widget.Get<LabelWidget>("CONNECTING_DESC").GetText = () =>
				"Connecting to {0}:{1}...".F(host, port);
		}

		public static void Connect(string host, int port, string password, Action onConnect, Action onAbort)
		{
			Game.JoinServer(host, port, password);
			Action<string> onRetry = newPassword => Connect(host, port, newPassword, onConnect, onAbort);

			Ui.OpenWindow("CONNECTING_PANEL", new WidgetArgs()
			{
				{ "host", host },
				{ "port", port },
				{ "onConnect", onConnect },
				{ "onAbort", onAbort },
				{ "onRetry", onRetry }
			});
		}
	}

	public class ConnectionFailedLogic : ChromeLogic
	{
		PasswordFieldWidget passwordField;
		bool passwordOffsetAdjusted;

		[ObjectCreator.UseCtor]
		public ConnectionFailedLogic(Widget widget, OrderManager orderManager, Action onAbort, Action<string> onRetry)
		{
			var panel = widget;
			var abortButton = panel.Get<ButtonWidget>("ABORT_BUTTON");
			var retryButton = panel.Get<ButtonWidget>("RETRY_BUTTON");

			abortButton.Visible = onAbort != null;
			abortButton.OnClick = () => { Ui.CloseWindow(); onAbort(); };

			retryButton.Visible = onRetry != null;
			retryButton.OnClick = () =>
			{
				var password = passwordField != null && passwordField.IsVisible() ? passwordField.Text : orderManager.Password;

				Ui.CloseWindow();
				onRetry(password);
			};

			widget.Get<LabelWidget>("CONNECTING_DESC").GetText = () =>
				"Could not connect to {0}:{1}".F(orderManager.Host, orderManager.Port);

			var connectionError = widget.Get<LabelWidget>("CONNECTION_ERROR");
			connectionError.GetText = () => orderManager.ServerError;

			var panelTitle = widget.Get<LabelWidget>("TITLE");
			panelTitle.GetText = () => orderManager.AuthenticationFailed ? "Password Required" : "Connection Failed";

			passwordField = panel.GetOrNull<PasswordFieldWidget>("PASSWORD");
			if (passwordField != null)
			{
				passwordField.TakeKeyboardFocus();
				passwordField.IsVisible = () => orderManager.AuthenticationFailed;
				var passwordLabel = widget.Get<LabelWidget>("PASSWORD_LABEL");
				passwordLabel.IsVisible = passwordField.IsVisible;
				passwordField.OnEnterKey = () => { retryButton.OnClick(); return true; };
				passwordField.OnEscKey = () => { abortButton.OnClick(); return true; };
			}

			passwordOffsetAdjusted = false;
			var connectionFailedTicker = panel.GetOrNull<LogicTickerWidget>("CONNECTION_FAILED_TICKER");
			if (connectionFailedTicker != null)
			{
				connectionFailedTicker.OnTick = () =>
				{
					// Adjust the dialog once the AuthenticationError is parsed.
					if (passwordField.IsVisible() && !passwordOffsetAdjusted)
					{
						var offset = passwordField.Bounds.Y - connectionError.Bounds.Y;
						abortButton.Bounds.Y += offset;
						retryButton.Bounds.Y += offset;
						panel.Bounds.Height += offset;
						panel.Bounds.Y -= offset / 2;

						var background = panel.GetOrNull("CONNECTION_BACKGROUND");
						if (background != null)
							background.Bounds.Height += offset;

						passwordOffsetAdjusted = true;
					}
				};
			}
		}
	}
}