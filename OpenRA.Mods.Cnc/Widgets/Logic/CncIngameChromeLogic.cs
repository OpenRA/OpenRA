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
using System.Linq;
using OpenRA.Traits;
using OpenRA.Widgets;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Orders;

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
		
		public void UnregisterEvents()
		{
			Game.AddChatLine -= AddChatLine;
			Game.BeforeGameStart -= UnregisterEvents;

			if (queueTabs != null)
			{
				world.ActorAdded += queueTabs.ActorChanged;
				world.ActorRemoved += queueTabs.ActorChanged;
			}
		}
		
		void SetupProductionGroupButton(ButtonWidget button, string group)
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

			var chromeName = group.ToLowerInvariant();
			var icon = button.GetWidget<ImageWidget>("ICON");
			icon.GetImageName = () => button.IsDisabled() ? chromeName+"-disabled" :
				queueTabs.Groups[group].Alert ? chromeName+"-alert" : chromeName;
		}
		
		[ObjectCreator.UseCtor]
		public CncIngameChromeLogic([ObjectCreator.Param] Widget widget,
		                            [ObjectCreator.Param] World world )
		{
			this.world = world;
			world.WorldActor.Trait<CncMenuPaletteEffect>()
				.Fade(CncMenuPaletteEffect.EffectType.None);
			
			Game.AddChatLine += AddChatLine;
			Game.BeforeGameStart += UnregisterEvents;
			
			ingameRoot = widget.GetWidget("INGAME_ROOT");
			var playerRoot = ingameRoot.GetWidget("PLAYER_ROOT");
			
			Action onOptionsClick = () =>
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
			};

			// Observer
			if (world.LocalPlayer == null)
			{
				var observerWidgets = Game.LoadWidget(world, "OBSERVER_WIDGETS", playerRoot, new WidgetArgs());
				observerWidgets.GetWidget<ButtonWidget>("OPTIONS_BUTTON").OnClick = onOptionsClick;
			}
			else
			{
				// Real player
				var playerWidgets = Game.LoadWidget(world, "PLAYER_WIDGETS", playerRoot, new WidgetArgs());
				playerWidgets.IsVisible = () => true;

				var sidebarRoot = playerWidgets.GetWidget("SIDEBAR_BACKGROUND");

				var sellButton = sidebarRoot.GetWidget<ButtonWidget>("SELL_BUTTON");
				sellButton.OnClick = () => world.ToggleInputMode<SellOrderGenerator>();
				var sellIcon = sellButton.GetWidget<ImageWidget>("ICON");
				sellIcon.GetImageName = () => world.OrderGenerator is SellOrderGenerator ? "sell-active" : "sell";

				var repairButton = sidebarRoot.GetWidget<ButtonWidget>("REPAIR_BUTTON");
				repairButton.IsDisabled = () => !RepairOrderGenerator.PlayerIsAllowedToRepair( world );
				repairButton.OnClick = () => world.ToggleInputMode<RepairOrderGenerator>();
				var repairIcon = repairButton.GetWidget<ImageWidget>("ICON");
				repairIcon.GetImageName = () => repairButton.IsDisabled() ? "repair-disabled" :
					world.OrderGenerator is RepairOrderGenerator ? "repair-active" : "repair";

				var playerResources = world.LocalPlayer.PlayerActor.Trait<PlayerResources>();
				sidebarRoot.GetWidget<LabelWidget>("CASH_DISPLAY").GetText = () =>
					"${0}".F(playerResources.DisplayCash + playerResources.DisplayOre);

				queueTabs = playerWidgets.GetWidget<ProductionTabsWidget>("PRODUCTION_TABS");
				world.ActorAdded += queueTabs.ActorChanged;
				world.ActorRemoved += queueTabs.ActorChanged;

				var queueTypes = sidebarRoot.GetWidget("PRODUCTION_TYPES");
				SetupProductionGroupButton(queueTypes.GetWidget<ButtonWidget>("BUILDING"), "Building");
				SetupProductionGroupButton(queueTypes.GetWidget<ButtonWidget>("DEFENSE"), "Defense");
				SetupProductionGroupButton(queueTypes.GetWidget<ButtonWidget>("INFANTRY"), "Infantry");
				SetupProductionGroupButton(queueTypes.GetWidget<ButtonWidget>("VEHICLE"), "Vehicle");
				SetupProductionGroupButton(queueTypes.GetWidget<ButtonWidget>("AIRCRAFT"), "Aircraft");

				playerWidgets.GetWidget<ButtonWidget>("OPTIONS_BUTTON").OnClick = onOptionsClick;

				var cheatsButton = playerWidgets.GetWidget<ButtonWidget>("CHEATS_BUTTON");
				cheatsButton.OnClick = () =>
				{
					if (menu != MenuType.None)
						Widget.CloseWindow();

					menu = MenuType.Cheats;
					Game.OpenWindow("CHEATS_PANEL", new WidgetArgs() {{"onExit", () => menu = MenuType.None }});
				};
				cheatsButton.IsVisible = () => world.LocalPlayer != null && world.LobbyInfo.GlobalSettings.AllowCheats;

				var postgameBG = ingameRoot.GetWidget("POSTGAME_BG");
				postgameBG.IsVisible = () =>
				{
					return world.LocalPlayer != null && world.LocalPlayer.WinState != WinState.Undefined;
				};

				postgameBG.GetWidget<LabelWidget>("TEXT").GetText = () =>
				{
					var state = world.LocalPlayer.WinState;
					return (state == WinState.Undefined)? "" :
									((state == WinState.Lost)? "YOU ARE DEFEATED" : "YOU ARE VICTORIOUS");
				};
			}
		}
	}
}
