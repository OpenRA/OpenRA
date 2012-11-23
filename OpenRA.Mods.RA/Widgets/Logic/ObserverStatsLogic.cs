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
using System.Collections.Generic;
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
		ContainerWidget basicStatsHeaders;
		ContainerWidget economicStatsHeaders;
		ContainerWidget supportStatsHeaders;
		ScrollPanelWidget playerStatsPanel;
		ScrollItemWidget basicPlayerTemplate;
		ScrollItemWidget economicPlayerTemplate;
		ScrollItemWidget supportPlayerTemplate;
		ScrollItemWidget teamTemplate;
		DropDownButtonWidget statsDropDown;
		LabelWidget title;
		IEnumerable<Player> players;
		World world;

		[ObjectCreator.UseCtor]
		public ObserverStatsLogic(World world, Widget widget)
		{
			this.world = world;
			players = world.Players.Where(p => !p.NonCombatant);

			title = widget.Get<LabelWidget>("TITLE");

			basicStatsHeaders = widget.Get<ContainerWidget>("BASIC_STATS_HEADERS");
			economicStatsHeaders = widget.Get<ContainerWidget>("ECONOMIC_STATS_HEADERS");
			supportStatsHeaders = widget.Get<ContainerWidget>("SUPPORT_STATS_HEADERS");

			playerStatsPanel = widget.Get<ScrollPanelWidget>("PLAYER_STATS_PANEL");
			playerStatsPanel.Layout = new GridLayout(playerStatsPanel);

			basicPlayerTemplate = playerStatsPanel.Get<ScrollItemWidget>("BASIC_PLAYER_TEMPLATE");
			economicPlayerTemplate = playerStatsPanel.Get<ScrollItemWidget>("ECONOMIC_PLAYER_TEMPLATE");
			supportPlayerTemplate = playerStatsPanel.Get<ScrollItemWidget>("SUPPORT_PLAYER_TEMPLATE");

			teamTemplate = playerStatsPanel.Get<ScrollItemWidget>("TEAM_TEMPLATE");

			statsDropDown = widget.Get<DropDownButtonWidget>("STATS_DROPDOWN");
			statsDropDown.GetText = () => "Basic";
			statsDropDown.OnMouseDown = _ =>
			{
				var options = new List<StatsDropDownOption>
				{
					new StatsDropDownOption 
					{
						Title = "Basic",
						IsSelected = () => basicStatsHeaders.Visible,
						OnClick = () =>
						{
							ClearStats();
							statsDropDown.GetText = () => "Basic";
							LoadStats(BasicStats);
						}
					},
					new StatsDropDownOption 
					{
						Title = "Economic",
						IsSelected = () => economicStatsHeaders.Visible,
						OnClick = () => 
						{
							ClearStats();
							statsDropDown.GetText = () => "Economic";
							LoadStats(EconomicStats);
						}
					},
					new StatsDropDownOption 
					{
						Title = "Support",
						IsSelected = () => supportStatsHeaders.Visible,
						OnClick = () => 
						{
							ClearStats();
							statsDropDown.GetText = () => "Support";
							LoadStats(SupportStats);
						}
					}
				};
				Func<StatsDropDownOption, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
				{
					var item = ScrollItemWidget.Setup(template, option.IsSelected, option.OnClick);
					item.Get<LabelWidget>("LABEL").GetText = () => option.Title;
					return item;
				};
				statsDropDown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 100, options, setupItem);
			};

			widget.Height = (200 + (Math.Min(8, players.Count()) * 25)).ToString();
			InitializeWidgets(widget, widget.Get("BACKGROUND"), widget.Get("PLAYER_STATS_PANEL"));

			ClearStats();
			LoadStats(BasicStats);
		}

		void ClearStats()
		{
			playerStatsPanel.Children.Clear();
			basicStatsHeaders.Visible = false;
			economicStatsHeaders.Visible = false;
			supportStatsHeaders.Visible = false;
		}

		void LoadStats(Func<Player, ScrollItemWidget> forEachPlayer)
		{
			var teams = players.GroupBy(p => (world.LobbyInfo.ClientWithIndex(p.ClientIndex) ?? new Session.Client()).Team).OrderBy(g => g.Key);
			foreach (var t in teams)
			{
				var team = t;
				var tt = ScrollItemWidget.Setup(teamTemplate, () => false, () => { });
				tt.IgnoreMouseOver = true;
				tt.Get<LabelWidget>("TEAM").GetText = () => team.Key == 0 ? "No team" : "Team " + team.Key;
				playerStatsPanel.AddChild(tt);
				foreach (var p in team)
				{
					var player = p;
					playerStatsPanel.AddChild(forEachPlayer(player));
				}
			}
		}

		ScrollItemWidget SupportStats(Player player)
		{
			supportStatsHeaders.Visible = true;
			var template = SetupPlayerScrollItemWidget(supportPlayerTemplate, player);

			AddPlayerFlagAndName(template, player);

			var supportPowers = template.Get<ObserverSupportPowerIconsWidget>("SUPPORT_POWERS");
			supportPowers.GetPlayer = () => player;

			return template;
		}

		ScrollItemWidget EconomicStats(Player player)
		{
			economicStatsHeaders.Visible = true;
			var template = SetupPlayerScrollItemWidget(economicPlayerTemplate, player);

			AddPlayerFlagAndName(template, player);

			var res = player.PlayerActor.Trait<PlayerResources>();
			template.Get<LabelWidget>("CASH").GetText = () => "$" + (res.DisplayCash + res.DisplayOre);
			template.Get<LabelWidget>("INCOME").GetText = () => "$" + res.IncomePerMin;
			var change = template.Get<LabelWidget>("INCOME_CHANGE");
			change.GetText = () => Math.Round(res.IncomeChange * 100, 1, MidpointRounding.AwayFromZero) + "%";
			change.GetColor = () =>
			{
				var c = Math.Round(res.IncomeChange, 1, MidpointRounding.AwayFromZero);
				if (c < 0) return Color.Red;
				if (c > 0) return Color.LimeGreen;
				return Color.White;
			};

			var assets = template.Get<LabelWidget>("TOTAL_ASSETS");
			assets.GetText = () => "$" + world.Actors
				.Where(a => a.Owner == player && !a.IsDead() && a.Info.Traits.WithInterface<ValuedInfo>().Any())
				.Sum(a => a.Info.Traits.WithInterface<ValuedInfo>().First().Cost);

			var numHarvesters = template.Get<LabelWidget>("NUMBER_HARVESTERS");
			numHarvesters.GetText = () => world.Actors.Count(a => a.Owner == player && !a.IsDead() && a.HasTrait<Harvester>()).ToString();

			return template;
		}

		ScrollItemWidget BasicStats(Player player)
		{
			basicStatsHeaders.Visible = true;
			var template = SetupPlayerScrollItemWidget(basicPlayerTemplate, player);

			AddPlayerFlagAndName(template, player);

			var res = player.PlayerActor.Trait<PlayerResources>();
			template.Get<LabelWidget>("CASH").GetText = () => "$" + (res.DisplayCash + res.DisplayOre);
			template.Get<LabelWidget>("INCOME").GetText = () => "$" + res.IncomePerMin;

			var powerRes = player.PlayerActor.Trait<PowerManager>();
			var power = template.Get<LabelWidget>("POWER");
			power.GetText = () => powerRes.PowerDrained + "/" + powerRes.PowerProvided;
			power.GetColor = () => GetPowerColor(powerRes.PowerState);

			template.Get<LabelWidget>("KILLS").GetText = () => player.Kills.ToString();
			template.Get<LabelWidget>("DEATHS").GetText = () => player.Deaths.ToString();

			var production = template.Get<ObserverProductionIconsWidget>("PRODUCTION_ICONS");
			production.GetPlayer = () => player;

			return template;
		}

		ScrollItemWidget SetupPlayerScrollItemWidget(ScrollItemWidget template, Player player)
		{
			return ScrollItemWidget.Setup(template, () => false, () =>
			{
				var playerBase = world.Actors.FirstOrDefault(a => !a.IsDead() && a.HasTrait<BaseBuilding>() && a.Owner == player);
				if (playerBase != null)
				{
					Game.MoveViewport(playerBase.Location.ToFloat2());
				}
			});
		}

		static void AddPlayerFlagAndName(ScrollItemWidget template, Player player)
		{
			var flag = template.Get<ImageWidget>("FLAG");
			flag.GetImageName = () => player.Country.Race;
			flag.GetImageCollection = () => "flags";

			var playerName = template.Get<LabelWidget>("PLAYER");
			playerName.GetText = () => player.PlayerName + (player.WinState == WinState.Undefined ? "" : " (" + player.WinState + ")");
			playerName.GetColor = () => player.ColorRamp.GetColor(0);
		}

		static void InitializeWidgets(params Widget[] widgets)
		{
			var args = new WidgetArgs();
			foreach (var widget in widgets)
			{
				widget.Initialize(args);
			}
		}

		static Color GetPowerColor(PowerState state)
		{
			if (state == PowerState.Critical) return Color.Red;
			if (state == PowerState.Low) return Color.Orange;
			return Color.LimeGreen;
		}

		class StatsDropDownOption
		{
			public string Title;
			public Func<bool> IsSelected;
			public Action OnClick;
		}
	}
}
