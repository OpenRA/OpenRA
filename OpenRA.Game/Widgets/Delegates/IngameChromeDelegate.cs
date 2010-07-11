#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion
using System;
using OpenRA.FileFormats;
using OpenRA.Support;

namespace OpenRA.Widgets.Delegates
{
	public class IngameChromeDelegate : IWidgetDelegate
	{
		public IngameChromeDelegate()
		{
			var r = Chrome.rootWidget;
			var gameRoot = r.GetWidget("INGAME_ROOT");
			var optionsBG = gameRoot.GetWidget("INGAME_OPTIONS_BG");
			
			Game.OnGameStart += () => r.OpenWindow("INGAME_ROOT");

			r.GetWidget("INGAME_OPTIONS_BUTTON").OnMouseUp = mi => {
				optionsBG.Visible = !optionsBG.Visible;
				return true;
			};
			
			optionsBG.GetWidget("BUTTON_DISCONNECT").OnMouseUp = mi => {
				optionsBG.Visible = false;
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
		}
	}
}
