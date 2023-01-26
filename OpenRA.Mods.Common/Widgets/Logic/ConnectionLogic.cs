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
	public class ConnectionLogic : ChromeLogic
	{
		[TranslationReference("endpoint")]
		const string ConnectingToEndpoint = "label-connecting-to-endpoint";

		readonly Action onConnect;
		readonly Action onAbort;
		readonly Action<string> onRetry;

		void ConnectionStateChanged(OrderManager om, string password, NetworkConnection connection)
		{
			if (connection.ConnectionState == ConnectionState.Connected)
			{
				CloseWindow();
				onConnect();
			}
			else if (connection.ConnectionState == ConnectionState.NotConnected)
			{
				CloseWindow();

				var switchPanel = CurrentServerSettings.ServerExternalMod != null ? "CONNECTION_SWITCHMOD_PANEL" : "CONNECTIONFAILED_PANEL";
				Ui.OpenWindow(switchPanel, new WidgetArgs()
				{
					{ "orderManager", om },
					{ "connection", connection },
					{ "password", password },
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
		public ConnectionLogic(Widget widget, ModData modData, ConnectionTarget endpoint, Action onConnect, Action onAbort, Action<string> onRetry)
		{
			this.onConnect = onConnect;
			this.onAbort = onAbort;
			this.onRetry = onRetry;

			Game.ConnectionStateChanged += ConnectionStateChanged;

			var panel = widget;
			panel.Get<ButtonWidget>("ABORT_BUTTON").OnClick = () => { CloseWindow(); onAbort(); };

			var connectingDesc = modData.Translation.GetString(ConnectingToEndpoint, Translation.Arguments("endpoint", endpoint));
			widget.Get<LabelWidget>("CONNECTING_DESC").GetText = () => connectingDesc;
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
		[TranslationReference("target")]
		const string CouldNotConnectToTarget = "label-could-not-connect-to-target";

		[TranslationReference]
		const string UnknownError = "label-unknown-error";

		[TranslationReference]
		const string PasswordRequired = "label-password-required";

		[TranslationReference]
		const string ConnectionFailed = "label-connection-failed";

		readonly PasswordFieldWidget passwordField;
		bool passwordOffsetAdjusted;

		[ObjectCreator.UseCtor]
		public ConnectionFailedLogic(Widget widget, ModData modData, OrderManager orderManager, NetworkConnection connection, string password, Action onAbort, Action<string> onRetry)
		{
			var panel = widget;
			var abortButton = panel.Get<ButtonWidget>("ABORT_BUTTON");
			var retryButton = panel.Get<ButtonWidget>("RETRY_BUTTON");

			abortButton.Visible = onAbort != null;
			abortButton.OnClick = () => { Ui.CloseWindow(); onAbort(); };

			retryButton.Visible = onRetry != null;
			retryButton.OnClick = () =>
			{
				var pass = passwordField != null && passwordField.IsVisible() ? passwordField.Text : password;

				Ui.CloseWindow();
				onRetry(pass);
			};

			var connectingDescText = modData.Translation.GetString(CouldNotConnectToTarget, Translation.Arguments("target", connection.Target));
			widget.Get<LabelWidget>("CONNECTING_DESC").GetText = () => connectingDescText;

			var connectionError = widget.Get<LabelWidget>("CONNECTION_ERROR");
			var connectionErrorText = orderManager.ServerError != null ? modData.Translation.GetString(orderManager.ServerError) : connection.ErrorMessage ?? modData.Translation.GetString(UnknownError);
			connectionError.GetText = () => connectionErrorText;

			var panelTitle = widget.Get<LabelWidget>("TITLE");
			var panelTitleText = orderManager.AuthenticationFailed ? modData.Translation.GetString(PasswordRequired) : modData.Translation.GetString(ConnectionFailed);
			panelTitle.GetText = () => panelTitleText;

			passwordField = panel.GetOrNull<PasswordFieldWidget>("PASSWORD");
			if (passwordField != null)
			{
				passwordField.TakeKeyboardFocus();
				passwordField.IsVisible = () => orderManager.AuthenticationFailed;
				var passwordLabel = widget.Get<LabelWidget>("PASSWORD_LABEL");
				passwordLabel.IsVisible = passwordField.IsVisible;
				passwordField.OnEnterKey = _ => { retryButton.OnClick(); return true; };
				passwordField.OnEscKey = _ => { abortButton.OnClick(); return true; };
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
		[TranslationReference]
		const string ModSwitchFailed = "notification-mod-switch-failed";

		[ObjectCreator.UseCtor]
		public ConnectionSwitchModLogic(Widget widget, OrderManager orderManager, NetworkConnection connection, Action onAbort, Action<string> onRetry)
		{
			var panel = widget;
			var abortButton = panel.Get<ButtonWidget>("ABORT_BUTTON");
			var switchButton = panel.Get<ButtonWidget>("SWITCH_BUTTON");

			var mod = CurrentServerSettings.ServerExternalMod;
			var modTitle = mod.Title;
			var modVersion = mod.Version;

			switchButton.OnClick = () =>
			{
				var launchCommand = $"Launch.URI={new UriBuilder("tcp", connection.EndPoint.Address.ToString(), connection.EndPoint.Port)}";
				Game.SwitchToExternalMod(CurrentServerSettings.ServerExternalMod, new[] { launchCommand }, () =>
				{
					orderManager.ServerError = ModSwitchFailed;
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

				if (mod.Icon == null)
				{
					// Hide the logo and center just the text
					if (title != null)
						title.Bounds.X = logo.Bounds.X;

					if (version != null)
						version.Bounds.X = logo.Bounds.X;

					width -= logo.Bounds.Width;
				}
				else
				{
					// Add an equal logo margin on the right of the text
					width += logo.Bounds.Width;
				}
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
