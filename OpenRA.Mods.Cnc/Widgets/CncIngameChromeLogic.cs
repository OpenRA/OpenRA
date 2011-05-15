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
using System.Drawing;
using System.Collections.Generic;
using System;
using OpenRA.Mods.RA;

namespace OpenRA.Mods.Cnc.Widgets
{
	public class CncIngameChromeLogic : IWidgetDelegate
	{
		static bool staticSetup;
		Widget ingameRoot;

		static void AddChatLineStub(Color c, string from, string text)
		{
			var panel = Widget.RootWidget.GetWidget("INGAME_ROOT");
            if (panel == null)
                return;
			
			var handler = panel.DelegateObject as CncIngameChromeLogic;
			if (handler == null)
				return;
			
			handler.AddChatLine(c, from, text);
		}
		
		
		void AddChatLine(Color c, string from, string text)
		{
			ingameRoot.GetWidget<ChatDisplayWidget>("CHAT_DISPLAY").AddLine(c, from, text);
		}
		
		[ObjectCreator.UseCtor]
		public CncIngameChromeLogic([ObjectCreator.Param] Widget widget,
		                            [ObjectCreator.Param] World world )
		{
			ingameRoot = widget.GetWidget("INGAME_ROOT");
			if (!staticSetup)
			{
				staticSetup = true;
				Game.AddChatLine += AddChatLineStub;
			}
			
			if (world.LocalPlayer != null)
				widget.GetWidget("PLAYER_WIDGETS").IsVisible = () => true;

			ingameRoot.GetWidget<CncMenuButtonWidget>("DIPLOMACY_BUTTON").IsDisabled = () => true;
			ingameRoot.GetWidget<CncMenuButtonWidget>("OPTIONS_BUTTON").OnClick = () =>
			{
				ingameRoot.IsVisible = () => false;
				Game.LoadWidget(world, "INGAME_MENU", Widget.RootWidget, new WidgetArgs()
				{
					{ "onExit", () => ingameRoot.IsVisible = () => true }
				});
			};
			
			var cheatsButton = ingameRoot.GetWidget<CncMenuButtonWidget>("CHEATS_BUTTON");
			cheatsButton.OnClick = () => Game.OpenWindow("CHEATS_PANEL", new WidgetArgs());
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
	
	public class CncIngameMenuLogic : IWidgetDelegate
	{
		Widget menu;
		
		[ObjectCreator.UseCtor]
		public CncIngameMenuLogic([ObjectCreator.Param] Widget widget,
		                          [ObjectCreator.Param] World world,
		                          [ObjectCreator.Param] Action onExit)
		{
			menu = widget.GetWidget("INGAME_MENU");
			world.WorldActor.Trait<DesaturatedPaletteEffect>().Active = true;
			
			bool hideButtons = false;
			menu.GetWidget("MENU_BUTTONS").IsVisible = () => !hideButtons;
			
			Action onQuit = () =>
			{
				Game.DisconnectOnly();
				Widget.RootWidget.RemoveChildren();
				Game.LoadShellMap();
			};
			
			Action doNothing = () => {};
			
			menu.GetWidget<CncMenuButtonWidget>("QUIT_BUTTON").OnClick = () =>
				PromptConfirmAction("Quit", "Are you sure you want to quit?", onQuit, doNothing);
			
			Action onSurrender = () => world.IssueOrder(new Order("Surrender", world.LocalPlayer.PlayerActor, false));
			var surrenderButton = menu.GetWidget<CncMenuButtonWidget>("SURRENDER_BUTTON");
			surrenderButton.IsDisabled = () => (world.LocalPlayer == null || world.LocalPlayer.WinState != WinState.Undefined);
			surrenderButton.OnClick = () =>
				PromptConfirmAction("Surrender", "Are you sure you want to surrender?", onSurrender, doNothing);
			
			menu.GetWidget<CncMenuButtonWidget>("MUSIC_BUTTON").OnClick = () =>
			{
				hideButtons = true;
				Widget.OpenWindow("MUSIC_PANEL", new WidgetArgs()
                {
					{ "onExit", () => hideButtons = false },
				});
			};
			
			menu.GetWidget<CncMenuButtonWidget>("PREFERENCES_BUTTON").OnClick = () =>
			{
				hideButtons = true;
				Widget.OpenWindow("SETTINGS_PANEL", new WidgetArgs()
                {
					{ "world", world },
					{ "onExit", () => hideButtons = false },
				});
			};
			
			menu.GetWidget<CncMenuButtonWidget>("RESUME_BUTTON").OnClick = () => 
			{
				Widget.RootWidget.RemoveChild(menu);
				world.WorldActor.Trait<DesaturatedPaletteEffect>().Active = false;
				onExit();
			};
		}
		
		public void PromptConfirmAction(string title, string text, Action onConfirm, Action onCancel)
		{
			var prompt = menu.GetWidget("CONFIRM_PROMPT");
			
			prompt.GetWidget<LabelWidget>("PROMPT_TITLE").GetText = () => title;
			prompt.GetWidget<LabelWidget>("PROMPT_TEXT").GetText = () => text;
			
			prompt.GetWidget<CncMenuButtonWidget>("CONFIRM_BUTTON").OnClick = () =>
			{
				prompt.IsVisible = () => false;
				onConfirm();
			};
			
			prompt.GetWidget<CncMenuButtonWidget>("CANCEL_BUTTON").OnClick = () =>
			{
				prompt.IsVisible = () => false;
				onCancel();
			};
			prompt.IsVisible = () => true;
		}
	}
}
