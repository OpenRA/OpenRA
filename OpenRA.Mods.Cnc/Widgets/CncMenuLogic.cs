#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.FileFormats;
using OpenRA.Network;
using OpenRA.Server;
using OpenRA.Widgets;
using System;
using System.Drawing;
using System.Linq;
using OpenRA.GameRules;

namespace OpenRA.Mods.RA.Widgets.Delegates
{
	public class CncMenuLogic : IWidgetDelegate
	{
		enum MenuType
		{
			Main,
			Multiplayer,
			Settings
		}
		MenuType Menu = MenuType.Main;
		
		[ObjectCreator.UseCtor]
		public CncMenuLogic([ObjectCreator.Param] Widget widget)
		{
			// Root level menu
			var mainMenu = widget.GetWidget("MAIN_MENU");
			mainMenu.IsVisible = () => Menu == MenuType.Main;
			
			mainMenu.GetWidget("SOLO_BUTTON").OnMouseUp = mi => { StartSkirmishGame(); return true; };
			mainMenu.GetWidget("MULTIPLAYER_BUTTON").OnMouseUp = mi => { Menu = MenuType.Multiplayer; return true; };
			mainMenu.GetWidget("SETTINGS_BUTTON").OnMouseUp = mi => { Menu = MenuType.Settings; return true; };
			mainMenu.GetWidget("QUIT_BUTTON").OnMouseUp = mi => { Game.Exit(); return true; };
			
			// Multiplayer menu
			var multiplayerMenu = widget.GetWidget("MULTIPLAYER_MENU");
			multiplayerMenu.IsVisible = () => Menu == MenuType.Multiplayer;
			
			multiplayerMenu.GetWidget("BACK_BUTTON").OnMouseUp = mi => { Menu = MenuType.Main; return true; };
			
			// Settings menu
			var settingsMenu = widget.GetWidget("SETTINGS_MENU");
			settingsMenu.IsVisible = () => Menu == MenuType.Settings;
			
			settingsMenu.GetWidget("BACK_BUTTON").OnMouseUp = mi => { Menu = MenuType.Main; return true; };
		}
		
		
		void StartSkirmishGame()
		{
			var map = Game.modData.AvailableMaps.FirstOrDefault(m => m.Value.Selectable).Key;
			
			var settings = Game.Settings;
			settings.Server.Name = "Skirmish Game";
			// TODO: we want to prevent binding a port altogether
			settings.Server.ListenPort = 1234;
			settings.Server.ExternalPort = 1234;
			Game.CreateAndJoinServer(settings, map);
		}
	}
}
