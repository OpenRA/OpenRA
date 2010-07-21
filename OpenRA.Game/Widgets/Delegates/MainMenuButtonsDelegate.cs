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
		public MainMenuButtonsDelegate()
		{
			// Main menu is the default window
			Widget.WindowList.Push("MAINMENU_BG");
			Widget.RootWidget.GetWidget("MAINMENU_BUTTON_QUIT").OnMouseUp = mi => { Game.Exit(); return true; };

			var version = Widget.RootWidget.GetWidget("MAINMENU_BG").GetWidget<LabelWidget>("VERSION_STRING");

			if (FileSystem.Exists("VERSION"))
			{
				var s = FileSystem.Open("VERSION");
				version.Text = s.ReadAllText();
				s.Close();
			}
		}
	}
}
