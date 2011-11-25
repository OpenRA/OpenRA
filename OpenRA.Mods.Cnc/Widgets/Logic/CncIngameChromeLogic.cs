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
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class CncIngameChromeLogic
	{
		enum MenuType { None, Cheats }
		MenuType menu = MenuType.None;

		Widget ingameRoot;
		ProductionTabsWidget queueTabs;
		World world;

		void AddChatLine(Color c, string from, string text)
		{
			ingameRoot.GetWidget<ChatDisplayWidget>("CHAT_DISPLAY").AddLine(c, from, text);
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
			var icon = button.GetWidget<ImageWidget>("ICON");
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

			ingameRoot = widget.GetWidget("INGAME_ROOT");
			var playerRoot = ingameRoot.GetWidget("PLAYER_ROOT");

			// Observer
			if (world.LocalPlayer == null)
				InitObserverWidgets(world, playerRoot);
			else
				InitPlayerWidgets(world, playerRoot);
		}

		public void OptionsClicked()
		{
			if (menu != MenuType.None)
			{
				Widget.CloseWindow();
				menu = MenuType.None;
			}

			ingameRoot.IsVisible = () => false;
			Game.LoadWidget(world, "INGAME_MENU", Widget.RootWidget, new WidgetArgs()
			{
				{ "onExit", () => ingameRoot.IsVisible = () => true }
			});
		}

		public void InitObserverWidgets(World world, Widget playerRoot)
		{
			var observerWidgets = Game.LoadWidget(world, "OBSERVER_WIDGETS", playerRoot, new WidgetArgs());
			observerWidgets.GetWidget<ButtonWidget>("OPTIONS_BUTTON").OnClick = OptionsClicked;
		}

		public void InitPlayerWidgets(World world, Widget playerRoot)
		{
			// Real player
			var playerWidgets = Game.LoadWidget(world, "PLAYER_WIDGETS", playerRoot, new WidgetArgs());
			playerWidgets.IsVisible = () => true;

			var sidebarRoot = playerWidgets.GetWidget("SIDEBAR_BACKGROUND");

			BindOrderButton<SellOrderGenerator>(world, sidebarRoot, "SELL_BUTTON", "sell");
			BindOrderButton<RepairOrderGenerator>(world, sidebarRoot, "REPAIR_BUTTON", "repair");

			var playerResources = world.LocalPlayer.PlayerActor.Trait<PlayerResources>();
			sidebarRoot.GetWidget<LabelWidget>("CASH_DISPLAY").GetText = () =>
				"${0}".F(playerResources.DisplayCash + playerResources.DisplayOre);

			queueTabs = playerWidgets.GetWidget<ProductionTabsWidget>("PRODUCTION_TABS");
			world.ActorAdded += queueTabs.ActorChanged;
			world.ActorRemoved += queueTabs.ActorChanged;

			var queueTypes = sidebarRoot.GetWidget("PRODUCTION_TYPES");
			SetupProductionGroupButton(queueTypes.GetWidget<ToggleButtonWidget>("BUILDING"), "Building");
			SetupProductionGroupButton(queueTypes.GetWidget<ToggleButtonWidget>("DEFENSE"), "Defense");
			SetupProductionGroupButton(queueTypes.GetWidget<ToggleButtonWidget>("INFANTRY"), "Infantry");
			SetupProductionGroupButton(queueTypes.GetWidget<ToggleButtonWidget>("VEHICLE"), "Vehicle");
			SetupProductionGroupButton(queueTypes.GetWidget<ToggleButtonWidget>("AIRCRAFT"), "Aircraft");

			playerWidgets.GetWidget<ButtonWidget>("OPTIONS_BUTTON").OnClick = OptionsClicked;

			var cheatsButton = playerWidgets.GetWidget<ButtonWidget>("CHEATS_BUTTON");
			cheatsButton.OnClick = () =>
			{
				if (menu != MenuType.None)
					Widget.CloseWindow();

				menu = MenuType.Cheats;
				Game.OpenWindow("CHEATS_PANEL", new WidgetArgs() {{"onExit", () => menu = MenuType.None }});
			};
			cheatsButton.IsVisible = () => world.LocalPlayer != null && world.LobbyInfo.GlobalSettings.AllowCheats;

			var winLossWatcher = playerWidgets.GetWidget<LogicTickerWidget>("WIN_LOSS_WATCHER");
			winLossWatcher.OnTick = () =>
			{
				if (world.LocalPlayer.WinState != WinState.Undefined)
					Game.RunAfterTick(() =>
					{
						playerRoot.RemoveChildren();
						InitObserverWidgets(world, playerRoot);
					});
			};
		}

		static void BindOrderButton<T>(World world, Widget parent, string button, string icon)
			where T : IOrderGenerator, new()
		{
			var w = parent.GetWidget<ToggleButtonWidget>(button);
			w.OnClick = () => world.ToggleInputMode<T>();
			w.IsToggled = () => world.OrderGenerator is T;

			w.GetWidget<ImageWidget>("ICON").GetImageName =
				() => world.OrderGenerator is T ? icon+"-active" : icon;
		}
	}
}
