#region Copyright & License Information
/*
 * Copyright 2007-2012 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Net;
using OpenRA.GameRules;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class ServerCreationLogic
	{
		Widget panel;
		Action onCreate;
		Action onExit;
		Map map;
		bool advertiseOnline;
		bool allowPortForward;

		[ObjectCreator.UseCtor]
		public ServerCreationLogic(Widget widget, Action onExit, Action openLobby)
		{
			panel = widget;
			onCreate = openLobby;
			this.onExit = onExit;

			var settings = Game.Settings;

			panel.Get<ButtonWidget>("BACK_BUTTON").OnClick = () => { Ui.CloseWindow(); onExit(); };
			panel.Get<ButtonWidget>("CREATE_BUTTON").OnClick = CreateAndJoin;

			map = Game.modData.AvailableMaps[ WidgetUtils.ChooseInitialMap(Game.Settings.Server.Map) ];

			var mapButton = panel.GetOrNull<ButtonWidget>("MAP_BUTTON");
			if (mapButton != null)
			{
				panel.Get<ButtonWidget>("MAP_BUTTON").OnClick = () =>
				{
					Ui.OpenWindow("MAPCHOOSER_PANEL", new WidgetArgs()
					{
						{ "initialMap", map.Uid },
						{ "onExit", () => {} },
						{ "onSelect", (Action<Map>)(m => map = m) }
					});
				};

				panel.Get<MapPreviewWidget>("MAP_PREVIEW").Map = () => map;
				panel.Get<LabelWidget>("MAP_NAME").GetText = () => map.Title;
			}

			panel.Get<TextFieldWidget>("SERVER_NAME").Text = settings.Server.Name ?? "";
			panel.Get<TextFieldWidget>("LISTEN_PORT").Text = settings.Server.ListenPort.ToString();
			advertiseOnline = Game.Settings.Server.AdvertiseOnline;

			var externalPort = panel.Get<TextFieldWidget>("EXTERNAL_PORT");
			externalPort.Text = settings.Server.ExternalPort.ToString();
			externalPort.IsDisabled = () => !advertiseOnline;

			var advertiseCheckbox = panel.Get<CheckboxWidget>("ADVERTISE_CHECKBOX");
			advertiseCheckbox.IsChecked = () => advertiseOnline;
			advertiseCheckbox.OnClick = () => advertiseOnline ^= true;

			allowPortForward = Game.Settings.Server.AllowPortForward;
			var UPnPCheckbox = panel.Get<CheckboxWidget>("UPNP_CHECKBOX");
			UPnPCheckbox.IsChecked = () => allowPortForward;
			UPnPCheckbox.OnClick = () => allowPortForward ^= true;
			UPnPCheckbox.IsDisabled = () => !Game.Settings.Server.NatDeviceAvailable;

			var passwordField = panel.GetOrNull<PasswordFieldWidget>("PASSWORD");
			if (passwordField != null)
				passwordField.Text = Game.Settings.Server.Password;
		}

		void CreateAndJoin()
		{
			var name = panel.Get<TextFieldWidget>("SERVER_NAME").Text;
			int listenPort, externalPort;
			if (!int.TryParse(panel.Get<TextFieldWidget>("LISTEN_PORT").Text, out listenPort))
				listenPort = 1234;

			if (!int.TryParse(panel.Get<TextFieldWidget>("EXTERNAL_PORT").Text, out externalPort))
				externalPort = 1234;

			var passwordField = panel.GetOrNull<PasswordFieldWidget>("PASSWORD");
			var password = passwordField != null ? passwordField.Text : "";

			// Save new settings
			Game.Settings.Server.Name = name;
			Game.Settings.Server.ListenPort = listenPort;
			Game.Settings.Server.ExternalPort = externalPort;
			Game.Settings.Server.AdvertiseOnline = advertiseOnline;
			Game.Settings.Server.AllowPortForward = allowPortForward;
			Game.Settings.Server.Map = map.Uid;
			Game.Settings.Server.Password = password;
			Game.Settings.Save();

			// Take a copy so that subsequent changes don't affect the server
			var settings = new ServerSettings(Game.Settings.Server);

			// Create and join the server
			bool serverSuccess = false;
			try {
				Game.CreateServer(settings);
				serverSuccess = true;
			}
			catch (System.Net.Sockets.SocketException) {
				panel.Get<TextFieldWidget>("LISTEN_PORT").Text = (listenPort+1).ToString();
				// XXX: Much better solution would be something like this:
				//   AlertBox("Unable to bind on port " + listenPort.ToString() + ": " + e.Message, AlertBox.Buttons.OK);
			}
			finally {
				if (serverSuccess) {
					Ui.CloseWindow();
					ConnectionLogic.Connect(IPAddress.Loopback.ToString(), Game.Settings.Server.ListenPort, password, onCreate, onExit);
				}
			}
		}
	}
}
