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
using System.Net;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ServerCreationLogic : ChromeLogic
	{
		Widget panel;
		Action onCreate;
		Action onExit;
		MapPreview preview = MapCache.UnknownMap;
		bool advertiseOnline;
		bool allowPortForward;

		[ObjectCreator.UseCtor]
		public ServerCreationLogic(Widget widget, ModData modData, Action onExit, Action openLobby)
		{
			panel = widget;
			onCreate = openLobby;
			this.onExit = onExit;

			var settings = Game.Settings;
			preview = modData.MapCache[modData.MapCache.ChooseInitialMap(Game.Settings.Server.Map, Game.CosmeticRandom)];

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
						{ "onSelect", (Action<string>)(uid => preview = modData.MapCache[uid]) },
						{ "filter", MapVisibility.Lobby },
						{ "onStart", () => { } }
					});
				};

				panel.Get<MapPreviewWidget>("MAP_PREVIEW").Preview = () => preview;

				var mapTitle = panel.Get<LabelWidget>("MAP_NAME");
				if (mapTitle != null)
				{
					var font = Game.Renderer.Fonts[mapTitle.Font];
					var title = new CachedTransform<MapPreview, string>(m => WidgetUtils.TruncateText(m.Title, mapTitle.Bounds.Width, font));
					mapTitle.GetText = () => title.Update(preview);
				}
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
			checkboxUPnP.IsDisabled = () => !Game.Settings.Server.AllowPortForward;

			var labelUPnP = panel.GetOrNull<LabelWidget>("UPNP_NOTICE");
			if (labelUPnP != null)
				labelUPnP.IsVisible = () => !Game.Settings.Server.DiscoverNatDevices;

			var labelUPnPUnsupported = panel.GetOrNull<LabelWidget>("UPNP_UNSUPPORTED_NOTICE");
			if (labelUPnPUnsupported != null)
				labelUPnPUnsupported.IsVisible = () => Game.Settings.Server.DiscoverNatDevices && !Game.Settings.Server.AllowPortForward;

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
			var settings = Game.Settings.Server.Clone();

			// Create and join the server
			try
			{
				Game.CreateServer(settings);
			}
			catch (System.Net.Sockets.SocketException e)
			{
				var message = "Could not listen on port {0}.".F(Game.Settings.Server.ListenPort);
				if (e.ErrorCode == 10048) { // AddressAlreadyInUse (WSAEADDRINUSE)
					message += "\nCheck if the port is already being used.";
				} else {
					message += "\nError is: \"{0}\" ({1})".F(e.Message, e.ErrorCode);
				}

				ConfirmationDialogs.ButtonPrompt("Server Creation Failed", message, onCancel: () => { }, cancelText: "Back");
				return;
			}

			ConnectionLogic.Connect(IPAddress.Loopback.ToString(), Game.Settings.Server.ListenPort, password, onCreate, onExit);
		}
	}
}
