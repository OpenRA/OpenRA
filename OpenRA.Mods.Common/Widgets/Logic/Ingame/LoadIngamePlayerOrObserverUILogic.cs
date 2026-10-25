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

using System.Linq;
using OpenRA.Mods.Common.Scripting;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class LoadIngamePlayerOrObserverUILogic : ChromeLogic
	{
		bool loadingObserverWidgets = false;

		[ObjectCreator.UseCtor]
		public LoadIngamePlayerOrObserverUILogic(Widget widget, World world)
		{
			var ingameRoot = widget.Get("INGAME_ROOT");
			var worldRoot = ingameRoot.Get("WORLD_ROOT");
			var menuRoot = ingameRoot.Get("MENU_ROOT");
			var playerRoot = worldRoot.Get("PLAYER_ROOT");

			if (world.LocalPlayer == null)
				Game.LoadWidget(world, "OBSERVER_WIDGETS", playerRoot, []);
			else
			{
				var playerWidgets = Game.LoadWidget(world, "PLAYER_WIDGETS", playerRoot, []);
				var sidebarTicker = playerWidgets.Get<LogicTickerWidget>("SIDEBAR_TICKER");
				var objectives = world.LocalPlayer.PlayerActor.Info.TraitInfoOrDefault<MissionObjectivesInfo>();

				sidebarTicker.OnTick = () =>
				{
					// Switch to observer mode after win/loss
					if (world.LocalPlayer.WinState != WinState.Undefined && !loadingObserverWidgets)
					{
						loadingObserverWidgets = true;
						Game.RunAfterDelay(objectives?.GameOverDelay ?? 0, () =>
						{
							if (!Game.IsCurrentWorld(world))
								return;

							playerRoot.RemoveChildren();
							Game.LoadWidget(world, "OBSERVER_WIDGETS", playerRoot, []);
						});
					}
				};
			}

			Game.LoadWidget(world, "DEBUG_WIDGETS", worldRoot, []);
			Game.LoadWidget(world, "TRANSIENTS_PANEL", worldRoot, []);

			world.GameOver += () =>
			{
				Ui.CloseWindow();
				menuRoot.RemoveChildren();

				void OpenMenu()
				{
					var optionsButton = playerRoot.GetOrNull<MenuButtonWidget>("OPTIONS_BUTTON");
					if (optionsButton != null)
						Sync.RunUnsynced(world, optionsButton.OnClick);
				}

				if (world.LocalPlayer != null)
				{
					var script = world.WorldActor.TraitOrDefault<LuaScript>();
					var missionData = world.WorldActor.Info.TraitInfoOrDefault<MissionDataInfo>();
					if (missionData != null && !(script != null && script.FatalErrorOccurred))
					{
						var won = world.LocalPlayer.WinState == WinState.Won;
						var video = won ? missionData.WinVideo : missionData.LossVideo;

						// Opt-in campaign flow. Leave multiplayer and replay presentation unchanged.
						if (!string.IsNullOrEmpty(missionData.GameOverScreen) && !world.IsReplay
							&& world.LobbyInfo.NonBotClients.Count() == 1)
						{
							void ShowResults()
							{
								if (!Game.IsCurrentWorld(world))
									return;

								var dismissed = false;
								void Dismiss(bool continueCampaign)
								{
									if (dismissed || !Game.IsCurrentWorld(world))
										return;

									dismissed = true;
									Ui.CloseWindow();
									if (continueCampaign && !string.IsNullOrEmpty(missionData.GameOverCallback)
										&& script?.Context?.TryInvokeGlobal(missionData.GameOverCallback, won) == true)
										return;

									OpenMenu();
								}

								Sync.RunUnsynced(world, () => Game.OpenWindow(missionData.GameOverScreen, new WidgetArgs()
								{
									{ "onContinue", () => Dismiss(true) },
									{ "onExit", () => Dismiss(false) }
								}));
							}

							if (!string.IsNullOrEmpty(video))
								Media.PlayFMVFullscreen(world, video, ShowResults);
							else
								ShowResults();

							return;
						}

						if (!string.IsNullOrEmpty(video))
							Media.PlayFMVFullscreen(world, video, () => { });
					}
				}

				OpenMenu();
			};
		}
	}
}
