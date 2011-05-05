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
			var mainButtons = widget.GetWidget("MAIN_BUTTONS");
			mainButtons.IsVisible = () => Menu == MenuType.Main;
			
			mainButtons.GetWidget("MULTIPLAYER_BUTTON").OnMouseUp = mi => { Menu = MenuType.Multiplayer; return true; };
			mainButtons.GetWidget("QUIT_BUTTON").OnMouseUp = mi => { Game.Exit(); return true; };
			
			// Multiplayer menu
			var multiplayerButtons = widget.GetWidget("MULTIPLAYER_BUTTONS");
			multiplayerButtons.IsVisible = () => Menu == MenuType.Multiplayer;
			
			multiplayerButtons.GetWidget("BACK_BUTTON").OnMouseUp = mi => { Menu = MenuType.Main; return true; };
		}
	}
}
