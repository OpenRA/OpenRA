#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Scripting;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class IngameMenuLogic : ChromeLogic
	{
		[TranslationReference]
		const string Leave = "menu-ingame.leave";

		[TranslationReference]
		const string AbortMission = "menu-ingame.abort";

		[TranslationReference]
		const string LeaveMissionTitle = "dialog-leave-mission.title";

		[TranslationReference]
		const string LeaveMissionPrompt = "dialog-leave-mission.prompt";

		[TranslationReference]
		const string LeaveMissionAccept = "dialog-leave-mission.confirm";

		[TranslationReference]
		const string LeaveMissionCancel = "dialog-leave-mission.cancel";

		[TranslationReference]
		const string RestartButton = "menu-ingame.restart";

		[TranslationReference]
		const string RestartMissionTitle = "dialog-restart-mission.title";

		[TranslationReference]
		const string RestartMissionPrompt = "dialog-restart-mission.prompt";

		[TranslationReference]
		const string RestartMissionAccept = "dialog-restart-mission.confirm";

		[TranslationReference]
		const string RestartMissionCancel = "dialog-restart-mission.cancel";

		[TranslationReference]
		const string SurrenderButton = "menu-ingame.surrender";

		[TranslationReference]
		const string SurrenderTitle = "dialog-surrender.title";

		[TranslationReference]
		const string SurrenderPrompt = "dialog-surrender.prompt";

		[TranslationReference]
		const string SurrenderAccept = "dialog-surrender.confirm";

		[TranslationReference]
		const string SurrenderCancel = "dialog-surrender.cancel";

		[TranslationReference]
		const string LoadGameButton = "menu-ingame.load-game";

		[TranslationReference]
		const string SaveGameButton = "menu-ingame.save-game";

		[TranslationReference]
		const string MusicButton = "menu-ingame.music";

		[TranslationReference]
		const string SettingsButton = "menu-ingame.settings";

		[TranslationReference]
		const string ReturnToMap = "menu-ingame.return-to-map";

		[TranslationReference]
		const string Resume = "menu-ingame.resume";

		[TranslationReference]
		const string SaveMapButton = "menu-ingame.save-map";

		[TranslationReference]
		const string ErrorMaxPlayerTitle = "dialog-error-max-player.title";

		[TranslationReference("players", "max")]
		const string ErrorMaxPlayerPrompt = "dialog-error-max-player.prompt";

		[TranslationReference]
		const string ErrorMaxPlayerAccept = "dialog-error-max-player.confirm";

		[TranslationReference]
		const string ExitMapButton = "menu-ingame.exit-map";

		[TranslationReference]
		const string ExitMapEditorTitle = "dialog-exit-map-editor.title";

		[TranslationReference]
		const string ExitMapEditorPromptUnsaved = "dialog-exit-map-editor.prompt-unsaved";

		[TranslationReference]
		const string ExitMapEditorPromptDeleted = "dialog-exit-map-editor.prompt-deleted";

		[TranslationReference]
		const string ExitMapEditorAnywayConfirm = "dialog-exit-map-editor.confirm-anyway";

		[TranslationReference]
		const string ExitMapEditorConfirm = "dialog-exit-map-editor.confirm";

		readonly Widget menu;
		readonly Widget buttonContainer;
		readonly ButtonWidget buttonTemplate;
		readonly int2 buttonStride;
		readonly List<ButtonWidget> buttons = new List<ButtonWidget>();

		readonly ModData modData;
		readonly Action onExit;
		readonly World world;
		readonly WorldRenderer worldRenderer;
		readonly MenuPaletteEffect mpe;
		readonly bool isSinglePlayer;
		readonly bool hasError;
		bool leaving;
		bool hideMenu;

		[ObjectCreator.UseCtor]
		public IngameMenuLogic(Widget widget, ModData modData, World world, Action onExit, WorldRenderer worldRenderer,
			IngameInfoPanel initialPanel, Dictionary<string, MiniYaml> logicArgs)
		{
			this.modData = modData;
			this.world = world;
			this.worldRenderer = worldRenderer;
			this.onExit = onExit;

			var buttonHandlers = new Dictionary<string, Action>
			{
				{ "ABORT_MISSION", CreateAbortMissionButton },
				{ "RESTART", CreateRestartButton },
				{ "SURRENDER", CreateSurrenderButton },
				{ "LOAD_GAME", CreateLoadGameButton },
				{ "SAVE_GAME", CreateSaveGameButton },
				{ "MUSIC", CreateMusicButton },
				{ "SETTINGS", CreateSettingsButton },
				{ "RESUME", CreateResumeButton },
				{ "SAVE_MAP", CreateSaveMapButton },
				{ "EXIT_EDITOR", CreateExitEditorButton }
			};

			isSinglePlayer = !world.LobbyInfo.GlobalSettings.Dedicated && world.LobbyInfo.NonBotClients.Count() == 1;

			menu = widget.Get("INGAME_MENU");
			mpe = world.WorldActor.TraitOrDefault<MenuPaletteEffect>();
			mpe?.Fade(mpe.Info.MenuEffect);

			menu.Get<LabelWidget>("VERSION_LABEL").Text = modData.Manifest.Metadata.Version;

			buttonContainer = menu.Get("MENU_BUTTONS");
			buttonTemplate = buttonContainer.Get<ButtonWidget>("BUTTON_TEMPLATE");
			buttonContainer.RemoveChild(buttonTemplate);
			buttonContainer.IsVisible = () => !hideMenu;

			if (logicArgs.TryGetValue("ButtonStride", out var buttonStrideNode))
				buttonStride = FieldLoader.GetValue<int2>("ButtonStride", buttonStrideNode.Value);

			var scriptContext = world.WorldActor.TraitOrDefault<LuaScript>();
			hasError = scriptContext != null && scriptContext.FatalErrorOccurred;

			if (logicArgs.TryGetValue("Buttons", out var buttonsNode))
			{
				var buttonIds = FieldLoader.GetValue<string[]>("Buttons", buttonsNode.Value);
				foreach (var button in buttonIds)
					if (buttonHandlers.TryGetValue(button, out var createHandler))
						createHandler();
			}

			// Recenter the button container
			if (buttons.Count > 0)
			{
				var expand = (buttons.Count - 1) * buttonStride;
				buttonContainer.Bounds.X -= expand.X / 2;
				buttonContainer.Bounds.Y -= expand.Y / 2;
				buttonContainer.Bounds.Width += expand.X;
				buttonContainer.Bounds.Height += expand.Y;
			}

			var panelRoot = widget.GetOrNull("PANEL_ROOT");
			if (panelRoot != null && world.Type != WorldType.Editor)
			{
				Action<bool> requestHideMenu = h => hideMenu = h;
				var gameInfoPanel = Game.LoadWidget(world, "GAME_INFO_PANEL", panelRoot, new WidgetArgs()
				{
					{ "initialPanel", initialPanel },
					{ "hideMenu", requestHideMenu }
				});

				gameInfoPanel.IsVisible = () => !hideMenu;
			}
		}

		void OnQuit()
		{
			// TODO: Create a mechanism to do things like this cleaner. Also needed for scripted missions
			if (world.Type == WorldType.Regular)
			{
				var moi = world.Map.Rules.Actors[SystemActors.Player].TraitInfoOrDefault<MissionObjectivesInfo>();
				if (moi != null)
				{
					var faction = world.LocalPlayer?.Faction.InternalName;
					Game.Sound.PlayNotification(world.Map.Rules, null, "Speech", moi.LeaveNotification, faction);
					TextNotificationsManager.AddTransientLine(moi.LeaveTextNotification, null);
				}
			}

			leaving = true;

			var iop = world.WorldActor.TraitsImplementing<IObjectivesPanel>().FirstOrDefault();
			var exitDelay = iop?.ExitDelay ?? 0;
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
		}

		void ShowMenu()
		{
			hideMenu = false;
		}

		void CloseMenu()
		{
			Ui.CloseWindow();
			mpe?.Fade(MenuPaletteEffect.EffectType.None);
			onExit();
			Ui.ResetTooltips();
		}

		ButtonWidget AddButton(string id, string text)
		{
			var button = buttonTemplate.Clone() as ButtonWidget;
			var lastButton = buttons.LastOrDefault();
			if (lastButton != null)
			{
				button.Bounds.X = lastButton.Bounds.X + buttonStride.X;
				button.Bounds.Y = lastButton.Bounds.Y + buttonStride.Y;
			}

			button.Id = id;
			button.IsDisabled = () => leaving;
			var translation = modData.Translation.GetString(text);
			button.GetText = () => translation;
			buttonContainer.AddChild(button);
			buttons.Add(button);

			return button;
		}

		void CreateAbortMissionButton()
		{
			if (world.Type != WorldType.Regular)
				return;

			var button = AddButton("ABORT_MISSION", world.IsGameOver
				? modData.Translation.GetString(Leave)
				: modData.Translation.GetString(AbortMission));

			button.OnClick = () =>
			{
				hideMenu = true;

				ConfirmationDialogs.ButtonPrompt(modData,
					title: LeaveMissionTitle,
					text: LeaveMissionPrompt,
					onConfirm: OnQuit,
					onCancel: ShowMenu,
					confirmText: LeaveMissionAccept,
					cancelText: LeaveMissionCancel);
			};
		}

		void CreateRestartButton()
		{
			if (world.Type != WorldType.Regular || !isSinglePlayer)
				return;

			var iop = world.WorldActor.TraitsImplementing<IObjectivesPanel>().FirstOrDefault();
			var exitDelay = iop?.ExitDelay ?? 0;

			Action onRestart = () =>
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

			var button = AddButton("RESTART", RestartButton);
			button.IsDisabled = () => hasError || leaving;
			button.OnClick = () =>
			{
				hideMenu = true;
				ConfirmationDialogs.ButtonPrompt(modData,
					title: RestartMissionTitle,
					text: RestartMissionPrompt,
					onConfirm: onRestart,
					onCancel: ShowMenu,
					confirmText: RestartMissionAccept,
					cancelText: RestartMissionCancel);
			};
		}

		void CreateSurrenderButton()
		{
			if (world.Type != WorldType.Regular || isSinglePlayer || world.LocalPlayer == null)
				return;

			Action onSurrender = () =>
			{
				world.IssueOrder(new Order("Surrender", world.LocalPlayer.PlayerActor, false));
				CloseMenu();
			};

			var button = AddButton("SURRENDER", SurrenderButton);
			button.IsDisabled = () => world.LocalPlayer.WinState != WinState.Undefined || hasError || leaving;
			button.OnClick = () =>
			{
				hideMenu = true;
				ConfirmationDialogs.ButtonPrompt(modData,
					title: SurrenderTitle,
					text: SurrenderPrompt,
					onConfirm: onSurrender,
					onCancel: ShowMenu,
					confirmText: SurrenderAccept,
					cancelText: SurrenderCancel);
			};
		}

		void CreateLoadGameButton()
		{
			if (world.Type != WorldType.Regular || !world.LobbyInfo.GlobalSettings.GameSavesEnabled || world.IsReplay)
				return;

			var button = AddButton("LOAD_GAME", LoadGameButton);
			button.IsDisabled = () => leaving || !GameSaveBrowserLogic.IsLoadPanelEnabled(modData.Manifest);
			button.OnClick = () =>
			{
				hideMenu = true;
				Ui.OpenWindow("GAMESAVE_BROWSER_PANEL", new WidgetArgs
				{
					{ "onExit", () => hideMenu = false },
					{ "onStart", CloseMenu },
					{ "isSavePanel", false },
					{ "world", null }
				});
			};
		}

		void CreateSaveGameButton()
		{
			if (world.Type != WorldType.Regular || !world.LobbyInfo.GlobalSettings.GameSavesEnabled || world.IsReplay)
				return;

			var button = AddButton("SAVE_GAME", SaveGameButton);
			button.IsDisabled = () => hasError || leaving || !world.Players.Any(p => p.Playable && p.WinState == WinState.Undefined);
			button.OnClick = () =>
			{
				hideMenu = true;
				Ui.OpenWindow("GAMESAVE_BROWSER_PANEL", new WidgetArgs
				{
					{ "onExit", () => hideMenu = false },
					{ "onStart", () => { } },
					{ "isSavePanel", true },
					{ "world", world }
				});
			};
		}

		void CreateMusicButton()
		{
			var button = AddButton("MUSIC", MusicButton);
			button.OnClick = () =>
			{
				hideMenu = true;
				Ui.OpenWindow("MUSIC_PANEL", new WidgetArgs()
				{
					{ "onExit", () => hideMenu = false },
					{ "world", world }
				});
			};
		}

		void CreateSettingsButton()
		{
			var button = AddButton("SETTINGS", SettingsButton);
			button.OnClick = () =>
			{
				hideMenu = true;
				Ui.OpenWindow("SETTINGS_PANEL", new WidgetArgs()
				{
					{ "world", world },
					{ "worldRenderer", worldRenderer },
					{ "onExit", () => hideMenu = false },
				});
			};
		}

		void CreateResumeButton()
		{
			var button = AddButton("RESUME", world.IsGameOver ? ReturnToMap	: Resume);
			button.Key = modData.Hotkeys["escape"];
			button.OnClick = CloseMenu;
		}

		void CreateSaveMapButton()
		{
			if (world.Type != WorldType.Editor)
				return;

			var button = AddButton("SAVE_MAP", SaveMapButton);
			button.OnClick = () =>
			{
				hideMenu = true;
				var editorActorLayer = world.WorldActor.Trait<EditorActorLayer>();
				var actionManager = world.WorldActor.Trait<EditorActionManager>();

				var playerDefinitions = editorActorLayer.Players.ToMiniYaml();

				var playerCount = new MapPlayers(playerDefinitions).Players.Count;
				if (playerCount > MapPlayers.MaximumPlayerCount)
				{
					ConfirmationDialogs.ButtonPrompt(modData,
						title: ErrorMaxPlayerTitle,
						text: ErrorMaxPlayerPrompt,
						textArguments: Translation.Arguments("players", playerCount, "max", MapPlayers.MaximumPlayerCount),
						onConfirm: ShowMenu,
						confirmText: ErrorMaxPlayerAccept);

					return;
				}

				Ui.OpenWindow("SAVE_MAP_PANEL", new WidgetArgs()
				{
					{ "onSave", (Action<string>)(_ => { ShowMenu(); actionManager.Modified = false; }) },
					{ "onExit", ShowMenu },
					{ "map", world.Map },
					{ "playerDefinitions", playerDefinitions },
					{ "actorDefinitions", editorActorLayer.Save() }
				});
			};
		}

		void CreateExitEditorButton()
		{
			if (world.Type != WorldType.Editor)
				return;

			var actionManager = world.WorldActor.Trait<EditorActionManager>();
			var button = AddButton("EXIT_EDITOR", ExitMapButton);

			// Show dialog only if updated since last save
			button.OnClick = () =>
			{
				var map = modData.MapCache.GetUpdatedMap(world.Map.Uid);
				var deletedOrUnavailable = map == null || modData.MapCache[map].Status != MapStatus.Available;
				if (actionManager.HasUnsavedItems() || deletedOrUnavailable)
				{
					hideMenu = true;
					ConfirmationDialogs.ButtonPrompt(modData,
						title: ExitMapEditorTitle,
						text: deletedOrUnavailable ? ExitMapEditorPromptDeleted : ExitMapEditorPromptUnsaved,
						confirmText: deletedOrUnavailable ? ExitMapEditorAnywayConfirm : ExitMapEditorConfirm,
						onConfirm: OnQuit,
						onCancel: ShowMenu);
				}
				else
					OnQuit();
			};
		}
	}
}
