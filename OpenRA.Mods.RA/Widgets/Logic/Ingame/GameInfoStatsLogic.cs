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
using OpenRA.Mods.RA;
using OpenRA.Network;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	class GameInfoStatsLogic
	{
		[ObjectCreator.UseCtor]
		public GameInfoStatsLogic(Widget widget, World world, OrderManager orderManager)
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
				var client = world.LobbyInfo.ClientWithIndex(pp.ClientIndex);

				var item = playerTemplate.Clone();

				item.Get("ADMIN_INDICATOR").IsVisible = () => client.IsAdmin;
				var block = item.Get("LATENCY");
				var visible = client.Bot == null;
				block.IsVisible = () => visible;

				if (visible)
					block.Get<ColorBlockWidget>("LATENCY_COLOR").GetColor = () =>
						LobbyUtils.LatencyColor(orderManager.LobbyInfo.PingFromClient(client));

				var tooltip = item.Get<ClientTooltipRegionWidget>("CLIENT_REGION");
				tooltip.IsVisible = () => visible;
				tooltip.Bind(orderManager, client.Index);

				var nameLabel = item.Get<LabelWidget>("NAME");
				nameLabel.GetText = () =>
				{
					if (client != null && client.State == Network.Session.ClientState.Disconnected)
						return pp.PlayerName + " (Gone)";
					return pp.PlayerName + (pp.WinState == WinState.Undefined ? "" : " (" + pp.WinState + ")");
				};
				nameLabel.GetColor = () => pp.Color.RGB;

				var flag = item.Get<ImageWidget>("FACTIONFLAG");
				flag.GetImageName = () => pp.Country.Race;
				flag.GetImageCollection = () => "flags";
				item.Get<LabelWidget>("FACTION").GetText = () => pp.Country.Name;

				var team = item.Get<LabelWidget>("TEAM");
				var teamNumber = (client == null) ? 0 : client.Team;
				team.GetText = () => (teamNumber == 0) ? "-" : teamNumber.ToString();
				playerPanel.AddChild(item);

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
