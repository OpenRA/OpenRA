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
using System.Linq;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ServerCreationLogic : ChromeLogic
	{
		readonly Widget panel;
		readonly LabelWidget noticesLabelA, noticesLabelB, noticesLabelC;
		readonly Action onCreate;
		readonly Action onExit;
		MapPreview preview = MapCache.UnknownMap;
		bool advertiseOnline;

		[ObjectCreator.UseCtor]
		public ServerCreationLogic(Widget widget, ModData modData, Action onExit, Action openLobby)
		{
			panel = widget;
			onCreate = openLobby;
			this.onExit = onExit;

			var settings = Game.Settings;
			preview = modData.MapCache[modData.MapCache.ChooseInitialMap(Game.Settings.Server.Map, Game.CosmeticRandom)];

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
						{ "onSelect", (Action<string>)(uid => preview = modData.MapCache[uid]) },
						{ "filter", MapVisibility.Lobby },
						{ "onStart", () => { } }
					});
				};

				panel.Get<MapPreviewWidget>("MAP_PREVIEW").Preview = () => preview;

				var titleLabel = panel.GetOrNull<LabelWithTooltipWidget>("MAP_TITLE");
				if (titleLabel != null)
				{
					var font = Game.Renderer.Fonts[titleLabel.Font];
					var title = new CachedTransform<MapPreview, string>(m => WidgetUtils.TruncateText(m.Title, titleLabel.Bounds.Width, font));
					titleLabel.GetText = () => title.Update(preview);
					titleLabel.GetTooltipText = () => preview.Title;
				}

				var typeLabel = panel.GetOrNull<LabelWidget>("MAP_TYPE");
				if (typeLabel != null)
				{
					var type = new CachedTransform<MapPreview, string>(m => m.Categories.FirstOrDefault() ?? "");
					typeLabel.GetText = () => type.Update(preview);
				}

				var authorLabel = panel.GetOrNull<LabelWidget>("MAP_AUTHOR");
				if (authorLabel != null)
				{
					var font = Game.Renderer.Fonts[authorLabel.Font];
					var author = new CachedTransform<MapPreview, string>(
						m => WidgetUtils.TruncateText("Created by {0}".F(m.Author), authorLabel.Bounds.Width, font));
					authorLabel.GetText = () => author.Update(preview);
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

			var advertiseCheckbox = panel.Get<CheckboxWidget>("ADVERTISE_CHECKBOX");
			advertiseCheckbox.IsChecked = () => advertiseOnline;
			advertiseCheckbox.OnClick = () =>
			{
				advertiseOnline ^= true;
				BuildNotices();
			};

			var passwordField = panel.GetOrNull<PasswordFieldWidget>("PASSWORD");
			if (passwordField != null)
				passwordField.Text = Game.Settings.Server.Password;

			noticesLabelA = panel.GetOrNull<LabelWidget>("NOTICES_HEADER_A");
			noticesLabelB = panel.GetOrNull<LabelWidget>("NOTICES_HEADER_B");
			noticesLabelC = panel.GetOrNull<LabelWidget>("NOTICES_HEADER_C");

			var noticesNoUPnP = panel.GetOrNull("NOTICES_NO_UPNP");
			if (noticesNoUPnP != null)
			{
				noticesNoUPnP.IsVisible = () => advertiseOnline &&
					(UPnP.Status == UPnPStatus.NotSupported || UPnP.Status == UPnPStatus.Disabled);

				var settingsA = noticesNoUPnP.GetOrNull("SETTINGS_A");
				if (settingsA != null)
					settingsA.IsVisible = () => UPnP.Status == UPnPStatus.Disabled;

				var settingsB = noticesNoUPnP.GetOrNull("SETTINGS_B");
				if (settingsB != null)
					settingsB.IsVisible = () => UPnP.Status == UPnPStatus.Disabled;
			}

			var noticesUPnP = panel.GetOrNull("NOTICES_UPNP");
			if (noticesUPnP != null)
				noticesUPnP.IsVisible = () => advertiseOnline && UPnP.Status == UPnPStatus.Enabled;

			var noticesLAN = panel.GetOrNull("NOTICES_LAN");
			if (noticesLAN != null)
				noticesLAN.IsVisible = () => !advertiseOnline;

			BuildNotices();
		}

		void BuildNotices()
		{
			if (noticesLabelA == null || noticesLabelB == null || noticesLabelC == null)
				return;

			if (advertiseOnline)
			{
				noticesLabelA.Text = "Internet Server (UPnP ";
				var aWidth = Game.Renderer.Fonts[noticesLabelA.Font].Measure(noticesLabelA.Text).X;
				noticesLabelA.Bounds.Width = aWidth;

				var status = UPnP.Status;
				noticesLabelB.Text = status == UPnPStatus.Enabled ? "Enabled" :
					status == UPnPStatus.NotSupported ? "Not Supported" : "Disabled";

				noticesLabelB.TextColor = status == UPnPStatus.Enabled ? ChromeMetrics.Get<Color>("NoticeSuccessColor") :
					status == UPnPStatus.NotSupported ? ChromeMetrics.Get<Color>("NoticeErrorColor") :
					ChromeMetrics.Get<Color>("NoticeInfoColor");

				var bWidth = Game.Renderer.Fonts[noticesLabelB.Font].Measure(noticesLabelB.Text).X;
				noticesLabelB.Bounds.X = noticesLabelA.Bounds.Right;
				noticesLabelB.Bounds.Width = bWidth;
				noticesLabelB.Visible = true;

				noticesLabelC.Text = "):";
				noticesLabelC.Bounds.X = noticesLabelB.Bounds.Right;
				noticesLabelC.Visible = true;
			}
			else
			{
				noticesLabelA.Text = "Local Server:";
				noticesLabelB.Visible = false;
				noticesLabelC.Visible = false;
			}
		}

		void CreateAndJoin()
		{
			var name = Settings.SanitizedServerName(panel.Get<TextFieldWidget>("SERVER_NAME").Text);
			int listenPort;
			if (!Exts.TryParseIntegerInvariant(panel.Get<TextFieldWidget>("LISTEN_PORT").Text, out listenPort))
				listenPort = 1234;

			var passwordField = panel.GetOrNull<PasswordFieldWidget>("PASSWORD");
			var password = passwordField != null ? passwordField.Text : "";

			// Save new settings
			Game.Settings.Server.Name = name;
			Game.Settings.Server.ListenPort = listenPort;
			Game.Settings.Server.AdvertiseOnline = advertiseOnline;
			Game.Settings.Server.Map = preview.Uid;
			Game.Settings.Server.Password = password;
			Game.Settings.Save();

			// Take a copy so that subsequent changes don't affect the server
			var settings = Game.Settings.Server.Clone();

			// Create and join the server
			try
			{
				var endpoint = Game.CreateServer(settings);

				Ui.CloseWindow();
				ConnectionLogic.Connect(endpoint, password, onCreate, onExit);
			}
			catch (System.Net.Sockets.SocketException e)
			{
				var message = "Could not listen on port {0}.".F(Game.Settings.Server.ListenPort);

				// AddressAlreadyInUse (WSAEADDRINUSE)
				if (e.ErrorCode == 10048)
					message += "\nCheck if the port is already being used.";
				else
					message += "\nError is: \"{0}\" ({1})".F(e.Message, e.ErrorCode);

				ConfirmationDialogs.ButtonPrompt("Server Creation Failed", message, onCancel: () => { }, cancelText: "Back");
			}
		}
	}
}
