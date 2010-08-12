#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

namespace OpenRA.Widgets.Delegates
{
	public class VideoPlayerDelegate : IWidgetDelegate
	{
		public VideoPlayerDelegate()
		{
			var bg = Widget.RootWidget.GetWidget("VIDEOPLAYER_MENU");
			var player = bg.GetWidget<VqaPlayerWidget>("VIDEOPLAYER");
			bg.GetWidget("BUTTON_PLAY").OnMouseUp = mi =>
			{
				player.Load("foo.vqa");
				player.Play();
				return true;
			};
			
			bg.GetWidget("BUTTON_STOP").OnMouseUp = mi =>
			{
				player.Stop();
				return true;
			};
		
			bg.GetWidget("BUTTON_CLOSE").OnMouseUp = mi => {
				player.Stop();
				Widget.RootWidget.CloseWindow();
				return true;
			};
			
			// Menu Buttons
			Widget.RootWidget.GetWidget("MAINMENU_BUTTON_VIDEOPLAYER").OnMouseUp = mi => {
				Widget.RootWidget.OpenWindow("VIDEOPLAYER_MENU");
				return true;
			};
		}
	}
}
