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
		Map map;
		bool advertiseOnline;
		[ObjectCreator.UseCtor]
		public CncServerCreationLogic([ObjectCreator.Param] Widget widget,
		                              [ObjectCreator.Param] Action onExit,
		                              [ObjectCreator.Param] Action openLobby)
		{
			panel = widget.GetWidget("CREATESERVER_PANEL");
			onCreate = openLobby;
			
			var settings = Game.Settings;
			panel.GetWidget<CncMenuButtonWidget>("BACK_BUTTON").OnClick = onExit;
			panel.GetWidget<CncMenuButtonWidget>("CREATE_BUTTON").OnClick = CreateAndJoin;

			panel.GetWidget<CncMenuButtonWidget>("MAP_BUTTON").OnClick = () =>
			{
				Widget.OpenWindow( "MAPCHOOSER_PANEL", new Dictionary<string, object>
				{
					{ "initialMap", map },
					{ "onExit", new Action(() => Widget.CloseWindow()) },
					{ "onSelect", new Action<Map>(m => { map = m; Widget.CloseWindow(); }) }
				});
			};
			
			map = Game.modData.AvailableMaps.FirstOrDefault(m => m.Value.Selectable).Value;
			panel.GetWidget<MapPreviewWidget>("MAP_PREVIEW").Map = () => map;
			panel.GetWidget<LabelWidget>("MAP_NAME").GetText = () => map.Title;
			
			panel.GetWidget<TextFieldWidget>("SERVER_NAME").Text = settings.Server.Name ?? "";
			panel.GetWidget<TextFieldWidget>("LISTEN_PORT").Text = settings.Server.ListenPort.ToString();
			panel.GetWidget<TextFieldWidget>("EXTERNAL_PORT").Text = settings.Server.ExternalPort.ToString();

			var advertiseCheckbox = panel.GetWidget<CncCheckboxWidget>("ADVERTISE_CHECKBOX");
			advertiseCheckbox.IsChecked = () => advertiseOnline;
			advertiseCheckbox.OnClick = () => advertiseOnline ^= true;
		}
	
		void CreateAndJoin()
		{
			Game.Settings.Server.Name = panel.GetWidget<TextFieldWidget>("SERVER_NAME").Text;
			Game.Settings.Server.ListenPort = int.Parse(panel.GetWidget<TextFieldWidget>("LISTEN_PORT").Text);
			Game.Settings.Server.ExternalPort = int.Parse(panel.GetWidget<TextFieldWidget>("EXTERNAL_PORT").Text);
			Game.Settings.Server.AdvertiseOnline = advertiseOnline;
			Game.Settings.Save();

			Game.CreateAndJoinServer(Game.Settings, map.Uid);
			Widget.CloseWindow();
			onCreate();
		}
	}
}
