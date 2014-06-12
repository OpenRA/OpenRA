#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class CncConquestObjectivesLogic
	{
		[ObjectCreator.UseCtor]
		public CncConquestObjectivesLogic(Widget widget, World world)
		{
			var panel = widget.Get("CONQUEST_OBJECTIVES");
			panel.Get<LabelWidget>("TITLE").GetText = () => "Conquest: " + world.Map.Title;

			var statusLabel = panel.Get<LabelWidget>("STATUS");
			statusLabel.IsVisible = () => world.LocalPlayer != null;

			if (world.LocalPlayer != null)
			{
				var lp = world.LocalPlayer;
				var objectiveCheckbox = panel.Get<CheckboxWidget>("1");
				objectiveCheckbox.IsChecked = () => lp.WinState != WinState.Undefined;
				objectiveCheckbox.GetCheckType = () => lp.WinState == WinState.Won ?
					"checked" : "crossed";

				statusLabel.GetText = () => lp.WinState == WinState.Won ? "Complete" :
					lp.WinState == WinState.Lost ? "Failed" : "Incomplete";
				statusLabel.GetColor = () => lp.WinState == WinState.Won ? Color.Green :
					lp.WinState == WinState.Lost ? Color.Red : Color.White;
			}

			var scrollpanel = panel.Get<ScrollPanelWidget>("PLAYER_LIST");
			var itemTemplate = scrollpanel.Get("PLAYER_TEMPLATE");
			scrollpanel.RemoveChildren();

			foreach (var p in world.Players.Where(a => !a.NonCombatant))
			{
				var pp = p;
				var item = itemTemplate.Clone();
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
				scrollpanel.AddChild(item);

				var stats = pp.PlayerActor.TraitOrDefault<PlayerStatistics>();
				if (stats == null)
					break;
				var totalKills = stats.UnitsKilled + stats.BuildingsKilled;
				var totalDeaths = stats.UnitsDead + stats.BuildingsDead;
				item.Get<LabelWidget>("KILLS").GetText = () => totalKills.ToString();
				item.Get<LabelWidget>("DEATHS").GetText = () => totalDeaths.ToString();
			}
		}
	}
}
