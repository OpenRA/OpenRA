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
using System.Linq;
using OpenRA.Widgets;
using System.Drawing;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class IngameChromeLogic
	{
		Widget gameRoot;

		[ObjectCreator.UseCtor]
		public IngameChromeLogic(World world)
		{
			Game.AddChatLine += AddChatLine;
			Game.BeforeGameStart += UnregisterEvents;

			var r = Ui.Root;
			gameRoot = r.Get("INGAME_ROOT");
			var optionsBG = gameRoot.Get("INGAME_OPTIONS_BG");

			r.Get<ButtonWidget>("INGAME_OPTIONS_BUTTON").OnClick = () =>
			{
				optionsBG.Visible = !optionsBG.Visible;
				if (world.LobbyInfo.IsSinglePlayer)
					world.IssueOrder(Order.PauseGame());
			};
			
			var cheatsButton = gameRoot.Get<ButtonWidget>("CHEATS_BUTTON");
			cheatsButton.OnClick = () =>
			{
				Game.OpenWindow("CHEATS_PANEL", new WidgetArgs() {{"onExit", () => {} }});
			};
			cheatsButton.IsVisible = () => world.LocalPlayer != null && world.LobbyInfo.GlobalSettings.AllowCheats;

			var iop = world.WorldActor.TraitsImplementing<IObjectivesPanel>().FirstOrDefault();
			if (iop != null && iop.ObjectivesPanel != null)
			{
				var objectivesButton = gameRoot.Get<ButtonWidget>("OBJECTIVES_BUTTON");
				var objectivesWidget = Game.LoadWidget(world, iop.ObjectivesPanel, Ui.Root, new WidgetArgs());
				objectivesWidget.Visible = false;
				objectivesButton.OnClick += () => objectivesWidget.Visible = !objectivesWidget.Visible;
				objectivesButton.IsVisible = () => world.LocalPlayer != null;
			}

			var moneybin = gameRoot.Get("INGAME_MONEY_BIN");
			moneybin.Get<OrderButtonWidget>("SELL").Key = Game.Settings.Keys.SellKey;
			moneybin.Get<OrderButtonWidget>("POWER_DOWN").Key = Game.Settings.Keys.PowerDownKey;
			moneybin.Get<OrderButtonWidget>("REPAIR").Key = Game.Settings.Keys.RepairKey;

			optionsBG.Get<ButtonWidget>("DISCONNECT").OnClick = () => LeaveGame(optionsBG, world);

			optionsBG.Get<ButtonWidget>("SETTINGS").OnClick = () => Ui.OpenWindow("SETTINGS_MENU");
			optionsBG.Get<ButtonWidget>("MUSIC").OnClick = () => Ui.OpenWindow("MUSIC_MENU");
			optionsBG.Get<ButtonWidget>("RESUME").OnClick = () =>
			{
				optionsBG.Visible = false;
				if (world.LobbyInfo.IsSinglePlayer)
					world.IssueOrder(Order.PauseGame());
			};

			optionsBG.Get<ButtonWidget>("SURRENDER").OnClick = () =>
			{
				optionsBG.Visible = false;
				world.IssueOrder(new Order("Surrender", world.LocalPlayer.PlayerActor, false));
			};

			optionsBG.Get("SURRENDER").IsVisible = () => (world.LocalPlayer != null && world.LocalPlayer.WinState == WinState.Undefined);

			var postgameBG = gameRoot.Get("POSTGAME_BG");
			var postgameText = postgameBG.Get<LabelWidget>("TEXT");
			var postGameObserve = postgameBG.Get<ButtonWidget>("POSTGAME_OBSERVE");

			var postgameQuit = postgameBG.Get<ButtonWidget>("POSTGAME_QUIT");
			postgameQuit.OnClick = () => LeaveGame(postgameQuit, world);

			postGameObserve.OnClick = () => postgameQuit.Visible = false;
			postGameObserve.IsVisible = () => world.LocalPlayer.WinState != WinState.Won;

			postgameBG.IsVisible = () =>
			{
				return postgameQuit.Visible && world.LocalPlayer != null && world.LocalPlayer.WinState != WinState.Undefined;
			};


			postgameText.GetText = () =>
			{
				var state = world.LocalPlayer.WinState;
				return state == WinState.Undefined ? "" :
								(state == WinState.Lost ? "YOU ARE DEFEATED" : "YOU ARE VICTORIOUS");
			};
		}

		void LeaveGame(Widget pane, World world)
		{
			Sound.PlayNotification(null, "Speech", "Leave", world.LocalPlayer.Country.Race);
			pane.Visible = false;
			Game.Disconnect();
			Game.LoadShellMap();
			Ui.CloseWindow();
			Ui.OpenWindow("MAINMENU_BG");
		}

		void UnregisterEvents()
		{
			Game.AddChatLine -= AddChatLine;
			Game.BeforeGameStart -= UnregisterEvents;
		}

		void AddChatLine(Color c, string from, string text)
		{
			gameRoot.Get<ChatDisplayWidget>("CHAT_DISPLAY").AddLine(c, from, text);
		}
	}
}
