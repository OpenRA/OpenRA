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
		[FluentReference]
		const string Leave = "menu-ingame.leave";

		[FluentReference]
		const string AbortMission = "menu-ingame.abort";

		[FluentReference]
		const string LeaveMissionTitle = "dialog-leave-mission.title";

		[FluentReference]
		const string LeaveMissionPrompt = "dialog-leave-mission.prompt";

		[FluentReference]
		const string LeaveMissionAccept = "dialog-leave-mission.confirm";

		[FluentReference]
		const string LeaveMissionCancel = "dialog-leave-mission.cancel";

		[FluentReference]
		const string RestartButton = "menu-ingame.restart";

		[FluentReference]
		const string RestartMissionTitle = "dialog-restart-mission.title";

		[FluentReference]
		const string RestartMissionPrompt = "dialog-restart-mission.prompt";

		[FluentReference]
		const string RestartMissionAccept = "dialog-restart-mission.confirm";

		[FluentReference]
		const string RestartMissionCancel = "dialog-restart-mission.cancel";

		[FluentReference]
		const string SurrenderButton = "menu-ingame.surrender";

		[FluentReference]
		const string SurrenderTitle = "dialog-surrender.title";

		[FluentReference]
		const string SurrenderPrompt = "dialog-surrender.prompt";

		[FluentReference]
		const string SurrenderAccept = "dialog-surrender.confirm";

		[FluentReference]
		const string SurrenderCancel = "dialog-surrender.cancel";

		[FluentReference]
		const string LoadGameButton = "menu-ingame.load-game";

		[FluentReference]
		const string SaveGameButton = "menu-ingame.save-game";

		[FluentReference]
		const string MusicButton = "menu-ingame.music";

		[FluentReference]
		const string SettingsButton = "menu-ingame.settings";

		[FluentReference]
		const string ReturnToMap = "menu-ingame.return-to-map";

		[FluentReference]
		const string Resume = "menu-ingame.resume";

		[FluentReference]
		const string SaveMapButton = "menu-ingame.save-map";

		[FluentReference]
		const string ErrorMaxPlayerTitle = "dialog-error-max-player.title";

		[FluentReference("players", "max")]
		const string ErrorMaxPlayerPrompt = "dialog-error-max-player.prompt";

		[FluentReference]
		const string ErrorMaxPlayerAccept = "dialog-error-max-player.confirm";

		[FluentReference]
		const string ExitMapButton = "menu-ingame.exit-map";

		[FluentReference]
		const string ExitMapEditorTitle = "dialog-exit-map-editor.title";

		[FluentReference]
		const string ExitMapEditorPromptUnsaved = "dialog-exit-map-editor.prompt-unsaved";

		[FluentReference]
		const string ExitMapEditorPromptDeleted = "dialog-exit-map-editor.prompt-deleted";

		[FluentReference]
		const string ExitMapEditorAnywayConfirm = "dialog-exit-map-editor.confirm-anyway";

		[FluentReference]
		const string ExitMapEditorConfirm = "dialog-exit-map-editor.confirm";

		[FluentReference]
		const string PlayMapWarningTitle = "dialog-play-map-warning.title";

		[FluentReference]
		const string PlayMapWarningPrompt = "dialog-play-map-warning.prompt";

		[FluentReference]
		const string PlayMapWarningCancel = "dialog-play-map-warning.cancel";

		[FluentReference]
		const string ExitToMapEditorTitle = "dialog-exit-to-map-editor.title";

		[FluentReference]
		const string ExitToMapEditorPrompt = "dialog-exit-to-map-editor.prompt";

		[FluentReference]
		const string ExitToMapEditorConfirm = "dialog-exit-to-map-editor.confirm";

		[FluentReference]
		const string ExitToMapEditorCancel = "dialog-exit-to-map-editor.cancel";

		readonly Widget menu;
		readonly Widget buttonContainer;
		readonly ButtonWidget buttonTemplate;
		readonly int2 buttonStride;
		readonly List<ButtonWidget> buttons = [];

		readonly ModData modData;
		readonly Action onExit;
		readonly World world;
		readonly WorldRenderer worldRenderer;
		readonly MenuPostProcessEffect mpe;
		readonly bool isSinglePlayer;
		readonly bool hasError;
		bool leaving;
		bool hideMenu;

		static bool lastGameEditor = false;

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
				{ "BACK_TO_EDITOR", CreateBackToEditorButton },
				{ "RESTART", CreateRestartButton },
				{ "SURRENDER", CreateSurrenderButton },
				{ "LOAD_GAME", CreateLoadGameButton },
				{ "SAVE_GAME", CreateSaveGameButton },
				{ "MUSIC", CreateMusicButton },
				{ "SETTINGS", CreateSettingsButton },
				{ "RESUME", CreateResumeButton },
				{ "SAVE_MAP", CreateSaveMapButton },
				{ "PLAY_MAP", CreatePlayMapButton },
				{ "EXIT_EDITOR", CreateExitEditorButton }
			};

			isSinglePlayer = !world.LobbyInfo.GlobalSettings.Dedicated && world.LobbyInfo.NonBotClients.Count() == 1;

			menu = widget.Get("INGAME_MENU");
			mpe = world.WorldActor.TraitOrDefault<MenuPostProcessEffect>();
			mpe?.Fade(mpe.Info.MenuEffect);

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
					{ "hideMenu", requestHideMenu },
					{ "closeMenu", CloseMenu },
				});

				gameInfoPanel.IsVisible = () => !hideMenu;
			}
		}

		public static void OnQuit(World world)
		{
			// TODO: Create a mechanism to do things like this cleaner. Also needed for scripted missions
			if (world.Type == WorldType.Regular)
			{
				var moi = world.Map.Rules.Actors[SystemActors.Player].TraitInfoOrDefault<MissionObjectivesInfo>();
				if (moi != null)
				{
					var faction = world.LocalPlayer?.Faction.InternalName;
					Game.Sound.PlayNotification(world.Map.Rules, null, "Speech", moi.LeaveNotification, faction);
					TextNotificationsManager.AddTransientLine(null, moi.LeaveTextNotification);
				}
			}

			var iop = world.WorldActor.TraitsImplementing<IObjectivesPanel>().FirstOrDefault();
			var exitDelay = iop?.ExitDelay ?? 0;
			var mpe = world.WorldActor.TraitOrDefault<MenuPostProcessEffect>();

			// HACK: Opening up skirmish menu can mess up the OrderManager.
			if (!Game.IsCurrentWorld(world))
			{
				Game.Disconnect();
				Ui.ResetAll();
				Game.LoadShellMap();
				return;
			}

			if (mpe != null)
			{
				Game.RunAfterDelay(exitDelay, () =>
				{
					if (Game.IsCurrentWorld(world))
						mpe.Fade(MenuPostProcessEffect.EffectType.Black);
				});
				exitDelay += 40 * mpe.Info.FadeLength;
			}

			lastGameEditor = false;
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
			mpe?.Fade(MenuPostProcessEffect.EffectType.None);
			onExit();
			Ui.ResetTooltips();
		}

		ButtonWidget AddButton(string id, string label)
		{
			var button = buttonTemplate.Clone();
			var lastButton = buttons.LastOrDefault();
			if (lastButton != null)
			{
				button.Bounds.X = lastButton.Bounds.X + buttonStride.X;
				button.Bounds.Y = lastButton.Bounds.Y + buttonStride.Y;
			}

			button.Id = id;
			button.IsDisabled = () => leaving;
			var text = FluentProvider.GetMessage(label);
			button.GetText = () => text;
			buttonContainer.AddChild(button);
			buttons.Add(button);

			return button;
		}

		void CreateAbortMissionButton()
		{
			if (world.Type != WorldType.Regular)
				return;

			var button = AddButton("ABORT_MISSION", world.IsGameOver
				? FluentProvider.GetMessage(Leave)
				: FluentProvider.GetMessage(AbortMission));

			button.OnClick = () =>
			{
				hideMenu = true;

				ConfirmationDialogs.ButtonPrompt(modData,
					title: LeaveMissionTitle,
					text: LeaveMissionPrompt,
					onConfirm: () => { OnQuit(world); leaving = true; },
					confirmText: LeaveMissionAccept,
					onCancel: ShowMenu,
					cancelText: LeaveMissionCancel);
			};
		}

		void CreateRestartButton()
		{
			if (world.Type != WorldType.Regular || !isSinglePlayer)
				return;

			var iop = world.WorldActor.TraitsImplementing<IObjectivesPanel>().FirstOrDefault();
			var exitDelay = iop?.ExitDelay ?? 0;

			void OnRestart()
			{
				Ui.CloseWindow();
				if (mpe != null)
				{
					if (Game.IsCurrentWorld(world))
						mpe.Fade(MenuPostProcessEffect.EffectType.Black);
					exitDelay += 40 * mpe.Info.FadeLength;
				}

				Game.RunAfterDelay(exitDelay, Game.RestartGame);
			}

			var button = AddButton("RESTART", RestartButton);
			button.IsDisabled = () => leaving;
			button.OnClick = () =>
			{
				hideMenu = true;
				ConfirmationDialogs.ButtonPrompt(modData,
					title: RestartMissionTitle,
					text: RestartMissionPrompt,
					onConfirm: OnRestart,
					confirmText: RestartMissionAccept,
					onCancel: ShowMenu,
					cancelText: RestartMissionCancel);
			};
		}

		void CreateSurrenderButton()
		{
			if (world.Type != WorldType.Regular || isSinglePlayer || world.LocalPlayer == null)
				return;

			void OnSurrender()
			{
				world.IssueOrder(new Order("Surrender", world.LocalPlayer.PlayerActor, false));
				CloseMenu();
			}

			var button = AddButton("SURRENDER", SurrenderButton);
			button.IsDisabled = () => world.LocalPlayer.WinState != WinState.Undefined || hasError || leaving;
			button.OnClick = () =>
			{
				hideMenu = true;
				ConfirmationDialogs.ButtonPrompt(modData,
					title: SurrenderTitle,
					text: SurrenderPrompt,
					onConfirm: OnSurrender,
					confirmText: SurrenderAccept,
					onCancel: ShowMenu,
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
			var button = AddButton("RESUME", world.IsGameOver ? ReturnToMap : Resume);
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
						textArguments: ["players", playerCount, "max", MapPlayers.MaximumPlayerCount],
						onConfirm: ShowMenu,
						confirmText: ErrorMaxPlayerAccept);

					return;
				}

				Ui.OpenWindow("SAVE_MAP_PANEL", new WidgetArgs()
				{
					{ "onSave", (Action<string>)(_ => { ShowMenu(); actionManager.Modified = false; }) },
					{ "onExit", CloseMenu },
					{ "map", world.Map },
					{ "world", world },
					{ "playerDefinitions", playerDefinitions },
					{ "actorDefinitions", editorActorLayer.Save() }
				});
			};
		}

		void CreatePlayMapButton()
		{
			if (world.Type != WorldType.Editor)
				return;

			var actionManager = world.WorldActor.Trait<EditorActionManager>();
			var button = AddButton("PLAY_MAP", "Play Map");
			button.IsDisabled = () => leaving || string.IsNullOrEmpty(world.Map.Package.Name);
			button.OnClick = () =>
				{
					hideMenu = true;
					var uid = modData.MapCache.GetUpdatedMap(world.Map.Uid);
					var map = uid == null ? null : modData.MapCache[uid];
					if (map == null || (map.Visibility != MapVisibility.Lobby && map.Visibility != MapVisibility.MissionSelector))
					{
						ConfirmationDialogs.ButtonPrompt(modData,
							title: PlayMapWarningTitle,
							text: PlayMapWarningPrompt,
							onCancel: ShowMenu,
							cancelText: PlayMapWarningCancel);

						return;
					}

					ExitEditor(actionManager, () =>
					{
						lastGameEditor = true;

						Ui.CloseWindow();
						Ui.ResetTooltips();
						void CloseMenu()
						{
							mpe?.Fade(MenuPostProcessEffect.EffectType.None);
							onExit();
						}

						if (map.Visibility == MapVisibility.Lobby)
						{
							// HACK: Server lobby should be usable without a server.
							ConnectionLogic.Connect(Game.CreateLocalServer(uid),
								"",
								() => Game.OpenWindow("SERVER_LOBBY", new WidgetArgs
								{
									{ "onExit", CloseMenu },
									{ "onStart", () => { } },
									{ "skirmishMode", true }
								}),
								() => Game.CloseServer());
						}
						else if (map.Visibility == MapVisibility.MissionSelector)
						{
							Game.OpenWindow("MISSIONBROWSER_PANEL", new WidgetArgs
							{
								{ "onExit", CloseMenu },
								{ "onStart", () => { } },
								{ "initialMap", uid }
							});
						}
					});
				};
		}

		void CreateBackToEditorButton()
		{
			if (world.Type != WorldType.Regular || !lastGameEditor)
				return;

			AddButton("BACK_TO_EDITOR", "Back To Editor")
				.OnClick = () =>
				{
					hideMenu = true;
					void OnConfirm()
					{
						lastGameEditor = false;
						var map = modData.MapCache.GetUpdatedMap(world.Map.Uid);
						if (map == null)
							Game.LoadShellMap();
						else
						{
							DiscordService.UpdateStatus(DiscordState.InMapEditor);
							Game.LoadEditor(map);
						}
					}

					ConfirmationDialogs.ButtonPrompt(modData,
						title: ExitToMapEditorTitle,
						text: ExitToMapEditorPrompt,
						onConfirm: OnConfirm,
						confirmText: ExitToMapEditorConfirm,
						onCancel: ShowMenu,
						cancelText: ExitToMapEditorCancel);
				};
		}

		void CreateExitEditorButton()
		{
			if (world.Type != WorldType.Editor)
				return;

			var actionManager = world.WorldActor.Trait<EditorActionManager>();
			AddButton("EXIT_EDITOR", ExitMapButton)
				.OnClick = () => ExitEditor(actionManager, () => OnQuit(world));
		}

		void ExitEditor(EditorActionManager actionManager, Action onSuccess)
		{
			var deletedOrUnavailable = false;
			if (!string.IsNullOrEmpty(world.Map.Package.Name))
			{
				var map = modData.MapCache.GetUpdatedMap(world.Map.Uid);
				deletedOrUnavailable = map == null || modData.MapCache[map].Status != MapStatus.Available;
			}

			if (actionManager.HasUnsavedItems() || deletedOrUnavailable)
			{
				hideMenu = true;
				ConfirmationDialogs.ButtonPrompt(modData,
					title: ExitMapEditorTitle,
					text: deletedOrUnavailable ? ExitMapEditorPromptDeleted : ExitMapEditorPromptUnsaved,
					onConfirm: () => { onSuccess(); leaving = true; },
					confirmText: deletedOrUnavailable ? ExitMapEditorAnywayConfirm : ExitMapEditorConfirm,
					onCancel: ShowMenu);
			}
			else
			{
				onSuccess();
				leaving = true;
			}
		}
	}
}
