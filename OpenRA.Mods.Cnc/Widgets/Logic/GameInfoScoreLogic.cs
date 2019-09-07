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
using System.Collections;
using System.Collections.Generic;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class GameInfoScoreLogic : ChromeLogic
	{
		readonly World world;
		readonly ModData modData;
		readonly Player localPlayer;
		Sheet sheet;
		Sprite gdiLogo, nodLogo;

		Widget panel;
		SpriteWidget spriteWidget;
		Dictionary<string, int> unitsLost, buildingsLost;
		readonly Dictionary<string, ProgressiveBarCounterWidget> progressUnitsLost,
		 progressBuildingsLost, progressResourcesEarned, progressResourcesSpent;
		List<string> factions;

		string[] labelIDs = {
			"TIME", "PRIMARY_OBJECTIVES_COUNT", "SECONDARY_OBJECTIVES_COUNT", 
			"TOTAL_SCORE", "UNITS_LOST", "UNITS_LOST_GDI", "UNITS_LOST_NOD",
			"BUILDINGS_LOST", "BUILDINGS_LOST_GDI", "BUILDINGS_LOST_NOD",
			"RESOURCES", "RESOURCES_GDI", "RESOURCES_EARNED_GDI",
			"RESOURCES_SPENT_GDI", "RESOURCES_NOD", "RESOURCES_EARNED_NOD",
			"RESOURCES_SPENT_NOD"
		};
		Dictionary<string, RevealingLabelWidget> labels;

		List<Widget> animationOrder;
		int animationCounter = 0;

		// int percentCounter;

		[ObjectCreator.UseCtor]
		public GameInfoScoreLogic(Widget widget, ModData modData, World world)
		{
			this.world = world;
			this.modData = modData;
			localPlayer = world.RenderPlayer ?? world.LocalPlayer;
			panel = widget;

			unitsLost = new Dictionary<string, int>();
			buildingsLost = new Dictionary<string, int>();
			progressUnitsLost = new Dictionary<string, ProgressiveBarCounterWidget>();
			progressBuildingsLost = new Dictionary<string, ProgressiveBarCounterWidget>();
			progressResourcesEarned = new Dictionary<string, ProgressiveBarCounterWidget>();
			progressResourcesSpent = new Dictionary<string, ProgressiveBarCounterWidget>();
			factions = new List<string>();
			labels = new Dictionary<string, RevealingLabelWidget>();
			animationOrder = new List<Widget>();

			using (var stream = modData.DefaultFileSystem.Open("cnc|uibits/scores.png"))
				sheet = new Sheet(SheetType.BGRA, stream);

			gdiLogo = new Sprite(sheet, new Rectangle(25, 31, 278, 304), TextureChannel.RGBA);
			nodLogo = new Sprite(sheet, new Rectangle(302, 32, 324, 304), TextureChannel.RGBA);

			var missionObjectives = localPlayer.PlayerActor.TraitOrDefault<MissionObjectives>();
			int primaryCompleted = 0, primaryTotal = 0,  secondaryCompleted = 0, secondaryTotal = 0;
			foreach (var objective in missionObjectives.Objectives)
			{
				if (objective.Type == "Primary")
				{
					primaryTotal++;
					if (objective.State == ObjectiveState.Completed)
						primaryCompleted++;
				}
				if (objective.Type == "Secondary")
				{
					secondaryTotal++;
					if (objective.State == ObjectiveState.Completed)
						secondaryCompleted++;
				}
			}

			foreach (var labelID in labelIDs)
			{
				labels[labelID] = panel.Get<RevealingLabelWidget>(labelID);
				labels[labelID].OnAnimationDone = IncreaseAnimationCounter;
			}

			labels["TIME"].Text = String.Format(labels["TIME"].Text, WidgetUtils.FormatTime(Math.Max(0, world.WorldTick), world.Timestep));
			labels["PRIMARY_OBJECTIVES_COUNT"].Text = String.Format(labels["PRIMARY_OBJECTIVES_COUNT"].Text, primaryCompleted, primaryTotal);
			labels["SECONDARY_OBJECTIVES_COUNT"].Text = String.Format(labels["SECONDARY_OBJECTIVES_COUNT"].Text, secondaryCompleted, secondaryTotal);

			foreach (var player in world.Players)
			{
				var playerStatistics = player.PlayerActor.Trait<PlayerStatistics>();
				var playerResources = player.PlayerActor.Trait<PlayerResources>();
				if (!factions.Contains(player.Faction.Name))
				{
					factions.Add(player.Faction.Name);

					progressUnitsLost[player.Faction.Name] = panel.Get<ProgressiveBarCounterWidget>("PROGRESS_UNITS_LOST_" + player.Faction.Name.ToUpper());
					progressUnitsLost[player.Faction.Name].Background = "progressbar-bg-" + player.Faction.Name.ToLower();
					progressUnitsLost[player.Faction.Name].Bar = "progressbar-thumb-" + player.Faction.Name.ToLower();

					progressBuildingsLost[player.Faction.Name] = panel.Get<ProgressiveBarCounterWidget>("PROGRESS_BUILDINGS_LOST_" + player.Faction.Name.ToUpper());
					progressBuildingsLost[player.Faction.Name].Background = "progressbar-bg-" + player.Faction.Name.ToLower();
					progressBuildingsLost[player.Faction.Name].Bar = "progressbar-thumb-" + player.Faction.Name.ToLower();

					progressResourcesEarned[player.Faction.Name] = panel.Get<ProgressiveBarCounterWidget>("PROGRESS_RESOURCES_EARNED_" + player.Faction.Name.ToUpper());
					progressResourcesEarned[player.Faction.Name].Background = "progressbar-bg-" + player.Faction.Name.ToLower();
					progressResourcesEarned[player.Faction.Name].Bar = "progressbar-thumb-" + player.Faction.Name.ToLower();

					progressResourcesSpent[player.Faction.Name] = panel.Get<ProgressiveBarCounterWidget>("PROGRESS_RESOURCES_SPENT_" + player.Faction.Name.ToUpper());
					progressResourcesSpent[player.Faction.Name].Background = "progressbar-bg-" + player.Faction.Name.ToLower();
					progressResourcesSpent[player.Faction.Name].Bar = "progressbar-thumb-" + player.Faction.Name.ToLower();
				}

				if (unitsLost.ContainsKey(player.Faction.Name))
					unitsLost[player.Faction.Name] += playerStatistics.UnitsDead;
				else
					unitsLost[player.Faction.Name] = playerStatistics.UnitsDead;

				if (buildingsLost.ContainsKey(player.Faction.Name))
					buildingsLost[player.Faction.Name] += playerStatistics.BuildingsDead;
				else
					buildingsLost[player.Faction.Name] = playerStatistics.BuildingsDead;

				progressResourcesEarned[player.Faction.Name].SetMaxValue(playerResources.Earned);
				progressResourcesSpent[player.Faction.Name].SetMaxValue(playerResources.Spent);
			}

			// TODO Cleanup testing code
			unitsLost["GDI"] = 200;
			unitsLost["Nod"] = 100;
			buildingsLost["GDI"] = 200;
			buildingsLost["Nod"] = 100;

			foreach (var faction in factions)
			{
				progressUnitsLost[faction].SetMaxValue(unitsLost[faction]);
				progressBuildingsLost[faction].SetMaxValue(buildingsLost[faction]);
			}

			// Define animation order
			animationOrder.AddRange(labels.Values);
			animationOrder.AddRange(progressUnitsLost.Values);
			animationOrder.AddRange(progressBuildingsLost.Values);
			animationOrder.AddRange(progressResourcesEarned.Values);
			animationOrder.AddRange(progressResourcesSpent.Values);

			// Faction sprite
			spriteWidget = panel.Get<SpriteWidget>("SPRITE_FACTION");
			spriteWidget.GetScale = () => 0.35f;
			spriteWidget.GetSprite = () => localPlayer.PlayerReference.Faction == "gdi" ? gdiLogo : nodLogo;

			var ticker = panel.GetOrNull<LogicTickerWidget>("ANIMATION_TICKER");
			if (ticker != null)
			{
				animationCounter = 0;
				ticker.OnTick = Animate;
			}
			if (localPlayer.WinState == WinState.Won)
				Game.Sound.PlayNotification(world.Map.Rules, null, "Speech", localPlayer.PlayerActor.Trait<MissionObjectives>().Info.WinNotification, localPlayer.Faction.InternalName);
			else if (localPlayer.WinState == WinState.Lost)
				Game.Sound.PlayNotification(world.Map.Rules, null, "Speech", localPlayer.PlayerActor.Trait<MissionObjectives>().Info.LoseNotification, localPlayer.Faction.InternalName);
		}

		void Animate()
		{
			if (animationCounter < animationOrder.Count)
			{
				animationOrder[animationCounter].Visible = true;
				if (animationOrder[animationCounter] is ProgressiveBarCounterWidget
				|| animationOrder[animationCounter] is ProgressiveCounterWidget)
					animationCounter++;
			}
		}

		void IncreaseAnimationCounter()
		{
			animationCounter++;
		}
	}
}
