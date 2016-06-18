#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Scripting;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class IngameMenuLogic : ChromeLogic
	{
		Widget menu;

		[ObjectCreator.UseCtor]
		public IngameMenuLogic(Widget widget, ModData modData, World world, Action onExit, WorldRenderer worldRenderer, IngameInfoPanel activePanel)
		{
			var leaving = false;
			menu = widget.Get("INGAME_MENU");
			var mpe = world.WorldActor.TraitOrDefault<MenuPaletteEffect>();
			if (mpe != null)
				mpe.Fade(mpe.Info.MenuEffect);

			menu.Get<LabelWidget>("VERSION_LABEL").Text = modData.Manifest.Mod.Version;

			var hideMenu = false;
			menu.Get("MENU_BUTTONS").IsVisible = () => !hideMenu;

			var scriptContext = world.WorldActor.TraitOrDefault<LuaScript>();
			var hasError = scriptContext != null && scriptContext.FatalErrorOccurred;

			// TODO: Create a mechanism to do things like this cleaner. Also needed for scripted missions
			Action onQuit = () =>
			{
				if (world.Type == WorldType.Regular)
					Game.Sound.PlayNotification(world.Map.Rules, null, "Speech", "Leave", world.LocalPlayer == null ? null : world.LocalPlayer.Faction.InternalName);

				leaving = true;

				var iop = world.WorldActor.TraitsImplementing<IObjectivesPanel>().FirstOrDefault();
				var exitDelay = iop != null ? iop.ExitDelay : 0;
				if (mpe != null)
				{
					Game.RunAfterDelay(exitDelay, () =>
					{
						if (Game.IsCurrentWorld(world))
							mpe.Fade(MenuPaletteEffect.EffectType.Black);
					});
					exitDelay += 40 * mpe.Info.FadeLength;
				}

				Game.RunAfterDelay(exitDelay, () =>
				{
					if (!Game.IsCurrentWorld(world))
						return;

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
			abortMissionButton.IsDisabled = () => leaving;
			if (world.IsGameOver)
				abortMissionButton.GetText = () => "Leave";

			abortMissionButton.OnClick = () =>
			{
				hideMenu = true;

				if (world.LocalPlayer == null || world.LocalPlayer.WinState != WinState.Won)
				{
					Action restartAction = null;
					var iop = world.WorldActor.TraitsImplementing<IObjectivesPanel>().FirstOrDefault();
					var exitDelay = iop != null ? iop.ExitDelay : 0;

					if (world.LobbyInfo.IsSinglePlayer)
					{
						restartAction = () =>
						{
							Ui.CloseWindow();
							if (mpe != null)
							{
								if (Game.IsCurrentWorld(world))
									mpe.Fade(MenuPaletteEffect.EffectType.Black);
								exitDelay += 40 * mpe.Info.FadeLength;
							}

							Game.RunAfterDelay(exitDelay, Game.RestartGame);
						};
					}

					ConfirmationDialogs.ButtonPrompt(
						title: "Leave Mission",
						text: "Leave this game and return to the menu?",
						onConfirm: onQuit,
						onCancel: showMenu,
						confirmText: "Leave",
						cancelText: "Stay",
						otherText: "Restart",
						onOther: restartAction);
				}
				else
					onQuit();
			};

			var exitEditorButton = menu.Get<ButtonWidget>("EXIT_EDITOR");
			exitEditorButton.IsVisible = () => world.Type == WorldType.Editor;
			exitEditorButton.OnClick = () =>
			{
				hideMenu = true;
				ConfirmationDialogs.ButtonPrompt(
					title: "Exit Map Editor",
					text: "Exit and lose all unsaved changes?",
					onConfirm: onQuit,
					onCancel: showMenu);
			};

			Action onSurrender = () =>
			{
				world.IssueOrder(new Order("Surrender", world.LocalPlayer.PlayerActor, false));
				closeMenu();
			};
			var surrenderButton = menu.Get<ButtonWidget>("SURRENDER");
			surrenderButton.IsVisible = () => world.Type == WorldType.Regular;
			surrenderButton.IsDisabled = () =>
				world.LocalPlayer == null || world.LocalPlayer.WinState != WinState.Undefined ||
				world.Map.Visibility.HasFlag(MapVisibility.MissionSelector) || hasError;
			surrenderButton.OnClick = () =>
			{
				hideMenu = true;
				ConfirmationDialogs.ButtonPrompt(
					title: "Surrender",
					text: "Are you sure you want to surrender?",
					onConfirm: onSurrender,
					onCancel: showMenu,
					confirmText: "Surrender",
					cancelText: "Stay");
			};

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

			var musicButton = menu.Get<ButtonWidget>("MUSIC");
			musicButton.IsDisabled = () => leaving;
			musicButton.OnClick = () =>
			{
				hideMenu = true;
				Ui.OpenWindow("MUSIC_PANEL", new WidgetArgs()
				{
					{ "onExit", () => hideMenu = false },
					{ "world", world }
				});
			};

			var settingsButton = widget.Get<ButtonWidget>("SETTINGS");
			settingsButton.IsDisabled = () => leaving;
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
			resumeButton.IsDisabled = () => leaving;
			if (world.IsGameOver)
				resumeButton.GetText = () => "Return to map";

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
