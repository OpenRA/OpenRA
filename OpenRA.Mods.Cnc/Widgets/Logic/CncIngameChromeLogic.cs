#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using OpenRA.Mods.RA.Orders;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class CncIngameChromeLogic
	{
		Widget ingameRoot;
		ProductionTabsWidget queueTabs;
		World world;

		void AddChatLine(Color c, string from, string text)
		{
			ingameRoot.Get<ChatDisplayWidget>("CHAT_DISPLAY").AddLine(c, from, text);
		}

		void UnregisterEvents()
		{
			Game.AddChatLine -= AddChatLine;
			Game.BeforeGameStart -= UnregisterEvents;

			if (queueTabs != null)
			{
				world.ActorAdded += queueTabs.ActorChanged;
				world.ActorRemoved += queueTabs.ActorChanged;
			}
		}

		void SetupProductionGroupButton(ToggleButtonWidget button, string group)
		{
			Action<bool> selectTab = reverse =>
			{
				if (queueTabs.QueueGroup == group)
					queueTabs.SelectNextTab(reverse);
				else
					queueTabs.QueueGroup = group;
			};

			button.IsDisabled = () => queueTabs.Groups[group].Tabs.Count == 0;
			button.OnMouseUp = mi => selectTab(mi.Modifiers.HasModifier(Modifiers.Shift));
			button.OnKeyPress = e => selectTab(e.Modifiers.HasModifier(Modifiers.Shift));
			button.IsToggled = () => queueTabs.QueueGroup == group;
			var chromeName = group.ToLowerInvariant();
			var icon = button.Get<ImageWidget>("ICON");
			icon.GetImageName = () => button.IsDisabled() ? chromeName+"-disabled" :
				queueTabs.Groups[group].Alert ? chromeName+"-alert" : chromeName;
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
			var cachedPause = world.Paused;

			ingameRoot.IsVisible = () => false;
			if (world.LobbyInfo.IsSinglePlayer)
				world.IssueOrder(Order.PauseGame(true));

			Game.LoadWidget(world, "INGAME_MENU", Ui.Root, new WidgetArgs()
			{
				{ "onExit", () =>
					{
						ingameRoot.IsVisible = () => true;
						if (world.LobbyInfo.IsSinglePlayer)
							world.IssueOrder(Order.PauseGame(cachedPause));
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

			sidebarRoot.Get<ToggleButtonWidget>("SELL_BUTTON").Key = Game.Settings.Keys.SellKey;
			sidebarRoot.Get<ToggleButtonWidget>("REPAIR_BUTTON").Key = Game.Settings.Keys.RepairKey;

			var powerManager = world.LocalPlayer.PlayerActor.Trait<PowerManager>();
			var playerResources = world.LocalPlayer.PlayerActor.Trait<PlayerResources>();
			sidebarRoot.Get<LabelWidget>("CASH").GetText = () =>
				"${0}".F(playerResources.DisplayCash + playerResources.DisplayOre);

			queueTabs = playerWidgets.Get<ProductionTabsWidget>("PRODUCTION_TABS");
			world.ActorAdded += queueTabs.ActorChanged;
			world.ActorRemoved += queueTabs.ActorChanged;

			var queueTypes = sidebarRoot.Get("PRODUCTION_TYPES");
			SetupProductionGroupButton(queueTypes.Get<ToggleButtonWidget>("BUILDING"), "Building");
			SetupProductionGroupButton(queueTypes.Get<ToggleButtonWidget>("DEFENSE"), "Defense");
			SetupProductionGroupButton(queueTypes.Get<ToggleButtonWidget>("INFANTRY"), "Infantry");
			SetupProductionGroupButton(queueTypes.Get<ToggleButtonWidget>("VEHICLE"), "Vehicle");
			SetupProductionGroupButton(queueTypes.Get<ToggleButtonWidget>("AIRCRAFT"), "Aircraft");

			playerWidgets.Get<ButtonWidget>("OPTIONS_BUTTON").OnClick = OptionsClicked;

			var winLossWatcher = playerWidgets.Get<LogicTickerWidget>("WIN_LOSS_WATCHER");
			winLossWatcher.OnTick = () =>
			{
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
			siloBar.RightIndicator = true;
			siloBar.GetBarColor = () => 
			{
				if (playerResources.Ore == playerResources.OreCapacity) return Color.Red;
				if (playerResources.Ore >= 0.8 * playerResources.OreCapacity) return Color.Orange;
				return Color.LimeGreen;
			};

			var powerBar = playerWidgets.Get<ResourceBarWidget>("POWERBAR");
			powerBar.GetProvided = () => powerManager.PowerProvided;
			powerBar.GetUsed = () => powerManager.PowerDrained;
			powerBar.TooltipFormat = "Power Usage: {0}/{1}";
			powerBar.RightIndicator = false;
			powerBar.GetBarColor = () => 
			{
				if (powerManager.PowerState == PowerState.Critical) return Color.Red;
				if (powerManager.PowerState == PowerState.Low) return Color.Orange;
				return Color.LimeGreen;
			};
		}

		static void BindOrderButton<T>(World world, Widget parent, string button, string icon)
			where T : IOrderGenerator, new()
		{
			var w = parent.Get<ToggleButtonWidget>(button);
			w.OnClick = () => world.ToggleInputMode<T>();
			w.IsToggled = () => world.OrderGenerator is T;

			w.Get<ImageWidget>("ICON").GetImageName =
				() => world.OrderGenerator is T ? icon+"-active" : icon;
		}
	}
}
