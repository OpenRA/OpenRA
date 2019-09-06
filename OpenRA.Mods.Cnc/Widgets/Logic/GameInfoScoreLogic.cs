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
		Dictionary<string, int> unitsLost;
		Dictionary<string, int> buildingsLost;
		readonly Dictionary<string, int> percentageUnitsLost;
		readonly Dictionary<string, int> percentageBuildingsLost;
		readonly Dictionary<string, ProgressBarWidget> progressUnitsLost;
		readonly Dictionary<string, ProgressBarWidget> progressBuildingsLost;
		readonly Dictionary<string, LabelWidget> labelUnitsLost;
		readonly Dictionary<string, LabelWidget> labelBuildingsLost;
		List<string> factions;

		bool animateProgressBars = true;
		int percentCounter;

		[ObjectCreator.UseCtor]
		public GameInfoScoreLogic(Widget widget, ModData modData, World world)
		{
			this.world = world;
			this.modData = modData;
			localPlayer = world.LocalPlayer;
			panel = widget;

			unitsLost = new Dictionary<string, int>();
			buildingsLost = new Dictionary<string, int>();
			percentageUnitsLost = new Dictionary<string, int>();
			percentageBuildingsLost = new Dictionary<string, int>();
			progressUnitsLost = new Dictionary<string, ProgressBarWidget>();
			progressBuildingsLost = new Dictionary<string, ProgressBarWidget>();
			labelUnitsLost = new Dictionary<string, LabelWidget>();
			labelBuildingsLost = new Dictionary<string, LabelWidget>();
			factions = new List<string>();
			percentCounter = 0;

			using (var stream = modData.DefaultFileSystem.Open("cnc|uibits/scores.png"))
				sheet = new Sheet(SheetType.BGRA, stream);

			gdiLogo = new Sprite(sheet, new Rectangle(25, 31, 278, 304), TextureChannel.RGBA);
			nodLogo = new Sprite(sheet, new Rectangle(302, 32, 324, 304), TextureChannel.RGBA);

			foreach (var player in world.Players)
			{
				if (!factions.Contains(player.Faction.Name))
				{
					factions.Add(player.Faction.Name);

					progressUnitsLost[player.Faction.Name] = panel.Get<ProgressBarWidget>("PROGRESS_UNITS_LOST_" + player.Faction.Name.ToUpper());
					progressUnitsLost[player.Faction.Name].Background = "progressbar-bg-" + player.Faction.Name.ToLower();
					progressUnitsLost[player.Faction.Name].Bar = "progressbar-thumb-" + player.Faction.Name.ToLower();
					progressUnitsLost[player.Faction.Name].Percentage = 0;
					labelUnitsLost[player.Faction.Name] = panel.Get<LabelWidget>("VALUE_UNITS_LOST_" + player.Faction.Name.ToUpper());
					labelUnitsLost[player.Faction.Name].Text = "0";

					progressBuildingsLost[player.Faction.Name] = panel.Get<ProgressBarWidget>("PROGRESS_BUILDINGS_LOST_" + player.Faction.Name.ToUpper());
					progressBuildingsLost[player.Faction.Name].Background = "progressbar-bg-" + player.Faction.Name.ToLower();
					progressBuildingsLost[player.Faction.Name].Bar = "progressbar-thumb-" + player.Faction.Name.ToLower();
					progressBuildingsLost[player.Faction.Name].Percentage = 0;
					labelBuildingsLost[player.Faction.Name] = panel.Get<LabelWidget>("VALUE_BUILDINGS_LOST_" + player.Faction.Name.ToUpper());
					labelBuildingsLost[player.Faction.Name].Text = "0";
				}

				if (unitsLost.ContainsKey(player.Faction.Name))
					unitsLost[player.Faction.Name] += player.PlayerActor.Trait<PlayerStatistics>().UnitsDead;
				else
					unitsLost[player.Faction.Name] = player.PlayerActor.Trait<PlayerStatistics>().UnitsDead;

				if (buildingsLost.ContainsKey(player.Faction.Name))
					buildingsLost[player.Faction.Name] += player.PlayerActor.Trait<PlayerStatistics>().BuildingsDead;
				else
					buildingsLost[player.Faction.Name] = player.PlayerActor.Trait<PlayerStatistics>().BuildingsDead;
			}

			// TODO Cleanup testing code
			unitsLost["GDI"] = 200;
			unitsLost["Nod"] = 100;
			buildingsLost["GDI"] = 200;
			buildingsLost["Nod"] = 100;

			var maxUnits = unitsLost.MaxBy(x => x.Value).Value;
			var maxBuildings = buildingsLost.MaxBy(x => x.Value).Value;

			foreach (var faction in factions)
			{
				percentageUnitsLost[faction] = unitsLost[faction] * 100 / maxUnits;
				percentageBuildingsLost[faction] = buildingsLost[faction] * 100 / maxBuildings;
			}

			// Faction sprite
			spriteWidget = panel.Get<SpriteWidget>("SPRITE_FACTION");
			spriteWidget.GetScale = () => 0.35f;
			spriteWidget.GetSprite = () => localPlayer.PlayerReference.Faction == "gdi" ? gdiLogo : nodLogo;

			var ticker = panel.GetOrNull<LogicTickerWidget>("ANIMATION_TICKER");
			if (ticker != null)
			{
				ticker.OnTick = Animate;
			}
		}

		void Animate()
		{
			// TODO Animate text
			if (animateProgressBars)
			{
				percentCounter += 1;
				if (percentCounter == 100)
					animateProgressBars = false;
				foreach (var faction in factions)
				{
					if (percentCounter <= percentageUnitsLost[faction])
						progressUnitsLost[faction].Percentage = percentCounter;
					labelUnitsLost[faction].Text = (unitsLost[faction] * percentCounter / 100).ToString();
					if (percentCounter <= percentageBuildingsLost[faction])
						progressBuildingsLost[faction].Percentage = percentCounter;
					labelBuildingsLost[faction].Text = (buildingsLost[faction] * percentCounter / 100).ToString();
				}
			}
		}
	}
}
