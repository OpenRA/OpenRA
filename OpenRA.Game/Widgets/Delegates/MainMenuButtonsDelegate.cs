#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.FileFormats;

namespace OpenRA.Widgets.Delegates
{
	public class MainMenuButtonsDelegate : IWidgetDelegate
	{
		[ObjectCreator.UseCtor]
		public MainMenuButtonsDelegate( [ObjectCreator.Param( "widget" )] Widget widget )
		{
			// Main menu is the default window
			widget.GetWidget( "MAINMENU_BUTTON_JOIN" ).OnMouseUp = mi => { Widget.OpenWindow( "JOINSERVER_BG" ); return true; };
			widget.GetWidget( "MAINMENU_BUTTON_CREATE" ).OnMouseUp = mi => { Widget.OpenWindow( "CREATESERVER_BG" ); return true; };
			widget.GetWidget( "MAINMENU_BUTTON_SETTINGS" ).OnMouseUp = mi => { Widget.OpenWindow( "SETTINGS_MENU" ); return true; };
			widget.GetWidget( "MAINMENU_BUTTON_MUSIC" ).OnMouseUp = mi => { Widget.OpenWindow( "MUSIC_MENU" ); return true; };
			widget.GetWidget( "MAINMENU_BUTTON_QUIT" ).OnMouseUp = mi => { Game.Exit(); return true; };

			var version = widget.GetWidget<LabelWidget>("VERSION_STRING");

			if (FileSystem.Exists("VERSION"))
			{
				var s = FileSystem.Open("VERSION");
				version.Text = s.ReadAllText();
				s.Close();
			}
		}
	}
}
