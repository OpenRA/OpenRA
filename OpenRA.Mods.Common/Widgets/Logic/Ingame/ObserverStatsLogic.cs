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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Lint;
using OpenRA.Mods.Common.Traits;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public enum ObserverStatsPanel { None, Basic, Economy, Production, SupportPowers, Combat, Graph, ArmyGraph }

	[ChromeLogicArgsHotkeys("StatisticsBasicKey", "StatisticsEconomyKey", "StatisticsProductionKey", "StatisticsSupportPowersKey", "StatisticsCombatKey", "StatisticsGraphKey",
		"StatisticsArmyGraphKey")]
	public class ObserverStatsLogic : ChromeLogic
	{
		readonly ContainerWidget basicStatsHeaders;
		readonly ContainerWidget economyStatsHeaders;
		readonly ContainerWidget productionStatsHeaders;
		readonly ContainerWidget supportPowerStatsHeaders;
		readonly ContainerWidget combatStatsHeaders;
		readonly ContainerWidget earnedThisMinuteGraphHeaders;
		readonly ContainerWidget armyThisMinuteGraphHeaders;
		readonly ScrollPanelWidget playerStatsPanel;
		readonly ScrollItemWidget basicPlayerTemplate;
		readonly ScrollItemWidget economyPlayerTemplate;
		readonly ScrollItemWidget productionPlayerTemplate;
		readonly ScrollItemWidget supportPowersPlayerTemplate;
		readonly ScrollItemWidget combatPlayerTemplate;
		readonly ContainerWidget earnedThisMinuteGraphContainer;
		readonly ContainerWidget armyThisMinuteGraphContainer;
		readonly LineGraphWidget earnedThisMinuteGraph;
		readonly LineGraphWidget armyThisMinuteGraph;
		readonly ScrollItemWidget teamTemplate;
		readonly IEnumerable<Player> players;
		readonly IOrderedEnumerable<IGrouping<int, Player>> teams;
		readonly bool hasTeams;
		readonly World world;
		readonly WorldRenderer worldRenderer;

		readonly string clickSound = ChromeMetrics.Get<string>("ClickSound");
		bool noneSelected = true;

		[ObjectCreator.UseCtor]
		public ObserverStatsLogic(World world, ModData modData, WorldRenderer worldRenderer, Widget widget, Dictionary<string, MiniYaml> logicArgs)
		{
			this.world = world;
			this.worldRenderer = worldRenderer;

			MiniYaml yaml;
			string[] keyNames = Enum.GetNames(typeof(ObserverStatsPanel));
			var statsHotkeys = new HotkeyReference[keyNames.Length];
			for (var i = 0; i < keyNames.Length; i++)
				statsHotkeys[i] = logicArgs.TryGetValue("Statistics" + keyNames[i] + "Key", out yaml) ? modData.Hotkeys[yaml.Value] : new HotkeyReference();

			players = world.Players.Where(p => !p.NonCombatant);
			teams = players.GroupBy(p => (world.LobbyInfo.ClientWithIndex(p.ClientIndex) ?? new Session.Client()).Team).OrderBy(g => g.Key);
			hasTeams = !(teams.Count() == 1 && teams.First().Key == 0);

			basicStatsHeaders = widget.Get<ContainerWidget>("BASIC_STATS_HEADERS");
			economyStatsHeaders = widget.Get<ContainerWidget>("ECONOMY_STATS_HEADERS");
			productionStatsHeaders = widget.Get<ContainerWidget>("PRODUCTION_STATS_HEADERS");
			supportPowerStatsHeaders = widget.Get<ContainerWidget>("SUPPORT_POWERS_HEADERS");
			combatStatsHeaders = widget.Get<ContainerWidget>("COMBAT_STATS_HEADERS");

			earnedThisMinuteGraphHeaders = widget.Get<ContainerWidget>("EARNED_THIS_MIN_GRAPH_HEADERS");
			armyThisMinuteGraphHeaders = widget.Get<ContainerWidget>("ARMY_THIS_MIN_GRAPH_HEADERS");

			playerStatsPanel = widget.Get<ScrollPanelWidget>("PLAYER_STATS_PANEL");
			playerStatsPanel.Layout = new GridLayout(playerStatsPanel);
			playerStatsPanel.IgnoreMouseOver = true;

			if (ShowScrollBar)
			{
				playerStatsPanel.ScrollBar = ScrollBar.Left;

				AdjustHeader(basicStatsHeaders);
				AdjustHeader(economyStatsHeaders);
				AdjustHeader(productionStatsHeaders);
				AdjustHeader(supportPowerStatsHeaders);
				AdjustHeader(combatStatsHeaders);
			}

			basicPlayerTemplate = playerStatsPanel.Get<ScrollItemWidget>("BASIC_PLAYER_TEMPLATE");
			economyPlayerTemplate = playerStatsPanel.Get<ScrollItemWidget>("ECONOMY_PLAYER_TEMPLATE");
			productionPlayerTemplate = playerStatsPanel.Get<ScrollItemWidget>("PRODUCTION_PLAYER_TEMPLATE");
			supportPowersPlayerTemplate = playerStatsPanel.Get<ScrollItemWidget>("SUPPORT_POWERS_PLAYER_TEMPLATE");
			combatPlayerTemplate = playerStatsPanel.Get<ScrollItemWidget>("COMBAT_PLAYER_TEMPLATE");

			earnedThisMinuteGraphContainer = widget.Get<ContainerWidget>("EARNED_THIS_MIN_GRAPH_CONTAINER");
			earnedThisMinuteGraph = earnedThisMinuteGraphContainer.Get<LineGraphWidget>("EARNED_THIS_MIN_GRAPH");

			armyThisMinuteGraphContainer = widget.Get<ContainerWidget>("ARMY_THIS_MIN_GRAPH_CONTAINER");
			armyThisMinuteGraph = armyThisMinuteGraphContainer.Get<LineGraphWidget>("ARMY_THIS_MIN_GRAPH");

			teamTemplate = playerStatsPanel.Get<ScrollItemWidget>("TEAM_TEMPLATE");

			var statsDropDown = widget.Get<DropDownButtonWidget>("STATS_DROPDOWN");
			Func<string, ContainerWidget, ScrollItemWidget, Action, StatsDropDownOption> createStatsOption = (title, headers, template, a) =>
			{
				return new StatsDropDownOption
				{
					Title = title,
					IsSelected = () => headers.Visible,
					OnClick = () =>
					{
						noneSelected = false;
						ClearStats();
						playerStatsPanel.Visible = true;
						statsDropDown.GetText = () => title;
						if (template != null)
							AdjustStatisticsPanel(template);

						a();
						Ui.ResetTooltips();
					}
				};
			};

			var statsDropDownOptions = new StatsDropDownOption[]
			{
				new StatsDropDownOption
				{
					Title = "Information: None",
					IsSelected = () => noneSelected,
					OnClick = () =>
					{
						noneSelected = true;
						statsDropDown.GetText = () => "Information: None";
						playerStatsPanel.Visible = false;
						ClearStats();
					}
				},
				createStatsOption("Basic", basicStatsHeaders, basicPlayerTemplate, () => DisplayStats(BasicStats)),
				createStatsOption("Economy", economyStatsHeaders, economyPlayerTemplate, () => DisplayStats(EconomyStats)),
				createStatsOption("Production", productionStatsHeaders, productionPlayerTemplate, () => DisplayStats(ProductionStats)),
				createStatsOption("Support Powers", supportPowerStatsHeaders, supportPowersPlayerTemplate, () => DisplayStats(SupportPowerStats)),
				createStatsOption("Combat", combatStatsHeaders, combatPlayerTemplate, () => DisplayStats(CombatStats)),
				createStatsOption("Earnings (graph)", earnedThisMinuteGraphHeaders, null, () => EarnedThisMinuteGraph()),
				createStatsOption("Army (graph)", armyThisMinuteGraphHeaders, null, () => ArmyThisMinuteGraph()),
			};

			Func<StatsDropDownOption, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
			{
				var item = ScrollItemWidget.Setup(template, option.IsSelected, option.OnClick);
				item.Get<LabelWidget>("LABEL").GetText = () => option.Title;
				return item;
			};

			var statsDropDownPanelTemplate = logicArgs.TryGetValue("StatsDropDownPanelTemplate", out yaml) ? yaml.Value : "LABEL_DROPDOWN_TEMPLATE";

			statsDropDown.OnMouseDown = _ => statsDropDown.ShowDropDown(statsDropDownPanelTemplate, 205, statsDropDownOptions, setupItem);
			statsDropDownOptions[0].OnClick();

			var keyListener = statsDropDown.Get<LogicKeyListenerWidget>("STATS_DROPDOWN_KEYHANDLER");
			keyListener.AddHandler(e =>
			{
				if (e.Event == KeyInputEvent.Down && !e.IsRepeat)
				{
					for (var i = 0; i < statsHotkeys.Length; i++)
					{
						if (statsHotkeys[i].IsActivatedBy(e))
						{
							Game.Sound.PlayNotification(modData.DefaultRules, null, "Sounds", clickSound, null);
							statsDropDownOptions[i].OnClick();
							return true;
						}
					}
				}

				return false;
			});

			if (logicArgs.TryGetValue("ClickSound", out yaml))
				clickSound = yaml.Value;
		}

		void ClearStats()
		{
			playerStatsPanel.Children.Clear();
			basicStatsHeaders.Visible = false;
			economyStatsHeaders.Visible = false;
			productionStatsHeaders.Visible = false;
			supportPowerStatsHeaders.Visible = false;
			combatStatsHeaders.Visible = false;
			earnedThisMinuteGraphHeaders.Visible = false;
			armyThisMinuteGraphHeaders.Visible = false;

			earnedThisMinuteGraphContainer.Visible = false;
			armyThisMinuteGraphContainer.Visible = false;

			earnedThisMinuteGraph.GetSeries = null;
			armyThisMinuteGraph.GetSeries = null;
		}

		void EarnedThisMinuteGraph()
		{
			playerStatsPanel.Visible = false;
			earnedThisMinuteGraphHeaders.Visible = true;
			earnedThisMinuteGraphContainer.Visible = true;

			earnedThisMinuteGraph.GetSeries = () =>
				players.Select(p => new LineGraphSeries(
					p.PlayerName,
					p.Color,
					(p.PlayerActor.TraitOrDefault<PlayerStatistics>() ?? new PlayerStatistics(p.PlayerActor)).EarnedSamples.Select(s => (float)s)));
		}

		void ArmyThisMinuteGraph()
		{
			playerStatsPanel.Visible = false;
			armyThisMinuteGraphHeaders.Visible = true;
			armyThisMinuteGraphContainer.Visible = true;

			armyThisMinuteGraph.GetSeries = () =>
				players.Select(p => new LineGraphSeries(
					p.PlayerName,
					p.Color,
					(p.PlayerActor.TraitOrDefault<PlayerStatistics>() ?? new PlayerStatistics(p.PlayerActor)).ArmySamples.Select(s => (float)s)));
		}

		void DisplayStats(Func<Player, ScrollItemWidget> createItem)
		{
			foreach (var team in teams)
			{
				if (hasTeams)
				{
					var tt = ScrollItemWidget.Setup(teamTemplate, () => false, () => { });
					tt.IgnoreMouseOver = true;

					var teamLabel = tt.Get<LabelWidget>("TEAM");
					teamLabel.GetText = () => team.Key == 0 ? "No Team" : "Team " + team.Key;
					tt.Bounds.Width = teamLabel.Bounds.Width = Game.Renderer.Fonts[tt.Font].Measure(tt.Get<LabelWidget>("TEAM").GetText()).X;

					var colorBlockWidget = tt.Get<ColorBlockWidget>("TEAM_COLOR");
					var scrollBarOffset = playerStatsPanel.ScrollBar != ScrollBar.Hidden
						? playerStatsPanel.ScrollbarWidth
						: 0;
					var boundsWidth = tt.Parent.Bounds.Width - scrollBarOffset;
					colorBlockWidget.Bounds.Width = boundsWidth - 200;

					var gradient = tt.Get<GradientColorBlockWidget>("TEAM_GRADIENT");
					gradient.Bounds.X = boundsWidth - 200;

					playerStatsPanel.AddChild(tt);
				}

				foreach (var p in team)
				{
					var player = p;
					playerStatsPanel.AddChild(createItem(player));
				}
			}
		}

		ScrollItemWidget CombatStats(Player player)
		{
			combatStatsHeaders.Visible = true;
			var template = SetupPlayerScrollItemWidget(combatPlayerTemplate, player);

			LobbyUtils.AddPlayerFlagAndName(template, player);

			var playerName = template.Get<LabelWidget>("PLAYER");
			playerName.GetColor = () => Color.White;

			var playerColor = template.Get<ColorBlockWidget>("PLAYER_COLOR");
			var playerGradient = template.Get<GradientColorBlockWidget>("PLAYER_GRADIENT");

			SetupPlayerColor(player, template, playerColor, playerGradient);

			var stats = player.PlayerActor.TraitOrDefault<PlayerStatistics>();
			if (stats == null) return template;
			template.Get<LabelWidget>("ASSETS_DESTROYED").GetText = () => "$" + stats.KillsCost;
			template.Get<LabelWidget>("ASSETS_LOST").GetText = () => "$" + stats.DeathsCost;
			template.Get<LabelWidget>("UNITS_KILLED").GetText = () => stats.UnitsKilled.ToString();
			template.Get<LabelWidget>("UNITS_DEAD").GetText = () => stats.UnitsDead.ToString();
			template.Get<LabelWidget>("BUILDINGS_KILLED").GetText = () => stats.BuildingsKilled.ToString();
			template.Get<LabelWidget>("BUILDINGS_DEAD").GetText = () => stats.BuildingsDead.ToString();
			template.Get<LabelWidget>("ARMY_VALUE").GetText = () => "$" + stats.ArmyValue.ToString();

			return template;
		}

		ScrollItemWidget ProductionStats(Player player)
		{
			productionStatsHeaders.Visible = true;
			var template = SetupPlayerScrollItemWidget(productionPlayerTemplate, player);

			LobbyUtils.AddPlayerFlagAndName(template, player);

			var playerName = template.Get<LabelWidget>("PLAYER");
			playerName.GetColor = () => Color.White;

			var playerColor = template.Get<ColorBlockWidget>("PLAYER_COLOR");
			var playerGradient = template.Get<GradientColorBlockWidget>("PLAYER_GRADIENT");

			SetupPlayerColor(player, template, playerColor, playerGradient);

			template.Get<ObserverProductionIconsWidget>("PRODUCTION_ICONS").GetPlayer = () => player;
			template.IgnoreChildMouseOver = false;

			return template;
		}

		ScrollItemWidget SupportPowerStats(Player player)
		{
			supportPowerStatsHeaders.Visible = true;
			var template = SetupPlayerScrollItemWidget(supportPowersPlayerTemplate, player);

			LobbyUtils.AddPlayerFlagAndName(template, player);

			var playerName = template.Get<LabelWidget>("PLAYER");
			playerName.GetColor = () => Color.White;

			var playerColor = template.Get<ColorBlockWidget>("PLAYER_COLOR");
			var playerGradient = template.Get<GradientColorBlockWidget>("PLAYER_GRADIENT");

			SetupPlayerColor(player, template, playerColor, playerGradient);

			template.Get<ObserverSupportPowerIconsWidget>("SUPPORT_POWER_ICONS").GetPlayer = () => player;
			template.IgnoreChildMouseOver = false;

			return template;
		}

		ScrollItemWidget EconomyStats(Player player)
		{
			economyStatsHeaders.Visible = true;
			var template = SetupPlayerScrollItemWidget(economyPlayerTemplate, player);

			LobbyUtils.AddPlayerFlagAndName(template, player);

			var playerName = template.Get<LabelWidget>("PLAYER");
			playerName.GetColor = () => Color.White;

			var playerColor = template.Get<ColorBlockWidget>("PLAYER_COLOR");
			var playerGradient = template.Get<GradientColorBlockWidget>("PLAYER_GRADIENT");

			SetupPlayerColor(player, template, playerColor, playerGradient);

			var res = player.PlayerActor.Trait<PlayerResources>();
			var stats = player.PlayerActor.TraitOrDefault<PlayerStatistics>();
			if (stats == null) return template;

			template.Get<LabelWidget>("CASH").GetText = () => "$" + (res.Cash + res.Resources);
			template.Get<LabelWidget>("EARNED_MIN").GetText = () => AverageEarnedPerMinute(res.Earned);
			template.Get<LabelWidget>("EARNED_THIS_MIN").GetText = () => "$" + stats.EarnedThisMinute;
			template.Get<LabelWidget>("EARNED").GetText = () => "$" + res.Earned;
			template.Get<LabelWidget>("SPENT").GetText = () => "$" + res.Spent;

			var assets = template.Get<LabelWidget>("ASSETS");
			assets.GetText = () => "$" + world.ActorsHavingTrait<Valued>()
				.Where(a => a.Owner == player && !a.IsDead)
				.Sum(a => a.Info.TraitInfos<ValuedInfo>().First().Cost);

			var harvesters = template.Get<LabelWidget>("HARVESTERS");
			harvesters.GetText = () => world.ActorsHavingTrait<Harvester>().Count(a => a.Owner == player && !a.IsDead).ToString();

			return template;
		}

		ScrollItemWidget BasicStats(Player player)
		{
			basicStatsHeaders.Visible = true;
			var template = SetupPlayerScrollItemWidget(basicPlayerTemplate, player);

			LobbyUtils.AddPlayerFlagAndName(template, player);

			var playerName = template.Get<LabelWidget>("PLAYER");
			playerName.GetColor = () => Color.White;

			var playerColor = template.Get<ColorBlockWidget>("PLAYER_COLOR");
			var playerGradient = template.Get<GradientColorBlockWidget>("PLAYER_GRADIENT");

			SetupPlayerColor(player, template, playerColor, playerGradient);

			var res = player.PlayerActor.Trait<PlayerResources>();
			template.Get<LabelWidget>("CASH").GetText = () => "$" + (res.Cash + res.Resources);
			template.Get<LabelWidget>("EARNED_MIN").GetText = () => AverageEarnedPerMinute(res.Earned);

			var powerRes = player.PlayerActor.TraitOrDefault<PowerManager>();
			if (powerRes != null)
			{
				var power = template.Get<LabelWidget>("POWER");
				power.GetText = () => powerRes.PowerDrained + "/" + powerRes.PowerProvided;
				power.GetColor = () => GetPowerColor(powerRes.PowerState);
			}

			var stats = player.PlayerActor.TraitOrDefault<PlayerStatistics>();
			if (stats == null) return template;
			template.Get<LabelWidget>("KILLS").GetText = () => (stats.UnitsKilled + stats.BuildingsKilled).ToString();
			template.Get<LabelWidget>("DEATHS").GetText = () => (stats.UnitsDead + stats.BuildingsDead).ToString();
			template.Get<LabelWidget>("ASSETS_DESTROYED").GetText = () => "$" + stats.KillsCost;
			template.Get<LabelWidget>("ASSETS_LOST").GetText = () => "$" + stats.DeathsCost;
			template.Get<LabelWidget>("EXPERIENCE").GetText = () => stats.Experience.ToString();
			template.Get<LabelWidget>("ACTIONS_MIN").GetText = () => AverageOrdersPerMinute(stats.OrderCount);

			return template;
		}

		void SetupPlayerColor(Player player, ScrollItemWidget template, ColorBlockWidget colorBlockWidget, GradientColorBlockWidget gradientColorBlockWidget)
		{
			var color = Color.FromArgb(128, player.Color.R, player.Color.G, player.Color.B);
			var hoverColor = Color.FromArgb(192, player.Color.R, player.Color.G, player.Color.B);

			var isMouseOver = new CachedTransform<Widget, bool>(w => w == template || template.Children.Contains(w));

			colorBlockWidget.GetColor = () => isMouseOver.Update(Ui.MouseOverWidget) ? hoverColor : color;

			gradientColorBlockWidget.GetTopLeftColor = () => isMouseOver.Update(Ui.MouseOverWidget) ? hoverColor : color;
			gradientColorBlockWidget.GetBottomLeftColor = () => isMouseOver.Update(Ui.MouseOverWidget) ? hoverColor : color;
			gradientColorBlockWidget.GetTopRightColor = () => isMouseOver.Update(Ui.MouseOverWidget) ? hoverColor : Color.Transparent;
			gradientColorBlockWidget.GetBottomRightColor = () => isMouseOver.Update(Ui.MouseOverWidget) ? hoverColor : Color.Transparent;
		}

		ScrollItemWidget SetupPlayerScrollItemWidget(ScrollItemWidget template, Player player)
		{
			return ScrollItemWidget.Setup(template, () => false, () =>
			{
				var playerBase = world.ActorsHavingTrait<BaseBuilding>().FirstOrDefault(a => !a.IsDead && a.Owner == player);
				if (playerBase != null)
					worldRenderer.Viewport.Center(playerBase.CenterPosition);
			});
		}

		void AdjustStatisticsPanel(Widget itemTemplate)
		{
			var height = playerStatsPanel.Bounds.Height;

			var scrollbarWidth = playerStatsPanel.ScrollBar != ScrollBar.Hidden ? playerStatsPanel.ScrollbarWidth : 0;
			playerStatsPanel.Bounds.Width = itemTemplate.Bounds.Width + scrollbarWidth;

			if (playerStatsPanel.Bounds.Height < height)
				playerStatsPanel.ScrollToTop();
		}

		void AdjustHeader(ContainerWidget headerTemplate)
		{
			var offset = playerStatsPanel.ScrollbarWidth;

			headerTemplate.Get<ColorBlockWidget>("HEADER_COLOR").Bounds.Width += offset;
			headerTemplate.Get<GradientColorBlockWidget>("HEADER_GRADIENT").Bounds.X += offset;

			foreach (var headerLabel in headerTemplate.Children.OfType<LabelWidget>())
			{
				headerLabel.Bounds.X += offset;
			}
		}

		string AverageOrdersPerMinute(double orders)
		{
			return (world.WorldTick == 0 ? 0 : orders / (world.WorldTick / 1500.0)).ToString("F1");
		}

		string AverageEarnedPerMinute(double earned)
		{
			return "$" + (world.WorldTick == 0 ? 0 : earned / (world.WorldTick / 1500.0)).ToString("F0");
		}

		static Color GetPowerColor(PowerState state)
		{
			if (state == PowerState.Critical) return Color.Red;
			if (state == PowerState.Low) return Color.Orange;
			return Color.LimeGreen;
		}

		// HACK The height of the templates and the scrollpanel needs to be kept in synch
		bool ShowScrollBar
		{
			get { return players.Count() + (hasTeams ? teams.Count() : 0) > 10; }
		}

		class StatsDropDownOption
		{
			public string Title;
			public Func<bool> IsSelected;
			public Action OnClick;
		}
	}
}
