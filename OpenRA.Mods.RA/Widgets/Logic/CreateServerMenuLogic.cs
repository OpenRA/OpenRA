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
using OpenRA.GameRules;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class CreateServerMenuLogic
	{
		[ObjectCreator.UseCtor]
		public CreateServerMenuLogic( [ObjectCreator.Param( "widget" )] Widget cs )
		{
			var settings = Game.Settings;

			cs.GetWidget<ButtonWidget>("BUTTON_CANCEL").OnMouseUp = mi => Widget.CloseWindow();
			cs.GetWidget<ButtonWidget>("BUTTON_START").OnMouseUp = mi =>
			{
				settings.Server.Name = cs.GetWidget<TextFieldWidget>("GAME_TITLE").Text;
				settings.Server.ListenPort = int.Parse(cs.GetWidget<TextFieldWidget>("LISTEN_PORT").Text);
				settings.Server.ExternalPort = int.Parse(cs.GetWidget<TextFieldWidget>("EXTERNAL_PORT").Text);
				settings.Server.Map = Game.modData.AvailableMaps.FirstOrDefault(m => m.Value.Selectable).Key;
				settings.Save();
				
				// Take a copy so that subsequent settings changes don't affect the server
				Game.CreateServer(new ServerSettings(Game.Settings.Server));
				Game.JoinServer(IPAddress.Loopback.ToString(), settings.Server.ListenPort);
			};
			
			cs.GetWidget<TextFieldWidget>("GAME_TITLE").Text = settings.Server.Name ?? "";
			cs.GetWidget<TextFieldWidget>("LISTEN_PORT").Text = settings.Server.ListenPort.ToString();
			cs.GetWidget<TextFieldWidget>("EXTERNAL_PORT").Text = settings.Server.ExternalPort.ToString();
			
			var onlineCheckbox = cs.GetWidget<CheckboxWidget>("CHECKBOX_ONLINE");
			onlineCheckbox.IsChecked = () => settings.Server.AdvertiseOnline;
			onlineCheckbox.OnClick = () => { settings.Server.AdvertiseOnline ^= true; settings.Save(); };
		}
	}
}
