#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using System.Net;
using OpenRA.Widgets;
using System;
using System.Collections.Generic;

namespace OpenRA.Mods.Cnc.Widgets
{
	public class CncServerCreationLogic : IWidgetDelegate
	{
		Widget panel;
		Action onCreate;
		Action onExit;
		Map map;
		bool advertiseOnline;
		[ObjectCreator.UseCtor]
		public CncServerCreationLogic([ObjectCreator.Param] Widget widget,
		                              [ObjectCreator.Param] Action onExit,
		                              [ObjectCreator.Param] Action openLobby)
		{
			panel = widget.GetWidget("CREATESERVER_PANEL");
			onCreate = openLobby;
			this.onExit = onExit;
			
			var settings = Game.Settings;
			panel.GetWidget<CncMenuButtonWidget>("BACK_BUTTON").OnClick = onExit;
			panel.GetWidget<CncMenuButtonWidget>("CREATE_BUTTON").OnClick = CreateAndJoin;

			panel.GetWidget<CncMenuButtonWidget>("MAP_BUTTON").OnClick = () =>
			{
				Widget.OpenWindow( "MAPCHOOSER_PANEL", new Dictionary<string, object>
				{
					{ "initialMap", map.Uid },
					{ "onExit", new Action(() => Widget.CloseWindow()) },
					{ "onSelect", new Action<Map>(m => { map = m; Widget.CloseWindow(); }) }
				});
			};
			
			if (string.IsNullOrEmpty(Game.Settings.Server.LastMap) || !Game.modData.AvailableMaps.TryGetValue(Game.Settings.Server.LastMap, out map))
				map = Game.modData.AvailableMaps.FirstOrDefault(m => m.Value.Selectable).Value;
			
			panel.GetWidget<MapPreviewWidget>("MAP_PREVIEW").Map = () => map;
			panel.GetWidget<LabelWidget>("MAP_NAME").GetText = () => map.Title;
			
			panel.GetWidget<TextFieldWidget>("SERVER_NAME").Text = settings.Server.Name ?? "";
			panel.GetWidget<TextFieldWidget>("LISTEN_PORT").Text = settings.Server.ListenPort.ToString();
			advertiseOnline = Game.Settings.Server.AdvertiseOnline;
			
			var externalPort = panel.GetWidget<CncTextFieldWidget>("EXTERNAL_PORT");
			externalPort.Text = settings.Server.ExternalPort.ToString();
			externalPort.IsDisabled = () => !advertiseOnline;

			var advertiseCheckbox = panel.GetWidget<CncCheckboxWidget>("ADVERTISE_CHECKBOX");
			advertiseCheckbox.IsChecked = () => advertiseOnline;
			advertiseCheckbox.OnClick = () => advertiseOnline ^= true;
			
			// Disable these until we have some logic behind them
			panel.GetWidget<CncTextFieldWidget>("SERVER_DESC").IsDisabled = () => true;
			panel.GetWidget<CncTextFieldWidget>("SERVER_PASSWORD").IsDisabled = () => true;
		}
	
		void CreateAndJoin()
		{
			var name = panel.GetWidget<TextFieldWidget>("SERVER_NAME").Text;
			int listenPort, externalPort;
			if (!int.TryParse(panel.GetWidget<TextFieldWidget>("LISTEN_PORT").Text, out listenPort))
				listenPort = 1234;
			
			if (!int.TryParse(panel.GetWidget<TextFieldWidget>("EXTERNAL_PORT").Text, out externalPort))
				externalPort = 1234;
			
			// Save new settings
			Game.Settings.Server.Name = name;
			Game.Settings.Server.ListenPort = listenPort;
			Game.Settings.Server.ExternalPort = externalPort;
			Game.Settings.Server.AdvertiseOnline = advertiseOnline;
			Game.Settings.Server.LastMap = map.Uid;
			Game.Settings.Save();
			
			// Create and join the server
			Game.CreateServer(listenPort, name, map.Uid);
			Widget.CloseWindow();
			CncConnectingLogic.Connect(IPAddress.Loopback.ToString(), Game.Settings.Server.ListenPort, onCreate, onExit);
		}
	}
}
