#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;
using OpenRA.Widgets;
using OpenRA.Network;
using System;
using System.Drawing;
using System.Linq;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class IngameObserverChromeLogic
	{
		Widget gameRoot;

		// WTF duplication
		[ObjectCreator.UseCtor]
		public IngameObserverChromeLogic(World world)
		{
			var r = Ui.Root;
			gameRoot = r.Get("OBSERVER_ROOT");
			var optionsBG = gameRoot.Get("INGAME_OPTIONS_BG");

			r.Get<ButtonWidget>("INGAME_OPTIONS_BUTTON").OnClick = () =>
			{
				optionsBG.Visible = !optionsBG.Visible;
				if (world.LobbyInfo.IsSinglePlayer)
					world.IssueOrder(Order.PauseGame(true));
			};

			optionsBG.Get<ButtonWidget>("DISCONNECT").OnClick = () =>
			{
				optionsBG.Visible = false;
				Game.Disconnect();
				Game.LoadShellMap();
				Ui.CloseWindow();
				Ui.OpenWindow("MAINMENU_BG");
			};

			optionsBG.Get<ButtonWidget>("SETTINGS").OnClick = () => Ui.OpenWindow("SETTINGS_MENU");
			optionsBG.Get<ButtonWidget>("MUSIC").OnClick = () => Ui.OpenWindow("MUSIC_MENU");
			optionsBG.Get<ButtonWidget>("RESUME").OnClick = () =>
			{
				optionsBG.Visible = false;
				if (world.LobbyInfo.IsSinglePlayer)
					world.IssueOrder(Order.PauseGame(false));
			};
			optionsBG.Get<ButtonWidget>("SURRENDER").IsVisible = () => false;

			Ui.Root.Get<ButtonWidget>("INGAME_STATS_BUTTON").OnClick = () => gameRoot.Get("OBSERVER_STATS").Visible ^= true;

			if (!world.IsShellmap)
			{
				var chatPanel = Game.LoadWidget(world, "CHAT_PANEL", Ui.Root, new WidgetArgs());
				gameRoot.AddChild(chatPanel);
			}
		}
	}
}
