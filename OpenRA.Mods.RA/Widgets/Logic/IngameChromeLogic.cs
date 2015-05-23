#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System.Linq;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class IngameChromeLogic
	{
		readonly Widget gameRoot;
		readonly Widget playerRoot;
		readonly World world;

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
			var cachedPause = false;
			Widget optionsBG = null;
			optionsBG = Game.LoadWidget(world, "INGAME_OPTIONS_BG", Ui.Root, new WidgetArgs
			{
				{ "transient", false },
				{ "onExit", () =>
					{
						optionsBG.Visible = false;

						if (world.LobbyInfo.IsSinglePlayer)
							world.SetPauseState(cachedPause);
					}
				}
			});

			optionsBG.Visible = false;

			gameRoot.Get<ButtonWidget>("INGAME_OPTIONS_BUTTON").OnClick = () =>
			{
				optionsBG.Visible ^= true;
				if (optionsBG.Visible)
				{
					cachedPause = world.PredictedPaused;

					if (world.LobbyInfo.IsSinglePlayer)
						world.SetPauseState(true);
				}
				else
					world.SetPauseState(cachedPause);
			};

			Game.LoadWidget(world, "CHAT_PANEL", gameRoot, new WidgetArgs());
		}

		void InitObserverWidgets()
		{
			var observerWidgets = Game.LoadWidget(world, "OBSERVER_WIDGETS", playerRoot, new WidgetArgs());

			Game.LoadWidget(world, "OBSERVER_STATS", observerWidgets, new WidgetArgs());
			observerWidgets.Get<ButtonWidget>("INGAME_STATS_BUTTON").OnClick = () => gameRoot.Get("OBSERVER_STATS").Visible ^= true;
		}

		enum RadarBinState { Closed, BinAnimating, RadarAnimating, Open };
		void InitPlayerWidgets()
		{
			var playerWidgets = Game.LoadWidget(world, "PLAYER_WIDGETS", playerRoot, new WidgetArgs());

			Widget diplomacy = null;
			diplomacy = Game.LoadWidget(world, "DIPLOMACY", playerWidgets, new WidgetArgs
			{
				{ "onExit", () => diplomacy.Visible = false }
			});
			var diplomacyButton = playerWidgets.Get<ButtonWidget>("INGAME_DIPLOMACY_BUTTON");
			diplomacyButton.OnClick = () => diplomacy.Visible ^= true;
			var validPlayers = 0;
			validPlayers = world.Players.Where(a => a != world.LocalPlayer && !a.NonCombatant).Count();
			diplomacyButton.IsVisible = () => validPlayers > 0;

			Widget cheats = null;
			cheats = Game.LoadWidget(world, "INGAME_DEBUG_BG", playerWidgets, new WidgetArgs
			{
				{ "transient", true },
				{ "onExit", () => cheats.Visible = false }
			});
			cheats.Visible = false;

			var cheatsButton = playerWidgets.Get<ButtonWidget>("INGAME_DEBUG_BUTTON");
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

			var radarActive = false;
			var binState = RadarBinState.Closed;
			var radarBin = playerWidgets.Get<SlidingContainerWidget>("INGAME_RADAR_BIN");
			radarBin.IsOpen = () => radarActive || binState > RadarBinState.BinAnimating;
			radarBin.AfterOpen = () => binState = RadarBinState.RadarAnimating;
			radarBin.AfterClose = () => binState = RadarBinState.Closed;

			var radarMap = radarBin.Get<RadarWidget>("RADAR_MINIMAP");
			radarMap.IsEnabled = () => radarActive && binState >= RadarBinState.RadarAnimating;
			radarMap.AfterOpen = () => binState = RadarBinState.Open;
			radarMap.AfterClose = () => binState = RadarBinState.BinAnimating;

			radarBin.Get<ImageWidget>("RADAR_BIN_BG").GetImageCollection = () => "chrome-"+world.LocalPlayer.Country.Race;

			var powerManager = world.LocalPlayer.PlayerActor.Trait<PowerManager>();
			var powerBar = radarBin.Get<ResourceBarWidget>("POWERBAR");
			powerBar.IndicatorCollection = "power-"+world.LocalPlayer.Country.Race;
			powerBar.GetProvided = () => powerManager.PowerProvided;
			powerBar.GetUsed = () => powerManager.PowerDrained;
			powerBar.TooltipFormat = "Power Usage: {0}/{1}";
			powerBar.GetBarColor = () =>
			{
				if (powerManager.PowerState == PowerState.Critical)
					return Color.Red;
				if (powerManager.PowerState == PowerState.Low)
					return Color.Orange;
				return Color.LimeGreen;
			};

			var cachedRadarActive = false;
			var sidebarTicker = playerWidgets.Get<LogicTickerWidget>("SIDEBAR_TICKER");
			sidebarTicker.OnTick = () =>
			{
				// Update radar bin
				radarActive = world.ActorsWithTrait<ProvidesRadar>()
					.Any(a => a.Actor.Owner == world.LocalPlayer && a.Trait.IsActive);

				if (radarActive != cachedRadarActive)
					Sound.PlayNotification(world.Map.Rules, null, "Sounds", (radarActive ? "RadarUp" : "RadarDown"), null);
				cachedRadarActive = radarActive;

				// Switch to observer mode after win/loss
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
