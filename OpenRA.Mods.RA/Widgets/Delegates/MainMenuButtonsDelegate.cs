#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using OpenRA.FileFormats;
using OpenRA.Network;
using OpenRA.Server;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Delegates
{
	public class MainMenuButtonsDelegate : IWidgetDelegate
	{
		[ObjectCreator.UseCtor]
		public MainMenuButtonsDelegate([ObjectCreator.Param] Widget widget)
		{
			Game.modData.WidgetLoader.LoadWidget( new Dictionary<string,object>(), Widget.RootWidget, "PERF_BG" );
			widget.GetWidget("MAINMENU_BUTTON_JOIN").OnMouseUp = mi => { Widget.OpenWindow("JOINSERVER_BG"); return true; };
			widget.GetWidget("MAINMENU_BUTTON_CREATE").OnMouseUp = mi => { Widget.OpenWindow("CREATESERVER_BG"); return true; };
			widget.GetWidget("MAINMENU_BUTTON_SETTINGS").OnMouseUp = mi => { Widget.OpenWindow("SETTINGS_MENU"); return true; };
			widget.GetWidget("MAINMENU_BUTTON_MUSIC").OnMouseUp = mi => { Widget.OpenWindow("MUSIC_MENU"); return true; };
			widget.GetWidget("MAINMENU_BUTTON_REPLAY_VIEWER").OnMouseUp = mi => { Widget.OpenWindow("REPLAYBROWSER_BG"); return true; };
			widget.GetWidget("MAINMENU_BUTTON_QUIT").OnMouseUp = mi => { Game.Exit(); return true; };
		}
	}
}
