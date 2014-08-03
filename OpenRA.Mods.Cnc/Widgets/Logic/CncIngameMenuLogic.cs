#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Widgets;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class CncIngameMenuLogic
	{
		Widget menu;

		enum PanelType { Objectives, Debug }

		[ObjectCreator.UseCtor]
		public CncIngameMenuLogic(Widget widget, World world, Action onExit, WorldRenderer worldRenderer)
		{
			var resumeDisabled = false;
			menu = widget.Get("INGAME_MENU");
			var mpe = world.WorldActor.Trait<MenuPaletteEffect>();
			mpe.Fade(MenuPaletteEffect.EffectType.Desaturated);

			menu.Get<LabelWidget>("VERSION_LABEL").Text = Game.modData.Manifest.Mod.Version;

			var hideButtons = false;
			menu.Get("MENU_BUTTONS").IsVisible = () => !hideButtons;

			// TODO: Create a mechanism to do things like this cleaner. Also needed for scripted missions
			Action onQuit = () =>
			{
				Sound.PlayNotification(world.Map.Rules, null, "Speech", "Leave", null);
				resumeDisabled = true;
				Game.RunAfterDelay(1200, () => mpe.Fade(MenuPaletteEffect.EffectType.Black));
				Game.RunAfterDelay(1200 + 40 * mpe.Info.FadeLength, () =>
				{
					Game.Disconnect();
					Ui.ResetAll();
					Game.LoadShellMap();
				});
			};

			Action doNothing = () => { };

			menu.Get<ButtonWidget>("QUIT_BUTTON").OnClick = () =>
				ConfirmationDialogs.PromptConfirmAction("Abort Mission", "Leave this game and return to the menu?", onQuit, doNothing);

			Action onSurrender = () => world.IssueOrder(new Order("Surrender", world.LocalPlayer.PlayerActor, false));
			var surrenderButton = menu.Get<ButtonWidget>("SURRENDER_BUTTON");
			surrenderButton.IsDisabled = () => (world.LocalPlayer == null || world.LocalPlayer.WinState != WinState.Undefined);
			surrenderButton.OnClick = () =>
				ConfirmationDialogs.PromptConfirmAction("Surrender", "Are you sure you want to surrender?", onSurrender, doNothing);

			menu.Get<ButtonWidget>("MUSIC_BUTTON").OnClick = () =>
			{
				hideButtons = true;
				Ui.OpenWindow("MUSIC_PANEL", new WidgetArgs()
				{
					{ "onExit", () => hideButtons = false },
					{ "world", world }
				});
			};

			menu.Get<ButtonWidget>("SETTINGS_BUTTON").OnClick = () =>
			{
				hideButtons = true;
				Ui.OpenWindow("SETTINGS_PANEL", new WidgetArgs()
				{
					{ "world", world },
					{ "worldRenderer", worldRenderer },
					{ "onExit", () => hideButtons = false },
				});
			};

			var resumeButton = menu.Get<ButtonWidget>("RESUME_BUTTON");
			resumeButton.IsDisabled = () => resumeDisabled;
			resumeButton.OnClick = () =>
			{
				Ui.CloseWindow();
				Ui.Root.RemoveChild(menu);
				world.WorldActor.Trait<MenuPaletteEffect>().Fade(MenuPaletteEffect.EffectType.None);
				onExit();
			};

			// Menu panels - ordered from lowest to highest priority
			var panelParent = Game.OpenWindow(world, "INGAME_MENU_PANEL");
			var panelType = PanelType.Objectives;
			var visibleButtons = 0;

			// Debug / Cheats panel
			var debugButton = panelParent.Get<ButtonWidget>("DEBUG_BUTTON");
			debugButton.OnClick = () => panelType = PanelType.Debug;
			debugButton.IsHighlighted = () => panelType == PanelType.Debug;

			if (world.LocalPlayer != null && world.LobbyInfo.GlobalSettings.AllowCheats)
			{
				panelType = PanelType.Debug;
				visibleButtons++;
				var debugPanel = Game.LoadWidget(world, "DEBUG_PANEL", panelParent, new WidgetArgs() { { "onExit", doNothing }, { "transient", true } });
				debugPanel.IsVisible = () => panelType == PanelType.Debug;
				debugButton.IsVisible = () => visibleButtons > 1;
			}

			// Mission objectives
			var iop = world.WorldActor.TraitsImplementing<IObjectivesPanel>().FirstOrDefault();
			var objectivesButton = panelParent.Get<ButtonWidget>("OBJECTIVES_BUTTON");
			objectivesButton.OnClick = () => panelType = PanelType.Objectives;
			objectivesButton.IsHighlighted = () => panelType == PanelType.Objectives;

			if (iop != null && iop.ObjectivesPanel != null)
			{
				panelType = PanelType.Objectives;
				visibleButtons++;
				var objectivesPanel = Game.LoadWidget(world, iop.ObjectivesPanel, panelParent, new WidgetArgs());
				objectivesPanel.IsVisible = () => panelType == PanelType.Objectives;
				objectivesButton.IsVisible = () => visibleButtons > 1;
			}
		}
	}
}
