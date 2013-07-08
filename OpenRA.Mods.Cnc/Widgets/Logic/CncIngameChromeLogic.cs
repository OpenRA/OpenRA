#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System.Linq;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Orders;
using OpenRA.Mods.RA.Widgets;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class CncIngameChromeLogic
	{
		Widget ingameRoot;
		World world;

		void AddChatLine(Color c, string from, string text)
		{
			ingameRoot.Get<ChatDisplayWidget>("CHAT_DISPLAY").AddLine(c, from, text);
		}

		void UnregisterEvents()
		{
			Game.AddChatLine -= AddChatLine;
			Game.BeforeGameStart -= UnregisterEvents;
		}

		[ObjectCreator.UseCtor]
		public CncIngameChromeLogic(Widget widget, World world)
		{
			this.world = world;
			world.WorldActor.Trait<CncMenuPaletteEffect>()
				.Fade(CncMenuPaletteEffect.EffectType.None);

			Game.AddChatLine += AddChatLine;
			Game.BeforeGameStart += UnregisterEvents;

			ingameRoot = widget.Get("INGAME_ROOT");
			var playerRoot = ingameRoot.Get("PLAYER_ROOT");

			// Observer
			if (world.LocalPlayer == null)
				InitObserverWidgets(world, playerRoot);
			else
				InitPlayerWidgets(world, playerRoot);
		}

		public void OptionsClicked()
		{
			var cachedPause = world.PredictedPaused;

			ingameRoot.IsVisible = () => false;
			if (world.LobbyInfo.IsSinglePlayer)
				world.SetPauseState(true);

			Game.LoadWidget(world, "INGAME_MENU", Ui.Root, new WidgetArgs()
			{
				{ "onExit", () =>
					{
						ingameRoot.IsVisible = () => true;
						if (world.LobbyInfo.IsSinglePlayer)
							world.SetPauseState(cachedPause);
					}
				}
			});
		}

		public void InitObserverWidgets(World world, Widget playerRoot)
		{
			var observerWidgets = Game.LoadWidget(world, "OBSERVER_WIDGETS", playerRoot, new WidgetArgs());
			observerWidgets.Get<ButtonWidget>("OPTIONS_BUTTON").OnClick = OptionsClicked;
		}

		public void InitPlayerWidgets(World world, Widget playerRoot)
		{
			// Real player
			var playerWidgets = Game.LoadWidget(world, "PLAYER_WIDGETS", playerRoot, new WidgetArgs());
			playerWidgets.IsVisible = () => true;

			var sidebarRoot = playerWidgets.Get("SIDEBAR_BACKGROUND");

			BindOrderButton<SellOrderGenerator>(world, sidebarRoot, "SELL_BUTTON", "sell");
			BindOrderButton<RepairOrderGenerator>(world, sidebarRoot, "REPAIR_BUTTON", "repair");

			sidebarRoot.Get<ButtonWidget>("SELL_BUTTON").Key = Game.Settings.Keys.SellKey;
			sidebarRoot.Get<ButtonWidget>("REPAIR_BUTTON").Key = Game.Settings.Keys.RepairKey;

			var powerManager = world.LocalPlayer.PlayerActor.Trait<PowerManager>();
			var playerResources = world.LocalPlayer.PlayerActor.Trait<PlayerResources>();
			sidebarRoot.Get<LabelWidget>("CASH").GetText = () =>
				"${0}".F(playerResources.DisplayCash + playerResources.DisplayOre);

			playerWidgets.Get<ButtonWidget>("OPTIONS_BUTTON").OnClick = OptionsClicked;

			var radarEnabled = false;
			var cachedRadarEnabled = false;
			sidebarRoot.Get<RadarWidget>("RADAR_MINIMAP").IsEnabled = () => radarEnabled;

			var sidebarTicker = playerWidgets.Get<LogicTickerWidget>("SIDEBAR_TICKER");
			sidebarTicker.OnTick = () =>
			{
				// Update radar bin
				radarEnabled = world.ActorsWithTrait<ProvidesRadar>()
					.Any(a => a.Actor.Owner == world.LocalPlayer && a.Trait.IsActive);

				if (radarEnabled != cachedRadarEnabled)
					Sound.PlayNotification(null, "Sounds", (radarEnabled ? "RadarUp" : "RadarDown"), null);
				cachedRadarEnabled = radarEnabled;

				// Switch to observer mode after win/loss
				if (world.LocalPlayer.WinState != WinState.Undefined)
					Game.RunAfterTick(() =>
					{
						playerRoot.RemoveChildren();
						InitObserverWidgets(world, playerRoot);
					});
			};

			var siloBar = playerWidgets.Get<ResourceBarWidget>("SILOBAR");
			siloBar.GetProvided = () => playerResources.OreCapacity;
			siloBar.GetUsed = () => playerResources.Ore;
			siloBar.TooltipFormat = "Silo Usage: {0}/{1}";
			siloBar.GetBarColor = () => 
			{
				if (playerResources.Ore == playerResources.OreCapacity)
					return Color.Red;
				if (playerResources.Ore >= 0.8 * playerResources.OreCapacity)
					return Color.Orange;
				return Color.LimeGreen;
			};

			var powerBar = playerWidgets.Get<ResourceBarWidget>("POWERBAR");
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
		}

		static void BindOrderButton<T>(World world, Widget parent, string button, string icon)
			where T : IOrderGenerator, new()
		{
			var w = parent.Get<ButtonWidget>(button);
			w.OnClick = () => world.ToggleInputMode<T>();
			w.IsHighlighted = () => world.OrderGenerator is T;

			w.Get<ImageWidget>("ICON").GetImageName =
				() => world.OrderGenerator is T ? icon+"-active" : icon;
		}
	}
}
