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

using System.Globalization;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	/// <summary>
	/// Logic for displaying AI Battle results after a battle ends.
	/// Shows winner, duration, and per-player statistics.
	/// </summary>
	public class AIBattleResultsLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public AIBattleResultsLogic(Widget widget, ModData modData)
		{
			var results = AIBattleManager.LastResults;
			if (results == null)
			{
				// No results available, go back to menu
				Ui.CloseWindow();
				Game.LoadShellMap();
				return;
			}

			// Winner display
			var winnerName = widget.Get<LabelWidget>("WINNER_NAME");
			var winnerText = results.WinnerName != null
				? $"{results.WinnerName} ({results.WinnerFaction})"
				: "Draw";
			winnerName.GetText = () => winnerText;
			winnerName.GetColor = () => Color.FromArgb(0, 255, 0); // Green for winner

			// Duration
			var durationValue = widget.Get<LabelWidget>("DURATION_VALUE");
			var durationText = WidgetUtils.FormatTime(results.DurationTicks, results.Timestep);
			durationValue.GetText = () => durationText;

			// Player stats
			var statsPanel = widget.Get<ScrollPanelWidget>("STATS_PANEL");
			var template = statsPanel.Get<ContainerWidget>("PLAYER_TEMPLATE");

			// Sort by damage dealt (highest first)
			var sortedStats = results.PlayerStats.OrderByDescending(p => p.KillsCost).ToList();

			foreach (var stats in sortedStats)
			{
				var row = template.Clone();
				row.IsVisible = () => true;

				var nameLabel = row.Get<LabelWidget>("PLAYER_NAME");
				var playerName = stats.IsWinner ? $"* {stats.Name}" : stats.Name;
				nameLabel.GetText = () => playerName;
				nameLabel.GetColor = () => stats.IsWinner ? Color.FromArgb(50, 205, 50) : Color.White; // LimeGreen for winners

				var killsLabel = row.Get<LabelWidget>("PLAYER_KILLS");
				var kills = stats.UnitsKilled + stats.BuildingsKilled;
				var killsText = kills.ToString(CultureInfo.InvariantCulture);
				killsLabel.GetText = () => killsText;

				var deathsLabel = row.Get<LabelWidget>("PLAYER_DEATHS");
				var deaths = stats.UnitsDead + stats.BuildingsDead;
				var deathsText = deaths.ToString(CultureInfo.InvariantCulture);
				deathsLabel.GetText = () => deathsText;

				var damageLabel = row.Get<LabelWidget>("PLAYER_DAMAGE");
				var damage = stats.KillsCost;
				damageLabel.GetText = () => $"${damage:N0}";

				var earnedLabel = row.Get<LabelWidget>("PLAYER_EARNED");
				var earned = stats.Earned;
				earnedLabel.GetText = () => $"${earned:N0}";

				var spentLabel = row.Get<LabelWidget>("PLAYER_SPENT");
				var spent = stats.Spent;
				spentLabel.GetText = () => $"${spent:N0}";

				var armyLabel = row.Get<LabelWidget>("PLAYER_ARMY");
				var army = stats.ArmyValue;
				armyLabel.GetText = () => $"${army:N0}";

				statsPanel.AddChild(row);
			}

			// Back button
			var backButton = widget.Get<ButtonWidget>("BACK_BUTTON");
			backButton.OnClick = () =>
			{
				Ui.CloseWindow();
				Game.LoadShellMap();
			};

			// Replay button
			var replayButton = widget.Get<ButtonWidget>("REPLAY_BUTTON");
			var hasReplay = !string.IsNullOrEmpty(AIBattleManager.LastReplayPath);
			replayButton.IsDisabled = () => !hasReplay;
			replayButton.OnClick = () =>
			{
				if (!hasReplay)
					return;

				Ui.CloseWindow();

				// Mark that we're watching an AI battle replay
				// (this can be used to show specialized replay UI in future phases)
				AIBattleState.IsAIBattle = true;

				Game.JoinReplay(AIBattleManager.LastReplayPath);
			};
		}
	}
}
