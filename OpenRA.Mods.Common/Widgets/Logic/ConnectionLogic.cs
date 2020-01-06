#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
		public ConnectionLogic(Widget widget, ConnectionTarget endpoint, Action onConnect, Action onAbort, Action<string> onRetry)
		{
			this.onConnect = onConnect;
			this.onAbort = onAbort;
			this.onRetry = onRetry;

			Game.ConnectionStateChanged += ConnectionStateChanged;

			var panel = widget;
			panel.Get<ButtonWidget>("ABORT_BUTTON").OnClick = () => { CloseWindow(); onAbort(); };

			widget.Get<LabelWidget>("CONNECTING_DESC").GetText = () =>
				"Connecting to {0}...".F(endpoint);
		}

		public static void Connect(ConnectionTarget endpoint, string password, Action onConnect, Action onAbort)
		{
			Game.JoinServer(endpoint, password);
			Action<string> onRetry = newPassword => Connect(endpoint, newPassword, onConnect, onAbort);

			Ui.OpenWindow("CONNECTING_PANEL", new WidgetArgs()
			{
				{ "endpoint", endpoint },
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
				"Could not connect to {0}".F(orderManager.Endpoint);

			var connectionError = widget.Get<LabelWidget>("CONNECTION_ERROR");
			connectionError.GetText = () => orderManager.ServerError ?? orderManager.Connection.ErrorMessage ?? "Unknown error";

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

			var mod = orderManager.ServerExternalMod;
			var modTitle = mod.Title;
			var modVersion = mod.Version;

			switchButton.OnClick = () =>
			{
				var launchCommand = "Launch.URI={0}".F(new UriBuilder("tcp", orderManager.Connection.EndPoint.Address.ToString(), orderManager.Connection.EndPoint.Port));
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
			if (logo != null)
			{
				logo.GetSprite = () =>
				{
					var ws = Game.Renderer.WindowScale;
					if (ws > 2 && mod.Icon3x != null)
						return mod.Icon3x;

					if (ws > 1 && mod.Icon2x != null)
						return mod.Icon2x;

					return mod.Icon;
				};
			}

			if (logo != null && mod.Icon == null)
			{
				// Hide the logo and center just the text
				if (title != null)
					title.Bounds.X = logo.Bounds.Left;

				if (version != null)
					version.Bounds.X = logo.Bounds.X;

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
				container.Bounds.X += (container.Bounds.Width - width) / 2;
				container.Bounds.Width = width;
			}
		}
	}
}
