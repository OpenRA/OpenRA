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
using System.Linq;
namespace OpenRA.Widgets.Delegates
{
	public class IngameChromeDelegate : IWidgetDelegate
	{
		public IngameChromeDelegate()
		{
			var r = Widget.RootWidget;
			var gameRoot = r.GetWidget("INGAME_ROOT");
			var optionsBG = gameRoot.GetWidget("INGAME_OPTIONS_BG");
			
			Game.OnGameStart += () => r.OpenWindow("INGAME_ROOT");
			Game.OnGameStart += () => gameRoot.GetWidget<RadarBinWidget>("INGAME_RADAR_BIN").SetWorld(Game.world);

			r.GetWidget("INGAME_OPTIONS_BUTTON").OnMouseUp = mi => {
				optionsBG.Visible = !optionsBG.Visible;
				return true;
			};
			
			optionsBG.GetWidget("BUTTON_DISCONNECT").OnMouseUp = mi => {
				optionsBG.Visible = false;
				if (Game.IsHost)
					Server.Server.CloseServer();
				Game.Disconnect();
				return true;
			};
			
			optionsBG.GetWidget("BUTTON_SETTINGS").OnMouseUp = mi => {
				r.OpenWindow("SETTINGS_MENU");
				return true;
			};

			optionsBG.GetWidget("BUTTON_RESUME").OnMouseUp = mi =>
			{
				optionsBG.Visible = false;
				return true;
			};

			optionsBG.GetWidget("BUTTON_SURRENDER").OnMouseUp = mi =>
			{
				Game.IssueOrder(new Order("Surrender", Game.world.LocalPlayer.PlayerActor));
				return true;
			};
			
			optionsBG.GetWidget("BUTTON_QUIT").OnMouseUp = mi => {
				Game.Exit();
				return true;
			};
			
			Game.AddChatLine += gameRoot.GetWidget<ChatDisplayWidget>("CHAT_DISPLAY").AddLine;
			
			
			var postgameBG = gameRoot.GetWidget("POSTGAME_BG");
			var postgameText = postgameBG.GetWidget<LabelWidget>("TEXT");
			postgameBG.IsVisible = () =>
			{
				return Game.world.LocalPlayer.WinState != WinState.Undefined;
			};
			
			postgameText.GetText = () =>
			{
				var state = Game.world.LocalPlayer.WinState;
				return (state == WinState.Undefined)? "" :
								((state == WinState.Lost)? "YOU ARE DEFEATED" : "YOU ARE VICTORIOUS");
			};
		}
		bool AreMutualAllies(Player a, Player b) { return a.Stances[b] == Stance.Ally && b.Stances[a] == Stance.Ally; }

	}
}
