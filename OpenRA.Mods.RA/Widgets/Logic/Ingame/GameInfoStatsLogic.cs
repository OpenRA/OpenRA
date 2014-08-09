#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.Linq;
using OpenRA.Widgets;
using OpenRA.Traits;
using OpenRA.Mods.RA;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	class GameInfoStatsLogic
	{
		[ObjectCreator.UseCtor]
		public GameInfoStatsLogic(Widget widget, World world)
		{
			var lp = world.LocalPlayer;

			var checkbox = widget.Get<CheckboxWidget>("STATS_CHECKBOX");
			checkbox.IsChecked = () => lp.WinState != WinState.Undefined;
			checkbox.GetCheckType = () => lp.WinState == WinState.Won ?
				"checked" : "crossed";

			var statusLabel = widget.Get<LabelWidget>("STATS_STATUS");

			statusLabel.GetText = () => lp.WinState == WinState.Won ? "Accomplished" :
				lp.WinState == WinState.Lost ? "Failed" : "In progress";
			statusLabel.GetColor = () => lp.WinState == WinState.Won ? Color.LimeGreen :
				lp.WinState == WinState.Lost ? Color.Red : Color.White;

			var playerPanel = widget.Get<ScrollPanelWidget>("PLAYER_LIST");
			var playerTemplate = playerPanel.Get("PLAYER_TEMPLATE");
			playerPanel.RemoveChildren();

			foreach (var p in world.Players.Where(a => !a.NonCombatant))
			{
				var pp = p;
				var item = playerTemplate.Clone();
				var nameLabel = item.Get<LabelWidget>("NAME");
				nameLabel.GetText = () => pp.WinState == WinState.Lost ? pp.PlayerName + " (Dead)" : pp.PlayerName;
				nameLabel.GetColor = () => pp.Color.RGB;

				var flag = item.Get<ImageWidget>("FACTIONFLAG");
				flag.GetImageName = () => pp.Country.Race;
				flag.GetImageCollection = () => "flags";
				item.Get<LabelWidget>("FACTION").GetText = () => pp.Country.Name;

				var team = item.Get<LabelWidget>("TEAM");
				var client = world.LobbyInfo.ClientWithIndex(pp.ClientIndex);
				var teamNumber = (client == null) ? 0 : client.Team;
				team.GetText = () => (teamNumber == 0) ? "-" : teamNumber.ToString();
				playerPanel.AddChild(item);

				var stats = pp.PlayerActor.TraitOrDefault<PlayerStatistics>();
				if (stats == null)
					break;
				var totalKills = stats.UnitsKilled + stats.BuildingsKilled;
				var totalDeaths = stats.UnitsDead + stats.BuildingsDead;
				var totalExperience = stats.ExperienceGained;
				item.Get<LabelWidget>("KILLS").GetText = () => totalKills.ToString();
				item.Get<LabelWidget>("DEATHS").GetText = () => totalDeaths.ToString();
				item.Get<LabelWidget>("EXPERIENCE").GetText = () => totalExperience.ToString();
			}
		}
	}
}
