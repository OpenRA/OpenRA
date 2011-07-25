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
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Traits;
using OpenRA.Widgets;
using OpenRA.Mods.RA.Activities;

namespace OpenRA.Mods.Cnc.Widgets.Logic
{	
	public class CncIngameMenuLogic
	{
		Widget menu;

		[ObjectCreator.UseCtor]
		public CncIngameMenuLogic([ObjectCreator.Param] Widget widget,
		                          [ObjectCreator.Param] World world,
		                          [ObjectCreator.Param] Action onExit)
		{
			var resumeDisabled = false;
			menu = widget.GetWidget("INGAME_MENU");
			var mpe = world.WorldActor.Trait<CncMenuPaletteEffect>();
			mpe.Fade(CncMenuPaletteEffect.EffectType.Desaturated);
			
			menu.GetWidget<LabelWidget>("VERSION_LABEL").GetText = ActiveModVersion;

			bool hideButtons = false;
			menu.GetWidget("MENU_BUTTONS").IsVisible = () => !hideButtons;
			
			// TODO: Create a mechanism to do things like this cleaner. Also needed for scripted missions
			Action onQuit = () =>
			{
				Sound.Play("batlcon1.aud");
				resumeDisabled = true;
				Game.RunAfterDelay(1200, () => mpe.Fade(CncMenuPaletteEffect.EffectType.Black));
				Game.RunAfterDelay(1200 + 40 * mpe.Info.FadeLength, () =>
				{
						Game.Disconnect();
						Widget.ResetAll();
						Game.LoadShellMap();
				});
			};
			
			Action doNothing = () => {};
			
			menu.GetWidget<ButtonWidget>("QUIT_BUTTON").OnClick = () =>
				PromptConfirmAction("Abort Mission", "Leave this game and return to the menu?", onQuit, doNothing);
			
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
			
			var resumeButton = menu.GetWidget<ButtonWidget>("RESUME_BUTTON");
			resumeButton.IsDisabled = () => resumeDisabled;
			resumeButton.OnClick = () => 
			{
				Widget.CloseWindow();
				Widget.RootWidget.RemoveChild(menu);
				world.WorldActor.Trait<CncMenuPaletteEffect>().Fade(CncMenuPaletteEffect.EffectType.None);
				onExit();
			};

			// Mission objectives panel
			var iop = world.WorldActor.TraitsImplementing<IObjectivesPanel>().FirstOrDefault();
			if (iop != null && iop.ObjectivesPanel != null)
				Game.OpenWindow(world, iop.ObjectivesPanel);
		}

		static string ActiveModVersion()
		{
			var mod = Game.modData.Manifest.Mods[0];
			return Mod.AllMods[mod].Version;
		}

		public void PromptConfirmAction(string title, string text, Action onConfirm, Action onCancel)
		{
			var prompt = Widget.OpenWindow("CONFIRM_PROMPT");
			prompt.GetWidget<LabelWidget>("PROMPT_TITLE").GetText = () => title;
			prompt.GetWidget<LabelWidget>("PROMPT_TEXT").GetText = () => text;
			
			prompt.GetWidget<ButtonWidget>("CONFIRM_BUTTON").OnClick = () =>
			{
				Widget.CloseWindow();
				onConfirm();
			};
			
			prompt.GetWidget<ButtonWidget>("CANCEL_BUTTON").OnClick = () =>
			{
				Widget.CloseWindow();
				onCancel();
			};
		}
	}
}
