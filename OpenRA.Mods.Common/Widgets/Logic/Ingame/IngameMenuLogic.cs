#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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
		bool hasError;
		bool leaving;
		bool hideMenu;

		[ObjectCreator.UseCtor]
		public IngameMenuLogic(Widget widget, ModData modData, World world, Action onExit, WorldRenderer worldRenderer,
			IngameInfoPanel activePanel, Dictionary<string, MiniYaml> logicArgs)
		{
			this.modData = modData;
			this.world = world;
			this.worldRenderer = worldRenderer;
			this.onExit = onExit;

			var buttonHandlers = new Dictionary<string, Action>
			{
				{ "ABORT_MISSION", CreateAbortMissionButton },
				{ "SURRENDER", CreateSurrenderButton },
				{ "LOAD_GAME", CreateLoadGameButton },
				{ "SAVE_GAME", CreateSaveGameButton },
				{ "MUSIC", CreateMusicButton },
				{ "SETTINGS", CreateSettingsButton },
				{ "RESUME", CreateResumeButton },
				{ "SAVE_MAP", CreateSaveMapButton },
				{ "EXIT_EDITOR", CreateExitEditorButton }
			};

			menu = widget.Get("INGAME_MENU");
			mpe = world.WorldActor.TraitOrDefault<MenuPaletteEffect>();
			if (mpe != null)
				mpe.Fade(mpe.Info.MenuEffect);

			menu.Get<LabelWidget>("VERSION_LABEL").Text = modData.Manifest.Metadata.Version;

			buttonContainer = menu.Get("MENU_BUTTONS");
			buttonTemplate = buttonContainer.Get<ButtonWidget>("BUTTON_TEMPLATE");
			buttonContainer.RemoveChild(buttonTemplate);
			buttonContainer.IsVisible = () => !hideMenu;

			MiniYaml buttonStrideNode;
			if (logicArgs.TryGetValue("ButtonStride", out buttonStrideNode))
				buttonStride = FieldLoader.GetValue<int2>("ButtonStride", buttonStrideNode.Value);

			var scriptContext = world.WorldActor.TraitOrDefault<LuaScript>();
			hasError = scriptContext != null && scriptContext.FatalErrorOccurred;

			MiniYaml buttonsNode;
			if (logicArgs.TryGetValue("Buttons", out buttonsNode))
			{
				Action createHandler;
				var buttonIds = FieldLoader.GetValue<string[]>("Buttons", buttonsNode.Value);
				foreach (var button in buttonIds)
					if (buttonHandlers.TryGetValue(button, out createHandler))
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
				var gameInfoPanel = Game.LoadWidget(world, "GAME_INFO_PANEL", panelRoot, new WidgetArgs()
				{
					{ "activePanel", activePanel }
				});

				gameInfoPanel.IsVisible = () => !hideMenu;
			}
		}

		void OnQuit()
		{
			// TODO: Create a mechanism to do things like this cleaner. Also needed for scripted missions
			if (world.Type == WorldType.Regular)
			{
				var moi = world.Map.Rules.Actors["player"].TraitInfoOrDefault<MissionObjectivesInfo>();
				if (moi != null)
				{
					var faction = world.LocalPlayer == null ? null : world.LocalPlayer.Faction.InternalName;
					Game.Sound.PlayNotification(world.Map.Rules, null, "Speech", moi.LeaveNotification, faction);
				}
			}

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
		}

		void ShowMenu()
		{
			hideMenu = false;
		}

		void CloseMenu()
		{
			Ui.CloseWindow();
			if (mpe != null)
				mpe.Fade(MenuPaletteEffect.EffectType.None);
			onExit();
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
			button.GetText = () => text;
			buttonContainer.AddChild(button);
			buttons.Add(button);

			return button;
		}

		void CreateAbortMissionButton()
		{
			if (world.Type != WorldType.Regular)
				return;

			var button = AddButton("ABORT_MISSION", world.IsGameOver ? "Leave" : "Abort Mission");

			button.OnClick = () =>
			{
				hideMenu = true;

				if (world.LocalPlayer == null || world.LocalPlayer.WinState != WinState.Won)
				{
					Action restartAction = null;
					var iop = world.WorldActor.TraitsImplementing<IObjectivesPanel>().FirstOrDefault();
					var exitDelay = iop != null ? iop.ExitDelay : 0;

					if (!world.LobbyInfo.GlobalSettings.Dedicated && world.LobbyInfo.NonBotClients.Count() == 1)
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
						onConfirm: OnQuit,
						onCancel: ShowMenu,
						confirmText: "Leave",
						cancelText: "Stay",
						otherText: "Restart",
						onOther: restartAction);
				}
				else
					OnQuit();
			};
		}

		void CreateSurrenderButton()
		{
			if (world.Type != WorldType.Regular || world.Map.Visibility.HasFlag(MapVisibility.MissionSelector) || world.LocalPlayer == null)
				return;

			Action onSurrender = () =>
			{
				world.IssueOrder(new Order("Surrender", world.LocalPlayer.PlayerActor, false));
				CloseMenu();
			};

			var button = AddButton("SURRENDER", "Surrender");
			button.IsDisabled = () => world.LocalPlayer.WinState != WinState.Undefined || hasError || leaving;
			button.OnClick = () =>
			{
				hideMenu = true;
				ConfirmationDialogs.ButtonPrompt(
					title: "Surrender",
					text: "Are you sure you want to surrender?",
					onConfirm: onSurrender,
					onCancel: ShowMenu,
					confirmText: "Surrender",
					cancelText: "Stay");
			};
		}

		void CreateLoadGameButton()
		{
			if (world.Type != WorldType.Regular || !world.LobbyInfo.GlobalSettings.GameSavesEnabled || world.IsReplay)
				return;

			var button = AddButton("LOAD_GAME", "Load Game");
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

			var button = AddButton("SAVE_GAME", "Save Game");
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
			var button = AddButton("MUSIC", "Music");
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
			var button = AddButton("SETTINGS", "Settings");
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
			var button = AddButton("RESUME", world.IsGameOver ? "Return to map" : "Resume");
			button.Key = modData.Hotkeys["escape"];
			button.OnClick = CloseMenu;
		}

		void CreateSaveMapButton()
		{
			if (world.Type != WorldType.Editor)
				return;

			var button = AddButton("SAVE_MAP", "Save Map");
			button.OnClick = () =>
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
		}

		void CreateExitEditorButton()
		{
			if (world.Type != WorldType.Editor)
				return;

			var button = AddButton("EXIT_EDITOR", "Exit Map Editor");
			button.OnClick = () =>
			{
				hideMenu = true;
				ConfirmationDialogs.ButtonPrompt(
					title: "Exit Map Editor",
					text: "Exit and lose all unsaved changes?",
					onConfirm: OnQuit,
					onCancel: ShowMenu);
			};
		}
	}
}
