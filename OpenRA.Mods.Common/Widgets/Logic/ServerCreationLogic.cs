#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Net;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ServerCreationLogic
	{
		Widget panel;
		Action onCreate;
		Action onExit;
		MapPreview preview = MapCache.UnknownMap;
		bool advertiseOnline;
		bool allowPortForward;

		[ObjectCreator.UseCtor]
		public ServerCreationLogic(Widget widget, Action onExit, Action openLobby)
		{
			panel = widget;
			onCreate = openLobby;
			this.onExit = onExit;

			var settings = Game.Settings;
			preview = Game.ModData.MapCache[WidgetUtils.ChooseInitialMap(Game.Settings.Server.Map)];

			panel.Get<ButtonWidget>("BACK_BUTTON").OnClick = () => { Ui.CloseWindow(); onExit(); };
			panel.Get<ButtonWidget>("CREATE_BUTTON").OnClick = CreateAndJoin;

			var mapButton = panel.GetOrNull<ButtonWidget>("MAP_BUTTON");
			if (mapButton != null)
			{
				panel.Get<ButtonWidget>("MAP_BUTTON").OnClick = () =>
				{
					Ui.OpenWindow("MAPCHOOSER_PANEL", new WidgetArgs()
					{
						{ "initialMap", preview.Uid },
						{ "initialTab", MapClassification.System },
						{ "onExit", () => { } },
						{ "onSelect", (Action<string>)(uid => preview = Game.ModData.MapCache[uid]) },
						{ "filter", MapVisibility.Lobby },
						{ "onStart", () => { } }
					});
				};

				panel.Get<MapPreviewWidget>("MAP_PREVIEW").Preview = () => preview;
				panel.Get<LabelWidget>("MAP_NAME").GetText = () => preview.Title;
			}

			var serverName = panel.Get<TextFieldWidget>("SERVER_NAME");
			serverName.Text = Settings.SanitizedServerName(settings.Server.Name);
			serverName.OnEnterKey = () => { serverName.YieldKeyboardFocus(); return true; };
			serverName.OnLoseFocus = () =>
			{
				serverName.Text = Settings.SanitizedServerName(serverName.Text);
				settings.Server.Name = serverName.Text;
			};

			panel.Get<TextFieldWidget>("LISTEN_PORT").Text = settings.Server.ListenPort.ToString();

			advertiseOnline = Game.Settings.Server.AdvertiseOnline;

			var externalPort = panel.Get<TextFieldWidget>("EXTERNAL_PORT");
			externalPort.Text = settings.Server.ExternalPort.ToString();
			externalPort.IsDisabled = () => !advertiseOnline;

			var advertiseCheckbox = panel.Get<CheckboxWidget>("ADVERTISE_CHECKBOX");
			advertiseCheckbox.IsChecked = () => advertiseOnline;
			advertiseCheckbox.OnClick = () => advertiseOnline ^= true;

			allowPortForward = Game.Settings.Server.AllowPortForward;
			var checkboxUPnP = panel.Get<CheckboxWidget>("UPNP_CHECKBOX");
			checkboxUPnP.IsChecked = () => allowPortForward;
			checkboxUPnP.OnClick = () => allowPortForward ^= true;
			checkboxUPnP.IsDisabled = () => !Game.Settings.Server.NatDeviceAvailable;

			var passwordField = panel.GetOrNull<PasswordFieldWidget>("PASSWORD");
			if (passwordField != null)
				passwordField.Text = Game.Settings.Server.Password;
		}

		void CreateAndJoin()
		{
			var name = Settings.SanitizedServerName(panel.Get<TextFieldWidget>("SERVER_NAME").Text);
			int listenPort, externalPort;
			if (!Exts.TryParseIntegerInvariant(panel.Get<TextFieldWidget>("LISTEN_PORT").Text, out listenPort))
				listenPort = 1234;

			if (!Exts.TryParseIntegerInvariant(panel.Get<TextFieldWidget>("EXTERNAL_PORT").Text, out externalPort))
				externalPort = 1234;

			var passwordField = panel.GetOrNull<PasswordFieldWidget>("PASSWORD");
			var password = passwordField != null ? passwordField.Text : "";

			// Save new settings
			Game.Settings.Server.Name = name;
			Game.Settings.Server.ListenPort = listenPort;
			Game.Settings.Server.ExternalPort = externalPort;
			Game.Settings.Server.AdvertiseOnline = advertiseOnline;
			Game.Settings.Server.AllowPortForward = allowPortForward;
			Game.Settings.Server.Map = preview.Uid;
			Game.Settings.Server.Password = password;
			Game.Settings.Save();

			// Take a copy so that subsequent changes don't affect the server
			var settings = new ServerSettings(Game.Settings.Server);

			// Create and join the server
			try
			{
				Game.CreateServer(settings);
			}
			catch (System.Net.Sockets.SocketException e)
			{
				var err_msg = "Could not listen on port {0}.".F(Game.Settings.Server.ListenPort);
				if (e.ErrorCode == 10048) { // AddressAlreadyInUse (WSAEADDRINUSE)
					err_msg += "\n\nCheck if the port is already being used.";
				} else {
					err_msg += "\n\nError is: \"{0}\" ({1})".F(e.Message, e.ErrorCode);
				}

				ConfirmationDialogs.CancelPrompt("Server Creation Failed", err_msg, cancelText: "OK");
				return;
			}

			Ui.CloseWindow();
			ConnectionLogic.Connect(IPAddress.Loopback.ToString(), Game.Settings.Server.ListenPort, password, onCreate, onExit);
		}
	}
}
