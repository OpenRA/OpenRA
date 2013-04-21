#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System.Linq;
using OpenRA.Network;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class IngameChromeLogic
	{
		Widget gameRoot;
		Widget playerRoot;
		World world;

		[ObjectCreator.UseCtor]
		public IngameChromeLogic(World world)
		{
			this.world = world;
			gameRoot = Ui.Root.Get("INGAME_ROOT");
			playerRoot = gameRoot.Get("PLAYER_ROOT");

			InitRootWidgets();
			if (world.LocalPlayer == null)
				InitObserverWidgets();
			else
				InitPlayerWidgets();
		}

		void InitRootWidgets()
		{
			Widget optionsBG = null;
			optionsBG = Game.LoadWidget(world, "INGAME_OPTIONS_BG", Ui.Root, new WidgetArgs
			{
				{ "onExit", () =>
					{
						if (world.LobbyInfo.IsSinglePlayer)
							world.IssueOrder(Order.PauseGame(false));
						optionsBG.Visible = false;
					}
				}
			});

			gameRoot.Get<ButtonWidget>("INGAME_OPTIONS_BUTTON").OnClick = () =>
			{
				optionsBG.Visible ^= true;
				if (world.LobbyInfo.IsSinglePlayer)
					world.IssueOrder(Order.PauseGame(optionsBG.Visible));
			};

			Game.LoadWidget(world, "CHAT_PANEL", gameRoot, new WidgetArgs());
		}

		void InitObserverWidgets()
		{
			var observerWidgets = Game.LoadWidget(world, "OBSERVER_WIDGETS", playerRoot, new WidgetArgs());

			Game.LoadWidget(world, "OBSERVER_STATS", observerWidgets, new WidgetArgs());
			observerWidgets.Get<ButtonWidget>("INGAME_STATS_BUTTON").OnClick = () => gameRoot.Get("OBSERVER_STATS").Visible ^= true;
		}

		void InitPlayerWidgets()
		{
			var playerWidgets = Game.LoadWidget(world, "PLAYER_WIDGETS", playerRoot, new WidgetArgs());

			Widget cheats = null;
			cheats = Game.LoadWidget(world, "CHEATS_PANEL", playerWidgets, new WidgetArgs
			{
				{ "onExit", () => cheats.Visible = false }
			});
			var cheatsButton = playerWidgets.Get<ButtonWidget>("CHEATS_BUTTON");
			cheatsButton.OnClick = () => cheats.Visible ^= true;
			cheatsButton.IsVisible = () => world.LobbyInfo.GlobalSettings.AllowCheats;

			var iop = world.WorldActor.TraitsImplementing<IObjectivesPanel>().FirstOrDefault();
			if (iop != null && iop.ObjectivesPanel != null)
			{
				var objectivesButton = playerWidgets.Get<ButtonWidget>("OBJECTIVES_BUTTON");
				var objectivesWidget = Game.LoadWidget(world, iop.ObjectivesPanel, playerWidgets, new WidgetArgs());
				objectivesButton.Visible = true;
				objectivesButton.OnClick += () => objectivesWidget.Visible ^= true;
			}

			var moneyBin = playerWidgets.Get("INGAME_MONEY_BIN");
			moneyBin.Get<OrderButtonWidget>("SELL").GetKey = _ => Game.Settings.Keys.SellKey;
			moneyBin.Get<OrderButtonWidget>("POWER_DOWN").GetKey = _ => Game.Settings.Keys.PowerDownKey;
			moneyBin.Get<OrderButtonWidget>("REPAIR").GetKey = _ => Game.Settings.Keys.RepairKey;

			var winLossWatcher = playerWidgets.Get<LogicTickerWidget>("WIN_LOSS_WATCHER");
			winLossWatcher.OnTick = () =>
			{
				if (world.LocalPlayer.WinState != WinState.Undefined)
					Game.RunAfterTick(() =>
					{
						playerRoot.RemoveChildren();
						InitObserverWidgets();
					});
			};
		}
	}
}
