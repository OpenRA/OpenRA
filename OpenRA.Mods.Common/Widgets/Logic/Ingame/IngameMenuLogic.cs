#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class IngameMenuLogic
	{
		Widget menu;

		[ObjectCreator.UseCtor]
		public IngameMenuLogic(Widget widget, World world, Action onExit, WorldRenderer worldRenderer, IngameInfoPanel activePanel)
		{
			var resumeDisabled = false;
			menu = widget.Get("INGAME_MENU");
			var mpe = world.WorldActor.TraitOrDefault<MenuPaletteEffect>();
			if (mpe != null)
				mpe.Fade(mpe.Info.MenuEffect);

			menu.Get<LabelWidget>("VERSION_LABEL").Text = Game.ModData.Manifest.Mod.Version;

			var hideMenu = false;
			menu.Get("MENU_BUTTONS").IsVisible = () => !hideMenu;

			// TODO: Create a mechanism to do things like this cleaner. Also needed for scripted missions
			Action onQuit = () =>
			{
				if (world.Type == WorldType.Regular)
					Sound.PlayNotification(world.Map.Rules, null, "Speech", "Leave", world.LocalPlayer == null ? null : world.LocalPlayer.Country.InternalName);

				resumeDisabled = true;

				var iop = world.WorldActor.TraitsImplementing<IObjectivesPanel>().FirstOrDefault();
				var exitDelay = iop != null ? iop.ExitDelay : 0;
				if (mpe != null)
				{
					Game.RunAfterDelay(exitDelay, () => mpe.Fade(MenuPaletteEffect.EffectType.Black));
					exitDelay += 40 * mpe.Info.FadeLength;
				}

				Game.RunAfterDelay(exitDelay, () =>
				{
					Game.Disconnect();
					Ui.ResetAll();
					Game.LoadShellMap();
				});
			};

			Action closeMenu = () =>
			{
				Ui.CloseWindow();
				if (mpe != null)
					mpe.Fade(MenuPaletteEffect.EffectType.None);
				onExit();
			};

			Action showMenu = () => hideMenu = false;

			var abortMissionButton = menu.Get<ButtonWidget>("ABORT_MISSION");
			abortMissionButton.IsVisible = () => world.Type == WorldType.Regular;
			abortMissionButton.OnClick = () =>
			{
				hideMenu = true;
				ConfirmationDialogs.PromptConfirmAction("Abort Mission", "Leave this game and return to the menu?", onQuit, showMenu);
			};

			var exitEditorButton = menu.Get<ButtonWidget>("EXIT_EDITOR");
			exitEditorButton.IsVisible = () => world.Type == WorldType.Editor;
			exitEditorButton.OnClick = () =>
			{
				hideMenu = true;
				ConfirmationDialogs.PromptConfirmAction("Exit Map Editor", "Exit and lose all unsaved changes?", onQuit, showMenu);
			};

			Action onSurrender = () =>
			{
				world.IssueOrder(new Order("Surrender", world.LocalPlayer.PlayerActor, false));
				closeMenu();
			};
			var surrenderButton = menu.Get<ButtonWidget>("SURRENDER");
			surrenderButton.IsVisible = () => world.Type == WorldType.Regular;
			surrenderButton.IsDisabled = () => (world.LocalPlayer == null || world.LocalPlayer.WinState != WinState.Undefined);
			surrenderButton.OnClick = () =>
			{
				hideMenu = true;
				ConfirmationDialogs.PromptConfirmAction("Surrender", "Are you sure you want to surrender?", onSurrender, showMenu);
			};
			surrenderButton.IsDisabled = () => world.LocalPlayer == null || world.LocalPlayer.WinState != WinState.Undefined;

			var saveMapButton = menu.Get<ButtonWidget>("SAVE_MAP");
			saveMapButton.IsVisible = () => world.Type == WorldType.Editor;
			saveMapButton.OnClick = () =>
			{
				hideMenu = true;
				var editorActorLayer = world.WorldActor.Trait<EditorActorLayer>();
				Ui.OpenWindow("SAVE_MAP_PANEL", new WidgetArgs()
				{
					{ "onSave", (Action<string>)(_ => hideMenu = false) },
					{ "onExit", () => hideMenu = false },
					{ "map", world.Map },
					{ "playerDefinitions", editorActorLayer.Players.ToMiniYaml() },
					{ "actorDefinitions", editorActorLayer.Save() }
				});
			};

			menu.Get<ButtonWidget>("MUSIC").OnClick = () =>
			{
				hideMenu = true;
				Ui.OpenWindow("MUSIC_PANEL", new WidgetArgs()
				{
					{ "onExit", () => hideMenu = false },
					{ "world", world }
				});
			};

			var settingsButton = widget.Get<ButtonWidget>("SETTINGS");
			settingsButton.OnClick = () =>
			{
				hideMenu = true;
				Ui.OpenWindow("SETTINGS_PANEL", new WidgetArgs()
				{
					{ "world", world },
					{ "worldRenderer", worldRenderer },
					{ "onExit", () => hideMenu = false },
				});
			};

			var resumeButton = menu.Get<ButtonWidget>("RESUME");
			resumeButton.IsDisabled = () => resumeDisabled;
			resumeButton.OnClick = closeMenu;

			var panelRoot = widget.GetOrNull("PANEL_ROOT");
			if (panelRoot != null && world.Type != WorldType.Editor)
			{
				var gameInfoPanel = Game.LoadWidget(world, "GAME_INFO_PANEL", panelRoot, new WidgetArgs()
				{
					{ "activePanel", activePanel }
				});

				gameInfoPanel.IsVisible = () => !hideMenu;
			}
		}
	}
}
