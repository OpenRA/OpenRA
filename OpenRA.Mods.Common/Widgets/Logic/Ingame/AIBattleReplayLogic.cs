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
using OpenRA.Mods.Common.Traits;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	/// <summary>
	/// Logic for the AI Battle replay viewer with timeline scrubbing capabilities.
	/// Supports forward seeking and rewind (via restart-and-fast-forward).
	/// </summary>
	public class AIBattleReplayLogic : ChromeLogic
	{
		[FluentReference("time")]
		const string LabelRewinding = "label-ai-battle-rewinding";

		[FluentReference("time")]
		const string LabelSeeking = "label-ai-battle-seeking";

		[FluentReference]
		const string LabelNoFog = "label-no-fog";

		[FluentReference]
		const string LabelCombinedVision = "label-combined-vision";

		readonly World world;
		readonly OrderManager orderManager;
		readonly int originalTimestep;
		readonly string replayPath;

		[ObjectCreator.UseCtor]
		public AIBattleReplayLogic(Widget widget, World world, OrderManager orderManager)
		{
			this.world = world;
			this.orderManager = orderManager;
			originalTimestep = world.Timestep;

			if (orderManager.Connection is ReplayConnection rc)
				replayPath = rc.Filename;

			SetupTimeline(widget);
			SetupSpeedControls(widget);
			SetupShroudSelector(widget);
			SetupMenuButton(widget);
			SetupStatsOverlay(widget);
		}

		void SetupTimeline(Widget widget)
		{
			var timeline = widget.GetOrNull<TimelineScrubberWidget>("TIMELINE");
			if (timeline == null)
				return;

			timeline.GetCurrentTick = () => AIBattleRewindState.IsRewinding ? AIBattleRewindState.TargetTick : world.WorldTick;
			timeline.GetTotalTicks = () =>
			{
				if (orderManager.Connection is ReplayConnection rc)
					return rc.FinalGameTick > 0 ? rc.FinalGameTick : rc.TickCount;
				return world.WorldTick;
			};
			timeline.GetTimestep = () => originalTimestep;
			timeline.IsRewinding = () => AIBattleRewindState.IsRewinding;
			timeline.RewindTargetTick = () => AIBattleRewindState.TargetTick;

			timeline.OnSeek = targetTick =>
			{
				if (targetTick < world.WorldTick)
					StartRewind(targetTick);
				else if (targetTick > world.WorldTick)
					FastForwardTo(targetTick);
			};

			// Rewind status label
			var statusLabel = widget.GetOrNull<LabelWidget>("REWIND_STATUS");
			if (statusLabel != null)
			{
				statusLabel.IsVisible = () => AIBattleRewindState.IsRewinding || AIBattleFastForwardState.IsFastForwarding;
				statusLabel.GetText = () =>
				{
					if (AIBattleRewindState.IsRewinding)
					{
						var targetTime = WidgetUtils.FormatTime(AIBattleRewindState.TargetTick, originalTimestep);
						return FluentProvider.GetMessage(LabelRewinding, "time", targetTime);
					}

					if (AIBattleFastForwardState.IsFastForwarding)
					{
						var targetTime = WidgetUtils.FormatTime(AIBattleFastForwardState.TargetTick, originalTimestep);
						return FluentProvider.GetMessage(LabelSeeking, "time", targetTime);
					}

					return "";
				};
			}
		}

		void StartRewind(int targetTick)
		{
			if (AIBattleRewindState.IsRewinding || string.IsNullOrEmpty(replayPath))
				return;

			// Store current state for restoration after restart
			var currentRenderPlayer = world.RenderPlayer;

			// Set up rewind state
			AIBattleRewindState.IsRewinding = true;
			AIBattleRewindState.TargetTick = targetTick;
			AIBattleRewindState.RestoreRenderPlayer = currentRenderPlayer?.InternalName;

			// Disconnect and restart the replay
			Game.RunAfterTick(() =>
			{
				// Set up fast-forward to reach the target tick
				AIBattleFastForwardState.IsFastForwarding = true;
				AIBattleFastForwardState.TargetTick = targetTick;

				Game.JoinReplay(replayPath);
			});
		}

		void FastForwardTo(int targetTick)
		{
			// Set maximum speed until we reach target
			AIBattleFastForwardState.IsFastForwarding = true;
			AIBattleFastForwardState.TargetTick = targetTick;
			world.ReplayTimestep = 1; // Maximum speed
		}

		void SetupSpeedControls(Widget widget)
		{
			var pauseButton = widget.GetOrNull<ButtonWidget>("BUTTON_PAUSE");
			var playButton = widget.GetOrNull<ButtonWidget>("BUTTON_PLAY");

			if (pauseButton != null)
			{
				pauseButton.IsVisible = () => world.ReplayTimestep != 0;
				pauseButton.OnClick = () => world.ReplayTimestep = 0;
			}

			if (playButton != null)
			{
				playButton.IsVisible = () => world.ReplayTimestep == 0;
				playButton.OnClick = () => world.ReplayTimestep = originalTimestep;
			}

			// Speed multiplier buttons
			var speeds = new[]
			{
				(0.5f, "BUTTON_SLOW"),
				(1f, "BUTTON_1X"),
				(2f, "BUTTON_2X"),
				(4f, "BUTTON_4X"),
				(1000f, "BUTTON_MAX")
			};

			foreach (var (multiplier, buttonId) in speeds)
			{
				var button = widget.GetOrNull<ButtonWidget>(buttonId);
				if (button == null)
					continue;

				var capturedMultiplier = multiplier;
				var targetTimestep = Math.Max(1, (int)(originalTimestep / capturedMultiplier));

				button.OnClick = () =>
				{
					world.ReplayTimestep = targetTimestep;

					// Cancel any ongoing fast-forward when manually changing speed
					if (AIBattleFastForwardState.IsFastForwarding)
						AIBattleFastForwardState.Reset();
				};
				button.IsHighlighted = () =>
					world.ReplayTimestep == targetTimestep && world.ReplayTimestep != 0 && !AIBattleFastForwardState.IsFastForwarding;
			}
		}

		void SetupShroudSelector(Widget widget)
		{
			var shroudSelector = widget.GetOrNull<DropDownButtonWidget>("SHROUD_SELECTOR");
			if (shroudSelector == null)
				return;

			var aiPlayers = world.Players
				.Where(p => p.IsBot && !p.NonCombatant)
				.ToList();

			var everyonePlayer = world.Players
				.FirstOrDefault(p => p.InternalName == "Everyone");

			shroudSelector.GetText = () =>
			{
				if (world.RenderPlayer == null)
					return FluentProvider.GetMessage(LabelNoFog);
				if (world.RenderPlayer == everyonePlayer)
					return FluentProvider.GetMessage(LabelCombinedVision);
				return world.RenderPlayer.ResolvedPlayerName;
			};

			shroudSelector.OnClick = () =>
			{
				var options = new List<DropDownOption>
				{
					// No Fog option
					new()
					{
						Title = FluentProvider.GetMessage(LabelNoFog),
						OnClick = () => world.RenderPlayer = null,
						IsSelected = () => world.RenderPlayer == null
					}
				};

				// Combined Vision option
				if (everyonePlayer != null)
				{
					options.Add(new DropDownOption
					{
						Title = FluentProvider.GetMessage(LabelCombinedVision),
						OnClick = () => world.RenderPlayer = everyonePlayer,
						IsSelected = () => world.RenderPlayer == everyonePlayer
					});
				}

				// Individual AI player options
				foreach (var player in aiPlayers)
				{
					var p = player;
					options.Add(new DropDownOption
					{
						Title = p.ResolvedPlayerName,
						OnClick = () => world.RenderPlayer = p,
						IsSelected = () => world.RenderPlayer == p
					});
				}

				ScrollItemWidget SetupItem(DropDownOption o, ScrollItemWidget itemTemplate)
				{
					var item = ScrollItemWidget.Setup(itemTemplate, o.IsSelected, o.OnClick);
					item.Get<LabelWidget>("LABEL").GetText = () => o.Title;
					return item;
				}

				shroudSelector.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 300, options, SetupItem);
			};
		}

		static void SetupMenuButton(Widget widget)
		{
			var menuButton = widget.GetOrNull<ButtonWidget>("BUTTON_MENU");
			if (menuButton == null)
				return;

			menuButton.OnClick = () =>
			{
				Game.Disconnect();
				AIBattleState.Reset();
				AIBattleRewindState.Reset();
				AIBattleFastForwardState.Reset();

				// Return to results screen if we have results, otherwise main menu
				if (AIBattleManager.LastResults != null)
					Ui.OpenWindow("AI_BATTLE_RESULTS_PANEL");
				else
					Game.LoadShellMap();
			};
		}

		void SetupStatsOverlay(Widget widget)
		{
			var statsOverlay = widget.GetOrNull<ContainerWidget>("STATS_OVERLAY");
			var statsButton = widget.GetOrNull<ButtonWidget>("BUTTON_STATS");

			if (statsOverlay == null || statsButton == null)
				return;

			var showStats = false;

			statsOverlay.IsVisible = () => showStats;
			statsButton.OnClick = () => showStats = !showStats;
			statsButton.IsHighlighted = () => showStats;

			var statsList = statsOverlay.GetOrNull<ScrollPanelWidget>("STATS_LIST");
			var template = statsOverlay.GetOrNull<ContainerWidget>("STAT_PLAYER_TEMPLATE");

			if (statsList == null || template == null)
				return;

			var aiPlayers = world.Players
				.Where(p => p.IsBot && !p.NonCombatant)
				.ToList();

			var yOffset = 0;
			foreach (var player in aiPlayers)
			{
				var row = template.Clone();
				row.IsVisible = () => true;
				row.Bounds.Y = yOffset;

				var p = player;
				var stats = p.PlayerActor.TraitOrDefault<PlayerStatistics>();
				var resources = p.PlayerActor.TraitOrDefault<PlayerResources>();

				var nameLabel = row.GetOrNull<LabelWidget>("PLAYER_NAME");
				if (nameLabel != null)
				{
					nameLabel.GetText = () => p.ResolvedPlayerName;
					nameLabel.GetColor = () => p.Color;
				}

				var killsLabel = row.GetOrNull<LabelWidget>("STAT_KILLS");
				if (killsLabel != null)
					killsLabel.GetText = () => $"Kills: {(stats?.UnitsKilled ?? 0) + (stats?.BuildingsKilled ?? 0)}";

				var deathsLabel = row.GetOrNull<LabelWidget>("STAT_DEATHS");
				if (deathsLabel != null)
					deathsLabel.GetText = () => $"Deaths: {(stats?.UnitsDead ?? 0) + (stats?.BuildingsDead ?? 0)}";

				var armyLabel = row.GetOrNull<LabelWidget>("STAT_ARMY");
				if (armyLabel != null)
					armyLabel.GetText = () => $"Army: ${stats?.ArmyValue ?? 0:N0}";

				var incomeLabel = row.GetOrNull<LabelWidget>("STAT_INCOME");
				if (incomeLabel != null)
					incomeLabel.GetText = () => $"Income: ${stats?.DisplayIncome ?? 0}/min";

				var resourcesLabel = row.GetOrNull<LabelWidget>("STAT_RESOURCES");
				if (resourcesLabel != null)
					resourcesLabel.GetText = () =>
						$"Earned: ${resources?.Earned ?? 0:N0} | Spent: ${resources?.Spent ?? 0:N0}";

				statsList.AddChild(row);
				yOffset += row.Bounds.Height + 5;
			}
		}

		sealed class DropDownOption
		{
			public string Title;
			public Action OnClick;
			public Func<bool> IsSelected;
		}
	}
}
