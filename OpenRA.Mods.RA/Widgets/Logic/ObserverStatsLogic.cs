#region Copyright & License Information
/*
 * Copyright 2007-2012 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.Linq;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Network;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class ObserverStatsLogic
	{
		ScrollPanelWidget playersPanel;
		ScrollItemWidget playerTemplate;
		ScrollItemWidget teamTemplate;

		[ObjectCreator.UseCtor]
		public ObserverStatsLogic(World world, Widget widget)
		{
			playersPanel = widget.Get<ScrollPanelWidget>("PLAYERS");
			playerTemplate = playersPanel.Get<ScrollItemWidget>("PLAYER_TEMPLATE");
			teamTemplate = playersPanel.Get<ScrollItemWidget>("TEAM_TEMPLATE");
			playersPanel.RemoveChildren();
			playersPanel.Layout = new GridLayout(playersPanel);

			var players = world.Players.Where(p => !p.NonCombatant);

			widget.Height = (200 + (Math.Min(8, players.Count()) * 25)).ToString();
			var args = new WidgetArgs();
			widget.Initialize(args);
			widget.Get("BACKGROUND").Initialize(args);
			widget.Get("PLAYERS").Initialize(args);

			var teams = players.GroupBy(p => (world.LobbyInfo.ClientWithIndex(p.ClientIndex) ?? new Session.Client()).Team).OrderBy(g => g.Key);
			foreach (var t in teams)
			{
				var team = t;
				var tt = ScrollItemWidget.Setup(teamTemplate, () => false, () => { });
				tt.IgnoreMouseOver = true;
				tt.Get<LabelWidget>("TEAM").GetText = () => team.Key == 0 ? "No team" : "Team " + team.Key;
				playersPanel.AddChild(tt);
				foreach (var p in team)
				{
					var player = p;
					var template = ScrollItemWidget.Setup(playerTemplate, () => false, null);
					template.OnClick = () =>
					{
						var playerBase = world.Actors.FirstOrDefault(a => !a.IsDead() && a.HasTrait<BaseBuilding>() && a.Owner == player);
						if (playerBase != null)
						{
							Game.MoveViewport(playerBase.Location.ToFloat2());
						}
					};

					var flag = template.Get<ImageWidget>("FACTION_FLAG");
					flag.GetImageName = () => player.Country.Race;
					flag.GetImageCollection = () => "flags";

					var playerName = template.Get<LabelWidget>("PLAYER");
					playerName.GetText = () => player.PlayerName + (player.WinState == WinState.Undefined ? "" : " (" + player.WinState + ")");
					playerName.GetColor = () => player.ColorRamp.GetColor(0);

					var res = player.PlayerActor.Trait<PlayerResources>();
					template.Get<LabelWidget>("CASH").GetText = () => "$" + (res.DisplayCash + res.DisplayOre);

					var powerRes = player.PlayerActor.Trait<PowerManager>();
					var power = template.Get<LabelWidget>("POWER");
					power.GetText = () => powerRes.PowerDrained + "/" + powerRes.PowerProvided;
					power.GetColor = () => GetPowerColor(powerRes.PowerState);

					template.Get<LabelWidget>("KILLS").GetText = () => player.Kills.ToString();
					template.Get<LabelWidget>("DEATHS").GetText = () => player.Deaths.ToString();

					playersPanel.AddChild(template);
				}
			}
		}

		static Color GetPowerColor(PowerState state)
		{
			if (state == PowerState.Critical) return Color.Red;
			if (state == PowerState.Low) return Color.Orange;
			return Color.LimeGreen;
		}
	}
}
