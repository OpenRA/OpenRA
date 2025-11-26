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

				// Handle AI Battle game over - show results immediately
				if (AIBattleState.IsAIBattle)
				{
					// Give a short delay so the user can see the final state
					Game.RunAfterDelay(1500, () =>
					{
						if (!Game.IsCurrentWorld(world))
							return;

						// Capture results before leaving
						CaptureAIBattleResults(world);

						// Disconnect and show results
						Game.Disconnect();
						Ui.ResetAll();
						ShowAIBattleResults();
					});
					return;
				}

				if (world.LocalPlayer != null)
				{
					var scriptContext = world.WorldActor.TraitOrDefault<LuaScript>();
					var missionData = world.WorldActor.Info.TraitInfoOrDefault<MissionDataInfo>();
					if (missionData != null && !(scriptContext != null && scriptContext.FatalErrorOccurred))
					{
						var video = world.LocalPlayer.WinState == WinState.Won ? missionData.WinVideo : missionData.LossVideo;
						if (!string.IsNullOrEmpty(video))
							Media.PlayFMVFullscreen(world, video, () => { });
					}
				}

				var optionsButton = playerRoot.GetOrNull<MenuButtonWidget>("OPTIONS_BUTTON");
				if (optionsButton != null)
					Sync.RunUnsynced(world, optionsButton.OnClick);
			};
		}

		static void CaptureAIBattleResults(World world)
		{
			var results = new AIBattleResults
			{
				DurationTicks = world.WorldTick,
				Timestep = world.Timestep
			};

			foreach (var player in world.Players.Where(p => p.IsBot && !p.NonCombatant))
			{
				var stats = player.PlayerActor.TraitOrDefault<PlayerStatistics>();
				var resources = player.PlayerActor.TraitOrDefault<PlayerResources>();

				var playerStats = new AIPlayerStats
				{
					Name = player.ResolvedPlayerName,
					Faction = player.Faction.InternalName,
					Team = world.LobbyInfo.ClientWithIndex(player.ClientIndex)?.Team ?? 0,
					IsWinner = player.WinState == WinState.Won,
					UnitsKilled = stats?.UnitsKilled ?? 0,
					UnitsDead = stats?.UnitsDead ?? 0,
					BuildingsKilled = stats?.BuildingsKilled ?? 0,
					BuildingsDead = stats?.BuildingsDead ?? 0,
					KillsCost = stats?.KillsCost ?? 0,
					DeathsCost = stats?.DeathsCost ?? 0,
					Earned = resources?.Earned ?? 0,
					Spent = resources?.Spent ?? 0,
					ArmyValue = stats?.ArmyValue ?? 0
				};

				if (playerStats.IsWinner)
				{
					results.WinnerName = playerStats.Name;
					results.WinnerFaction = playerStats.Faction;
				}

				results.PlayerStats.Add(playerStats);
			}

			// If no winner yet, determine by most kills value
			if (results.WinnerName == null && results.PlayerStats.Count > 0)
			{
				var bestPlayer = results.PlayerStats.OrderByDescending(p => p.KillsCost).First();
				results.WinnerName = bestPlayer.Name;
				results.WinnerFaction = bestPlayer.Faction;
			}

			AIBattleManager.LastResults = results;
		}

		static void ShowAIBattleResults()
		{
			// Capture the replay path now that the connection is closed
			CaptureAIBattleReplayPath();

			AIBattleState.Reset();

			// Set the state so MainMenuLogic.OpenMenuBasedOnLastGame opens the results panel
			MainMenuLogic.SetLastGameState(MainMenuLogic.MenuPanelType.AIBattleResults);

			Game.LoadShellMap();
		}

		static void CaptureAIBattleReplayPath()
		{
			var mod = Game.ModData.Manifest;
			var replayDir = System.IO.Path.Combine(Platform.SupportDir, "Replays", mod.Id, mod.Metadata.Version);
			if (System.IO.Directory.Exists(replayDir))
			{
				var latestReplay = System.IO.Directory.GetFiles(replayDir, "*.orarep")
					.OrderByDescending(f => System.IO.File.GetCreationTime(f))
					.FirstOrDefault();
				AIBattleManager.LastReplayPath = latestReplay;
			}
		}
	}
}

