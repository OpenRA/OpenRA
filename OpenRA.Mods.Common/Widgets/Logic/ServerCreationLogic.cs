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
using System.Globalization;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ServerCreationLogic : ChromeLogic
	{
		[FluentReference]
		const string InternetServerNatA = "label-internet-server-nat-A";

		[FluentReference]
		const string InternetServerNatBenabled = "label-internet-server-nat-B-enabled";

		[FluentReference]
		const string InternetServerNatBnotSupported = "label-internet-server-nat-B-not-supported";

		[FluentReference]
		const string InternetServerNatBdisabled = "label-internet-server-nat-B-disabled";

		[FluentReference]
		const string InternetServerNatC = "label-internet-server-nat-C";

		[FluentReference]
		const string LocalServer = "label-local-server";

		[FluentReference("port")]
		const string ServerCreationFailedPrompt = "dialog-server-creation-failed.prompt";

		[FluentReference]
		const string ServerCreationFailedPortUsed = "dialog-server-creation-failed.prompt-port-used";

		[FluentReference("message", "code")]
		const string ServerCreationFailedError = "dialog-server-creation-failed.prompt-error";

		[FluentReference]
		const string ServerCreationFailedTitle = "dialog-server-creation-failed.title";

		[FluentReference]
		const string ServerCreationFailedCancel = "dialog-server-creation-failed.cancel";

		readonly Widget panel;
		readonly ModData modData;
		readonly LabelWidget noticesLabelA, noticesLabelB, noticesLabelC;
		readonly Action onCreate;
		readonly Action onExit;
		MapPreview map = MapCache.UnknownMap;
		bool advertiseOnline;

		[ObjectCreator.UseCtor]
		public ServerCreationLogic(Widget widget, ModData modData, Action onExit, Action openLobby)
		{
			panel = widget;
			this.modData = modData;
			onCreate = openLobby;
			this.onExit = onExit;

			var settings = Game.Settings;

			map = modData.MapCache[
				modData.MapCache.ChooseInitialMap(
					modData.MapCache.PickLastModifiedMap(MapVisibility.Lobby) ?? Game.Settings.Server.Map,
					Game.CosmeticRandom)];

			Ui.LoadWidget("MAP_PREVIEW", panel.Get("MAP_PREVIEW_ROOT"), new WidgetArgs
			{
				{ "orderManager", null },
				{ "getMap", (Func<(MapPreview, Session.MapStatus)>)(() => (map, Session.MapStatus.Playable)) },
				{ "onMouseDown", null },
				{ "getSpawnOccupants", null },
				{ "getDisabledSpawnPoints", null },
				{ "showUnoccupiedSpawnpoints", false },
				{ "mapUpdatesEnabled", true },
				{ "onMapUpdate", (Action<string>)(uid => map = modData.MapCache[uid]) },
			});

			panel.Get<ButtonWidget>("BACK_BUTTON").OnClick = () => { Ui.CloseWindow(); onExit(); };
			panel.Get<ButtonWidget>("CREATE_BUTTON").OnClick = CreateAndJoin;

			var mapButton = panel.GetOrNull<ButtonWidget>("MAP_BUTTON");
			if (mapButton != null)
			{
				panel.Get<ButtonWidget>("MAP_BUTTON").OnClick = () =>
				{
					Ui.OpenWindow("MAPCHOOSER_PANEL", new WidgetArgs()
					{
						{ "initialMap", map.Uid },
						{ "remoteMapPool", null },
						{ "initialTab", MapClassification.System },
						{ "onExit", () => modData.MapCache.UpdateMaps() },
						{ "onSelect", (Action<string>)(uid => map = modData.MapCache[uid]) },
						{ "filter", MapVisibility.Lobby },
						{ "onStart", () => { } }
					});
				};
			}

			var serverName = panel.Get<TextFieldWidget>("SERVER_NAME");
			serverName.Text = Game.Settings.SanitizedServerName(settings.Server.Name);
			serverName.OnEnterKey = _ => { serverName.YieldKeyboardFocus(); return true; };
			serverName.OnLoseFocus = () =>
			{
				serverName.Text = Game.Settings.SanitizedServerName(serverName.Text);
				settings.Server.Name = serverName.Text;
			};

			panel.Get<TextFieldWidget>("LISTEN_PORT").Text = settings.Server.ListenPort.ToString(NumberFormatInfo.CurrentInfo);

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
					(Nat.Status == NatStatus.NotSupported || Nat.Status == NatStatus.Disabled);

				var settingsA = noticesNoUPnP.GetOrNull("SETTINGS_A");
				if (settingsA != null)
					settingsA.IsVisible = () => Nat.Status == NatStatus.Disabled;

				var settingsB = noticesNoUPnP.GetOrNull("SETTINGS_B");
				if (settingsB != null)
					settingsB.IsVisible = () => Nat.Status == NatStatus.Disabled;
			}

			var noticesUPnP = panel.GetOrNull("NOTICES_UPNP");
			if (noticesUPnP != null)
				noticesUPnP.IsVisible = () => advertiseOnline && Nat.Status == NatStatus.Enabled;

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
				var noticesLabelAText = FluentProvider.GetMessage(InternetServerNatA) + " ";
				noticesLabelA.GetText = () => noticesLabelAText;
				var aWidth = Game.Renderer.Fonts[noticesLabelA.Font].Measure(noticesLabelAText).X;
				noticesLabelA.Bounds.Width = aWidth;

				var noticesLabelBText =
					Nat.Status == NatStatus.Enabled ? FluentProvider.GetMessage(InternetServerNatBenabled) :
					Nat.Status == NatStatus.NotSupported ? FluentProvider.GetMessage(InternetServerNatBnotSupported) :
					FluentProvider.GetMessage(InternetServerNatBdisabled);
				noticesLabelB.GetText = () => noticesLabelBText;

				noticesLabelB.TextColor =
					Nat.Status == NatStatus.Enabled ? ChromeMetrics.Get<Color>("NoticeSuccessColor") :
					Nat.Status == NatStatus.NotSupported ? ChromeMetrics.Get<Color>("NoticeErrorColor") :
					ChromeMetrics.Get<Color>("NoticeInfoColor");

				var bWidth = Game.Renderer.Fonts[noticesLabelB.Font].Measure(noticesLabelBText).X;
				noticesLabelB.Bounds.X = noticesLabelA.Bounds.Right;
				noticesLabelB.Bounds.Width = bWidth;
				noticesLabelB.Visible = true;

				var noticesLabelCText = FluentProvider.GetMessage(InternetServerNatC);
				noticesLabelC.GetText = () => noticesLabelCText;
				noticesLabelC.Bounds.X = noticesLabelB.Bounds.Right;
				noticesLabelC.Visible = true;
			}
			else
			{
				var noticesLabelAText = FluentProvider.GetMessage(LocalServer);
				noticesLabelA.GetText = () => noticesLabelAText;
				noticesLabelB.Visible = false;
				noticesLabelC.Visible = false;
			}
		}

		void CreateAndJoin()
		{
			// Refresh MapCache.
			if (modData.MapCache[map.Uid].Status != MapStatus.Available)
				return;

			var name = Game.Settings.SanitizedServerName(panel.Get<TextFieldWidget>("SERVER_NAME").Text);
			if (!int.TryParse(panel.Get<TextFieldWidget>("LISTEN_PORT").Text, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out var listenPort))
				listenPort = 1234;

			var passwordField = panel.GetOrNull<PasswordFieldWidget>("PASSWORD");
			var password = passwordField != null ? passwordField.Text : "";

			// Save new settings.
			Game.Settings.Server.Name = name;
			Game.Settings.Server.ListenPort = listenPort;
			Game.Settings.Server.AdvertiseOnline = advertiseOnline;
			Game.Settings.Server.Map = map.Uid;
			Game.Settings.Server.Password = password;
			Game.Settings.Save();

			// Take a copy so that subsequent changes don't affect the server.
			var settings = Game.Settings.Server.Clone();

			// Create and join the server.
			try
			{
				var endpoint = Game.CreateServer(settings);

				Ui.CloseWindow();
				ConnectionLogic.Connect(endpoint, password, onCreate, onExit);
			}
			catch (System.Net.Sockets.SocketException e)
			{
				var message = FluentProvider.GetMessage(ServerCreationFailedPrompt, "port", Game.Settings.Server.ListenPort);

				// AddressAlreadyInUse (WSAEADDRINUSE)
				if (e.ErrorCode == 10048)
					message += "\n" + FluentProvider.GetMessage(ServerCreationFailedPortUsed);
				else
					message += "\n" + FluentProvider.GetMessage(ServerCreationFailedError, "message", e.Message, "code", e.ErrorCode);

				ConfirmationDialogs.ButtonPrompt(modData, ServerCreationFailedTitle, message,
					onCancel: () => { }, cancelText: ServerCreationFailedCancel);
			}
		}
	}
}
