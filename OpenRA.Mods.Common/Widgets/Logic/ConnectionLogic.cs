#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
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

				var switchPanel = om.ServerExternalMod != null ? "CONNECTION_SWITCHMOD_PANEL" : "CONNECTIONFAILED_PANEL";
				Ui.OpenWindow(switchPanel, new WidgetArgs()
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

			if (abortButton.Visible && !retryButton.Visible)
				abortButton.Bounds.X = abortButton.Parent.Bounds.Width / 2 - abortButton.Bounds.Width / 2;

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

	public class ConnectionSwitchModLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public ConnectionSwitchModLogic(Widget widget, OrderManager orderManager, Action onAbort, Action<string> onRetry)
		{
			var panel = widget;
			var abortButton = panel.Get<ButtonWidget>("ABORT_BUTTON");
			var switchButton = panel.Get<ButtonWidget>("SWITCH_BUTTON");

			var modTitle = orderManager.ServerExternalMod.Title;
			var modVersion = orderManager.ServerExternalMod.Version;
			var modIcon = orderManager.ServerExternalMod.Icon;

			switchButton.OnClick = () =>
			{
				var launchCommand = "Launch.Connect=" + orderManager.Host + ":" + orderManager.Port;
				Game.SwitchToExternalMod(orderManager.ServerExternalMod, new[] { launchCommand }, () =>
				{
					orderManager.ServerError = "Failed to switch mod.";
					Ui.CloseWindow();
					Ui.OpenWindow("CONNECTIONFAILED_PANEL", new WidgetArgs()
					{
						{ "orderManager", orderManager },
						{ "onAbort", onAbort },
						{ "onRetry", onRetry }
					});
				});
			};

			abortButton.Visible = onAbort != null;
			abortButton.OnClick = () => { Ui.CloseWindow(); onAbort(); };

			var width = 0;
			var title = panel.GetOrNull<LabelWidget>("MOD_TITLE");
			if (title != null)
			{
				var font = Game.Renderer.Fonts[title.Font];
				var label = WidgetUtils.TruncateText(modTitle, title.Bounds.Width, font);
				var labelWidth = font.Measure(label).X;
				width = Math.Max(width, title.Bounds.X + labelWidth);
				title.Bounds.Width = labelWidth;
				title.GetText = () => label;
			}

			var version = panel.GetOrNull<LabelWidget>("MOD_VERSION");
			if (version != null)
			{
				var font = Game.Renderer.Fonts[version.Font];
				var label = WidgetUtils.TruncateText(modVersion, version.Bounds.Width, font);
				var labelWidth = font.Measure(label).X;
				width = Math.Max(width, version.Bounds.X + labelWidth);
				version.Bounds.Width = labelWidth;
				version.GetText = () => label;
			}

			var logo = panel.GetOrNull<RGBASpriteWidget>("MOD_ICON");
			if (logo != null && modIcon != null)
				logo.GetSprite = () => modIcon;

			if (logo != null && modIcon == null)
			{
				// Hide the logo and center just the text
				if (title != null)
					title.Bounds.Offset(logo.Bounds.Left - title.Bounds.Left, 0);

				if (version != null)
					version.Bounds.Offset(logo.Bounds.Left - version.Bounds.Left, 0);

				width -= logo.Bounds.Width;
			}
			else
			{
				// Add an equal logo margin on the right of the text
				width += logo.Bounds.Width;
			}

			var container = panel.GetOrNull("MOD_CONTAINER");
			if (container != null)
			{
				container.Bounds.Offset((container.Bounds.Width - width) / 2, 0);
				container.Bounds.Width = width;
			}
		}
	}
}
