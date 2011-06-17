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
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class CncIngameChromeLogic
	{
		enum MenuType { None, Diplomacy, Cheats }
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
				widget.GetWidget("PLAYER_WIDGETS").IsVisible = () => true;

			var diplomacyButton = ingameRoot.GetWidget<ButtonWidget>("DIPLOMACY_BUTTON");
			var diplomacyAvailable = world.Players.Any(a => a != world.LocalPlayer && !a.NonCombatant);
			diplomacyButton.IsDisabled = () => !diplomacyAvailable;
			diplomacyButton.OnClick = () => 
			{
				if (menu != MenuType.None)
					Widget.CloseWindow();
				
				menu = MenuType.Diplomacy;
				Game.OpenWindow("DIPLOMACY_PANEL", new WidgetArgs() {{"onExit", () => menu = MenuType.None }});
			};
			
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
				
				menu = MenuType.Diplomacy;
				Game.OpenWindow("CHEATS_PANEL", new WidgetArgs() {{"onExit", () => menu = MenuType.None }});
			};
			
			cheatsButton.IsVisible = () => world.LobbyInfo.GlobalSettings.AllowCheats;
			
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
