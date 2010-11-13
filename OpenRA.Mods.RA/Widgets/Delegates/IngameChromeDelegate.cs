#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Delegates
{
	public class IngameChromeDelegate : IWidgetDelegate
	{
		[ObjectCreator.UseCtor]
		public IngameChromeDelegate( [ObjectCreator.Param] World world )
		{
			var r = Widget.RootWidget;
			var gameRoot = r.GetWidget("INGAME_ROOT");
			var optionsBG = gameRoot.GetWidget("INGAME_OPTIONS_BG");
			
			r.GetWidget("INGAME_OPTIONS_BUTTON").OnMouseUp = mi => {
				optionsBG.Visible = !optionsBG.Visible;
				return true;
			};
			
			optionsBG.GetWidget("DISCONNECT").OnMouseUp = mi => {
				optionsBG.Visible = false;
				Game.Disconnect();
				return true;
			};
			
			optionsBG.GetWidget("SETTINGS").OnMouseUp = mi => {
				Widget.OpenWindow("SETTINGS_MENU");
				return true;
			};

			optionsBG.GetWidget("MUSIC").OnMouseUp = mi => {
				Widget.OpenWindow("MUSIC_MENU");
				return true;
			};
			
			optionsBG.GetWidget("RESUME").OnMouseUp = mi =>
			{
				optionsBG.Visible = false;
				return true;
			};

			optionsBG.GetWidget("SURRENDER").OnMouseUp = mi =>
			{
				world.IssueOrder(new Order("Surrender", world.LocalPlayer.PlayerActor, false));
				return true;
			};
			
			optionsBG.GetWidget("QUIT").OnMouseUp = mi => {
				Game.Exit();
				return true;
			};
			
			Game.AddChatLine += gameRoot.GetWidget<ChatDisplayWidget>("CHAT_DISPLAY").AddLine;
			
			
			var postgameBG = gameRoot.GetWidget("POSTGAME_BG");
			var postgameText = postgameBG.GetWidget<LabelWidget>("TEXT");
			postgameBG.IsVisible = () =>
			{
				return world.LocalPlayer != null && world.LocalPlayer.WinState != WinState.Undefined;
			};
			
			postgameText.GetText = () =>
			{
				var state = world.LocalPlayer.WinState;
				return (state == WinState.Undefined)? "" :
								((state == WinState.Lost)? "YOU ARE DEFEATED" : "YOU ARE VICTORIOUS");
			};
		}
	}
}
