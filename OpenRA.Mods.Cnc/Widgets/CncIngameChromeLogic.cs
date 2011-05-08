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

namespace OpenRA.Mods.Cnc.Widgets
{
	public class CncIngameChromeLogic : IWidgetDelegate
	{
		static bool staticSetup;
		Widget ingameRoot;
		
		public static CncIngameChromeLogic GetHandler()
		{
			var panel = Widget.RootWidget.GetWidget("INGAME_ROOT");
            if (panel == null)
                return null;
			
			return panel.DelegateObject as CncIngameChromeLogic;
		}
		
		static void AddChatLineStub(Color c, string from, string text)
		{
			var handler = GetHandler();
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
			
			ingameRoot.GetWidget<CncMenuButtonWidget>("OPTIONS_BUTTON").OnClick = () =>
			{
				ingameRoot.IsVisible = () => false;
				var onExit = new Action(() => {ingameRoot.IsVisible = () => true;});
				Widget.LoadWidget("INGAME_MENU", new Dictionary<string, object>() {{ "world", world }, { "onExit", onExit }});
			};
			
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
			var onQuit = (Action)(() =>
			{
				Game.DisconnectOnly();
				Widget.RootWidget.RemoveChildren();
				Game.LoadShellMap();
			});
			
			var doNothing = (Action)(() => {});
			
			menu.GetWidget<CncMenuButtonWidget>("QUIT_BUTTON").OnClick = () =>
				PromptConfirmAction("Exit Game", "Are you sure you want to leave the game?", onQuit, doNothing);
			
			var onSurrender = (Action)(() => world.IssueOrder(new Order("Surrender", world.LocalPlayer.PlayerActor, false)));
			var surrenderButton = menu.GetWidget<CncMenuButtonWidget>("SURRENDER_BUTTON");
			surrenderButton.IsDisabled = () => (world.LocalPlayer == null || world.LocalPlayer.WinState != WinState.Undefined);
			surrenderButton.OnClick = () =>
				PromptConfirmAction("Surrender", "Are you sure you want to surrender?", onSurrender, doNothing);
			
			menu.GetWidget<CncMenuButtonWidget>("RESUME_BUTTON").OnClick = () => 
			{
				Widget.RootWidget.RemoveChild(menu);
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
