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
using OpenRA.Traits;
using OpenRA.Widgets;
using OpenRA.Mods.RA;

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class CncIngameChromeLogic
	{
		enum MenuType { None, Cheats }
		MenuType menu = MenuType.None;
		
		Widget ingameRoot;
		
		void AddChatLine(Color c, string from, string text)
		{
			ingameRoot.GetWidget<ChatDisplayWidget>("CHAT_DISPLAY").AddLine(c, from, text);
		}
		
		public void UnregisterEvents()
		{
			Game.AddChatLine -= AddChatLine;
			Game.BeforeGameStart -= UnregisterEvents;
		}
		
		ProductionQueue QueueForType(World world, string type)
		{
			return world.ActorsWithTrait<ProductionQueue>()
				.Where(p => p.Actor.Owner == world.LocalPlayer)
				.Select(p => p.Trait).FirstOrDefault(p => p.Info.Type == type);
		}
		
		[ObjectCreator.UseCtor]
		public CncIngameChromeLogic([ObjectCreator.Param] Widget widget,
		                            [ObjectCreator.Param] World world )
		{
			world.WorldActor.Trait<CncMenuPaletteEffect>()
				.Fade(CncMenuPaletteEffect.EffectType.None);
			
			Game.AddChatLine += AddChatLine;
			Game.BeforeGameStart += UnregisterEvents;
			
			ingameRoot = widget.GetWidget("INGAME_ROOT");
			
			if (world.LocalPlayer != null)
			{
				var playerWidgets = widget.GetWidget("PLAYER_WIDGETS");
				playerWidgets.IsVisible = () => true;

				var sidebarRoot = playerWidgets.GetWidget("SIDEBAR_BACKGROUND");
				var playerResources = world.LocalPlayer.PlayerActor.Trait<PlayerResources>();
				sidebarRoot.GetWidget<LabelWidget>("CASH_DISPLAY").GetText = () =>
					"${0}".F(playerResources.DisplayCash + playerResources.DisplayOre);
				
				var buildPalette = playerWidgets.GetWidget<ProductionPaletteWidget>("PRODUCTION_PALETTE");
				var queueTabs = playerWidgets.GetWidget<ProductionTabsWidget>("PRODUCTION_TABS");
				var queueTypes = sidebarRoot.GetWidget("PRODUCTION_TYPES");
				queueTypes.GetWidget<ButtonWidget>("BUILDING").OnClick = () => queueTabs.QueueType = "Building";
				queueTypes.GetWidget<ButtonWidget>("DEFENSE").OnClick = () => queueTabs.QueueType = "Defense";
				queueTypes.GetWidget<ButtonWidget>("INFANTRY").OnClick = () => queueTabs.QueueType = "Infantry";
				queueTypes.GetWidget<ButtonWidget>("VEHICLE").OnClick = () => queueTabs.QueueType = "Vehicle";
				queueTypes.GetWidget<ButtonWidget>("AIRCRAFT").OnClick = () => queueTabs.QueueType = "Aircraft";
			}
			ingameRoot.GetWidget<ButtonWidget>("OPTIONS_BUTTON").OnClick = () =>
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
			
			var cheatsButton = ingameRoot.GetWidget<ButtonWidget>("CHEATS_BUTTON");
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
