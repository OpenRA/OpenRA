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
using OpenRA.Mods.RA;
using OpenRA.Widgets;

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
			world.WorldActor.Trait<CncMenuPaletteEffect>()
				.Fade(CncMenuPaletteEffect.EffectType.None);
			
			ingameRoot = widget.GetWidget("INGAME_ROOT");
			if (!staticSetup)
			{
				staticSetup = true;
				Game.AddChatLine += AddChatLineStub;
			}
			
			if (world.LocalPlayer != null)
				widget.GetWidget("PLAYER_WIDGETS").IsVisible = () => true;

			ingameRoot.GetWidget<ButtonWidget>("DIPLOMACY_BUTTON").IsDisabled = () => true;
			ingameRoot.GetWidget<ButtonWidget>("OPTIONS_BUTTON").OnClick = () =>
			{
				ingameRoot.IsVisible = () => false;
				Game.LoadWidget(world, "INGAME_MENU", Widget.RootWidget, new WidgetArgs()
				{
					{ "onExit", () => ingameRoot.IsVisible = () => true }
				});
			};
			
			var cheatsButton = ingameRoot.GetWidget<ButtonWidget>("CHEATS_BUTTON");
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
			world.WorldActor.Trait<CncMenuPaletteEffect>().Fade(CncMenuPaletteEffect.EffectType.Desaturated);
			
			bool hideButtons = false;
			menu.GetWidget("MENU_BUTTONS").IsVisible = () => !hideButtons;
			
			Action onQuit = () =>
			{
				Game.DisconnectOnly();
				Widget.RootWidget.RemoveChildren();
				Game.LoadShellMap();
			};
			
			Action doNothing = () => {};
			
			menu.GetWidget<ButtonWidget>("QUIT_BUTTON").OnClick = () =>
				PromptConfirmAction("Quit", "Are you sure you want to quit?", onQuit, doNothing);
			
			Action onSurrender = () => world.IssueOrder(new Order("Surrender", world.LocalPlayer.PlayerActor, false));
			var surrenderButton = menu.GetWidget<ButtonWidget>("SURRENDER_BUTTON");
			surrenderButton.IsDisabled = () => (world.LocalPlayer == null || world.LocalPlayer.WinState != WinState.Undefined);
			surrenderButton.OnClick = () =>
				PromptConfirmAction("Surrender", "Are you sure you want to surrender?", onSurrender, doNothing);
			
			menu.GetWidget<ButtonWidget>("MUSIC_BUTTON").OnClick = () =>
			{
				hideButtons = true;
				Widget.OpenWindow("MUSIC_PANEL", new WidgetArgs()
                {
					{ "onExit", () => hideButtons = false },
				});
			};
			
			menu.GetWidget<ButtonWidget>("PREFERENCES_BUTTON").OnClick = () =>
			{
				hideButtons = true;
				Widget.OpenWindow("SETTINGS_PANEL", new WidgetArgs()
                {
					{ "world", world },
					{ "onExit", () => hideButtons = false },
				});
			};
			
			menu.GetWidget<ButtonWidget>("RESUME_BUTTON").OnClick = () => 
			{
				Widget.RootWidget.RemoveChild(menu);
				world.WorldActor.Trait<CncMenuPaletteEffect>().Fade(CncMenuPaletteEffect.EffectType.None);
				onExit();
			};
		}
		
		public void PromptConfirmAction(string title, string text, Action onConfirm, Action onCancel)
		{
			var prompt = menu.GetWidget("CONFIRM_PROMPT");
			
			prompt.GetWidget<LabelWidget>("PROMPT_TITLE").GetText = () => title;
			prompt.GetWidget<LabelWidget>("PROMPT_TEXT").GetText = () => text;
			
			prompt.GetWidget<ButtonWidget>("CONFIRM_BUTTON").OnClick = () =>
			{
				prompt.IsVisible = () => false;
				onConfirm();
			};
			
			prompt.GetWidget<ButtonWidget>("CANCEL_BUTTON").OnClick = () =>
			{
				prompt.IsVisible = () => false;
				onCancel();
			};
			prompt.IsVisible = () => true;
		}
	}
}
