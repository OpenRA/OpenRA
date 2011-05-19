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
using OpenRA.Mods.RA.Activities;
using System.Linq;

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

			var diplomacyButton = ingameRoot.GetWidget<ButtonWidget>("DIPLOMACY_BUTTON");
			var diplomacyAvailable = world.players.Values.Any(a => a != world.LocalPlayer && !a.NonCombatant);
			diplomacyButton.IsDisabled = () => !diplomacyAvailable;
			diplomacyButton.OnClick = () => Game.OpenWindow("DIPLOMACY_PANEL", new WidgetArgs());

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
}
