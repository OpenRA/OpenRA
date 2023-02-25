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
using System.Globalization;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Lint;
using OpenRA.Mods.Common.Traits;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public enum ObserverStatsPanel { None, Basic, Economy, Production, SupportPowers, Combat, Army, Graph, ArmyGraph }

	[ChromeLogicArgsHotkeys("StatisticsBasicKey", "StatisticsEconomyKey", "StatisticsProductionKey", "StatisticsSupportPowersKey", "StatisticsCombatKey", "StatisticsArmyKey", "StatisticsGraphKey",
		"StatisticsArmyGraphKey")]
	public class ObserverStatsLogic : ChromeLogic
	{
		[TranslationReference]
		const string InformationNone = "options-observer-stats.none";

		[TranslationReference]
		const string Basic = "options-observer-stats.basic";

		[TranslationReference]
		const string Economy = "options-observer-stats.economy";

		[TranslationReference]
		const string Production = "options-observer-stats.production";

		[TranslationReference]
		const string SupportPowers = "options-observer-stats.support-powers";

		[TranslationReference]
		const string Combat = "options-observer-stats.combat";

		[TranslationReference]
		const string Army = "options-observer-stats.army";

		[TranslationReference]
		const string EarningsGraph = "options-observer-stats.earnings-graph";

		[TranslationReference]
		const string ArmyGraph = "options-observer-stats.army-graph";

		[TranslationReference("team")]
		const string TeamNumber = "label-team-name";

		[TranslationReference]
		const string NoTeam = "label-no-team";

		readonly ContainerWidget basicStatsHeaders;
		readonly ContainerWidget economyStatsHeaders;
		readonly ContainerWidget productionStatsHeaders;
		readonly ContainerWidget supportPowerStatsHeaders;
		readonly ContainerWidget combatStatsHeaders;
		readonly ContainerWidget armyHeaders;
		readonly ScrollPanelWidget playerStatsPanel;
		readonly ScrollItemWidget basicPlayerTemplate;
		readonly ScrollItemWidget economyPlayerTemplate;
		readonly ScrollItemWidget productionPlayerTemplate;
		readonly ScrollItemWidget supportPowersPlayerTemplate;
		readonly ScrollItemWidget armyPlayerTemplate;
		readonly ScrollItemWidget combatPlayerTemplate;
		readonly ContainerWidget incomeGraphContainer;
		readonly ContainerWidget armyValueGraphContainer;
		readonly LineGraphWidget incomeGraph;
		readonly LineGraphWidget armyValueGraph;
		readonly ScrollItemWidget teamTemplate;
		readonly IEnumerable<Player> players;
		readonly IOrderedEnumerable<IGrouping<int, Player>> teams;
		readonly bool hasTeams;
		readonly World world;
		readonly WorldRenderer worldRenderer;

		readonly string clickSound = ChromeMetrics.Get<string>("ClickSound");
		ObserverStatsPanel activePanel;

		[ObjectCreator.UseCtor]
		public ObserverStatsLogic(World world, ModData modData, WorldRenderer worldRenderer, Widget widget, Dictionary<string, MiniYaml> logicArgs)
		{
			this.world = world;
			this.worldRenderer = worldRenderer;

			MiniYaml yaml;
			var keyNames = Enum.GetNames(typeof(ObserverStatsPanel));
			var statsHotkeys = new HotkeyReference[keyNames.Length];
			for (var i = 0; i < keyNames.Length; i++)
				statsHotkeys[i] = logicArgs.TryGetValue("Statistics" + keyNames[i] + "Key", out yaml) ? modData.Hotkeys[yaml.Value] : new HotkeyReference();

			players = world.Players.Where(p => !p.NonCombatant && p.Playable);
			teams = players.GroupBy(p => (world.LobbyInfo.ClientWithIndex(p.ClientIndex) ?? new Session.Client()).Team).OrderBy(g => g.Key);
			hasTeams = !(teams.Count() == 1 && teams.First().Key == 0);

			basicStatsHeaders = widget.Get<ContainerWidget>("BASIC_STATS_HEADERS");
			economyStatsHeaders = widget.Get<ContainerWidget>("ECONOMY_STATS_HEADERS");
			productionStatsHeaders = widget.Get<ContainerWidget>("PRODUCTION_STATS_HEADERS");
			supportPowerStatsHeaders = widget.Get<ContainerWidget>("SUPPORT_POWERS_HEADERS");
			armyHeaders = widget.Get<ContainerWidget>("ARMY_HEADERS");
			combatStatsHeaders = widget.Get<ContainerWidget>("COMBAT_STATS_HEADERS");

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
				AdjustHeader(armyHeaders);
			}

			basicPlayerTemplate = playerStatsPanel.Get<ScrollItemWidget>("BASIC_PLAYER_TEMPLATE");
			economyPlayerTemplate = playerStatsPanel.Get<ScrollItemWidget>("ECONOMY_PLAYER_TEMPLATE");
			productionPlayerTemplate = playerStatsPanel.Get<ScrollItemWidget>("PRODUCTION_PLAYER_TEMPLATE");
			supportPowersPlayerTemplate = playerStatsPanel.Get<ScrollItemWidget>("SUPPORT_POWERS_PLAYER_TEMPLATE");
			armyPlayerTemplate = playerStatsPanel.Get<ScrollItemWidget>("ARMY_PLAYER_TEMPLATE");
			combatPlayerTemplate = playerStatsPanel.Get<ScrollItemWidget>("COMBAT_PLAYER_TEMPLATE");

			incomeGraphContainer = widget.Get<ContainerWidget>("INCOME_GRAPH_CONTAINER");
			incomeGraph = incomeGraphContainer.Get<LineGraphWidget>("INCOME_GRAPH");

			armyValueGraphContainer = widget.Get<ContainerWidget>("ARMY_VALUE_GRAPH_CONTAINER");
			armyValueGraph = armyValueGraphContainer.Get<LineGraphWidget>("ARMY_VALUE_GRAPH");

			teamTemplate = playerStatsPanel.Get<ScrollItemWidget>("TEAM_TEMPLATE");

			var statsDropDown = widget.Get<DropDownButtonWidget>("STATS_DROPDOWN");
			StatsDropDownOption CreateStatsOption(string title, ObserverStatsPanel panel, ScrollItemWidget template, Action a)
			{
				title = TranslationProvider.GetString(title);
				return new StatsDropDownOption
				{
					Title = TranslationProvider.GetString(title),
					IsSelected = () => activePanel == panel,
					OnClick = () =>
					{
						ClearStats();
						playerStatsPanel.Visible = true;
						statsDropDown.GetText = () => title;
						activePanel = panel;
						if (template != null)
							AdjustStatisticsPanel(template);

						a();
						Ui.ResetTooltips();
					}
				};
			}

			var statsDropDownOptions = new StatsDropDownOption[]
			{
				new()
				{
					Title = TranslationProvider.GetString(InformationNone),
					IsSelected = () => activePanel == ObserverStatsPanel.None,
					OnClick = () =>
					{
						var informationNone = TranslationProvider.GetString(InformationNone);
						statsDropDown.GetText = () => informationNone;
						playerStatsPanel.Visible = false;
						ClearStats();
						activePanel = ObserverStatsPanel.None;
					}
				},
				CreateStatsOption(Basic, ObserverStatsPanel.Basic, basicPlayerTemplate, () => DisplayStats(BasicStats)),
				CreateStatsOption(Economy, ObserverStatsPanel.Economy, economyPlayerTemplate, () => DisplayStats(EconomyStats)),
				CreateStatsOption(Production, ObserverStatsPanel.Production, productionPlayerTemplate, () => DisplayStats(ProductionStats)),
				CreateStatsOption(SupportPowers, ObserverStatsPanel.SupportPowers, supportPowersPlayerTemplate, () => DisplayStats(SupportPowerStats)),
				CreateStatsOption(Combat, ObserverStatsPanel.Combat, combatPlayerTemplate, () => DisplayStats(CombatStats)),
				CreateStatsOption(Army, ObserverStatsPanel.Army, armyPlayerTemplate, () => DisplayStats(ArmyStats)),
				CreateStatsOption(EarningsGraph, ObserverStatsPanel.Graph, null, () => IncomeGraph()),
				CreateStatsOption(ArmyGraph, ObserverStatsPanel.ArmyGraph, null, () => ArmyValueGraph()),
			};

			ScrollItemWidget SetupItem(StatsDropDownOption option, ScrollItemWidget template)
			{
				var item = ScrollItemWidget.Setup(template, option.IsSelected, option.OnClick);
				item.Get<LabelWidget>("LABEL").GetText = () => option.Title;
				return item;
			}

			var statsDropDownPanelTemplate = logicArgs.TryGetValue("StatsDropDownPanelTemplate", out yaml) ? yaml.Value : "LABEL_DROPDOWN_TEMPLATE";

			statsDropDown.OnMouseDown = _ => statsDropDown.ShowDropDown(statsDropDownPanelTemplate, 230, statsDropDownOptions, SetupItem);
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
			armyHeaders.Visible = false;
			combatStatsHeaders.Visible = false;

			incomeGraphContainer.Visible = false;
			armyValueGraphContainer.Visible = false;

			incomeGraph.GetSeries = null;
			armyValueGraph.GetSeries = null;
		}

		void IncomeGraph()
		{
			playerStatsPanel.Visible = false;
			incomeGraphContainer.Visible = true;

			incomeGraph.GetSeries = () =>
				players.Select(p => new LineGraphSeries(
					p.PlayerName,
					p.Color,
					(p.PlayerActor.TraitOrDefault<PlayerStatistics>() ?? new PlayerStatistics(p.PlayerActor)).IncomeSamples.Select(s => (float)s)));
		}

		void ArmyValueGraph()
		{
			playerStatsPanel.Visible = false;
			armyValueGraphContainer.Visible = true;

			armyValueGraph.GetSeries = () =>
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
					var teamText = team.Key > 0 ? TranslationProvider.GetString(TeamNumber, Translation.Arguments("team", team.Key))
						: TranslationProvider.GetString(NoTeam);
					teamLabel.GetText = () => teamText;
					tt.Bounds.Width = teamLabel.Bounds.Width = Game.Renderer.Fonts[tt.Font].Measure(teamText).X;

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

			AddPlayerFlagAndName(template, player);

			var playerName = template.Get<LabelWidget>("PLAYER");
			playerName.GetColor = () => Color.White;

			var playerColor = template.Get<ColorBlockWidget>("PLAYER_COLOR");
			var playerGradient = template.Get<GradientColorBlockWidget>("PLAYER_GRADIENT");

			SetupPlayerColor(player, template, playerColor, playerGradient);

			var stats = player.PlayerActor.TraitOrDefault<PlayerStatistics>();
			if (stats == null)
				return template;

			var destroyedText = new CachedTransform<int, string>(i => "$" + i.ToString(NumberFormatInfo.CurrentInfo));
			template.Get<LabelWidget>("ASSETS_DESTROYED").GetText = () => destroyedText.Update(stats.KillsCost);

			var lostText = new CachedTransform<int, string>(i => "$" + i.ToString(NumberFormatInfo.CurrentInfo));
			template.Get<LabelWidget>("ASSETS_LOST").GetText = () => lostText.Update(stats.DeathsCost);

			var unitsKilledText = new CachedTransform<int, string>(i => i.ToString(NumberFormatInfo.CurrentInfo));
			template.Get<LabelWidget>("UNITS_KILLED").GetText = () => unitsKilledText.Update(stats.UnitsKilled);

			var unitsDeadText = new CachedTransform<int, string>(i => i.ToString(NumberFormatInfo.CurrentInfo));
			template.Get<LabelWidget>("UNITS_DEAD").GetText = () => unitsDeadText.Update(stats.UnitsDead);

			var buildingsKilledText = new CachedTransform<int, string>(i => i.ToString(NumberFormatInfo.CurrentInfo));
			template.Get<LabelWidget>("BUILDINGS_KILLED").GetText = () => buildingsKilledText.Update(stats.BuildingsKilled);

			var buildingsDeadText = new CachedTransform<int, string>(i => i.ToString(NumberFormatInfo.CurrentInfo));
			template.Get<LabelWidget>("BUILDINGS_DEAD").GetText = () => buildingsDeadText.Update(stats.BuildingsDead);

			var armyText = new CachedTransform<int, string>(i => "$" + i.ToString(NumberFormatInfo.CurrentInfo));
			template.Get<LabelWidget>("ARMY_VALUE").GetText = () => armyText.Update(stats.ArmyValue);

			var visionText = new CachedTransform<int, string>(i => Vision(i));
			template.Get<LabelWidget>("VISION").GetText = () => player.Shroud.Disabled ? "100%" : visionText.Update(player.Shroud.RevealedCells);

			return template;
		}

		ScrollItemWidget ProductionStats(Player player)
		{
			productionStatsHeaders.Visible = true;
			var template = SetupPlayerScrollItemWidget(productionPlayerTemplate, player);

			AddPlayerFlagAndName(template, player);

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

			AddPlayerFlagAndName(template, player);

			var playerName = template.Get<LabelWidget>("PLAYER");
			playerName.GetColor = () => Color.White;

			var playerColor = template.Get<ColorBlockWidget>("PLAYER_COLOR");
			var playerGradient = template.Get<GradientColorBlockWidget>("PLAYER_GRADIENT");

			SetupPlayerColor(player, template, playerColor, playerGradient);

			template.Get<ObserverSupportPowerIconsWidget>("SUPPORT_POWER_ICONS").GetPlayer = () => player;
			template.IgnoreChildMouseOver = false;

			return template;
		}

		ScrollItemWidget ArmyStats(Player player)
		{
			armyHeaders.Visible = true;
			var template = SetupPlayerScrollItemWidget(armyPlayerTemplate, player);

			AddPlayerFlagAndName(template, player);

			var playerName = template.Get<LabelWidget>("PLAYER");
			playerName.GetColor = () => Color.White;

			var playerColor = template.Get<ColorBlockWidget>("PLAYER_COLOR");
			var playerGradient = template.Get<GradientColorBlockWidget>("PLAYER_GRADIENT");

			SetupPlayerColor(player, template, playerColor, playerGradient);

			template.Get<ObserverArmyIconsWidget>("ARMY_ICONS").GetPlayer = () => player;
			template.IgnoreChildMouseOver = false;

			return template;
		}

		ScrollItemWidget EconomyStats(Player player)
		{
			economyStatsHeaders.Visible = true;
			var template = SetupPlayerScrollItemWidget(economyPlayerTemplate, player);

			AddPlayerFlagAndName(template, player);

			var playerName = template.Get<LabelWidget>("PLAYER");
			playerName.GetColor = () => Color.White;

			var playerColor = template.Get<ColorBlockWidget>("PLAYER_COLOR");
			var playerGradient = template.Get<GradientColorBlockWidget>("PLAYER_GRADIENT");

			SetupPlayerColor(player, template, playerColor, playerGradient);

			var stats = player.PlayerActor.TraitOrDefault<PlayerStatistics>();
			if (stats == null)
				return template;

			var res = player.PlayerActor.Trait<PlayerResources>();
			var cashText = new CachedTransform<int, string>(i => "$" + i);
			template.Get<LabelWidget>("CASH").GetText = () => cashText.Update(res.GetCashAndResources());

			var incomeText = new CachedTransform<int, string>(i => "$" + i);
			template.Get<LabelWidget>("INCOME").GetText = () => incomeText.Update(stats.DisplayIncome);

			var earnedText = new CachedTransform<int, string>(i => "$" + i);
			template.Get<LabelWidget>("EARNED").GetText = () => earnedText.Update(res.Earned);

			var spentText = new CachedTransform<int, string>(i => "$" + i);
			template.Get<LabelWidget>("SPENT").GetText = () => spentText.Update(res.Spent);

			var assetsText = new CachedTransform<int, string>(i => "$" + i);
			template.Get<LabelWidget>("ASSETS").GetText = () => assetsText.Update(stats.AssetsValue);

			var harvesters = template.Get<LabelWidget>("HARVESTERS");
			harvesters.GetText = () => world.ActorsWithTrait<Harvester>().Count(a => a.Actor.Owner == player && !a.Actor.IsDead && !a.Trait.IsTraitDisabled).ToString(NumberFormatInfo.CurrentInfo);

			var carryalls = template.GetOrNull<LabelWidget>("CARRYALLS");
			if (carryalls != null)
				carryalls.GetText = () => world.ActorsWithTrait<AutoCarryall>().Count(a => a.Actor.Owner == player && !a.Actor.IsDead).ToString(NumberFormatInfo.CurrentInfo);

			var derricks = template.GetOrNull<LabelWidget>("DERRICKS");
			if (derricks != null)
				derricks.GetText = () => world.ActorsHavingTrait<UpdatesDerrickCount>().Count(a => a.Owner == player && !a.IsDead).ToString(NumberFormatInfo.CurrentInfo);

			return template;
		}

		ScrollItemWidget BasicStats(Player player)
		{
			basicStatsHeaders.Visible = true;
			var template = SetupPlayerScrollItemWidget(basicPlayerTemplate, player);

			AddPlayerFlagAndName(template, player);

			var playerName = template.Get<LabelWidget>("PLAYER");
			playerName.GetColor = () => Color.White;

			var playerColor = template.Get<ColorBlockWidget>("PLAYER_COLOR");
			var playerGradient = template.Get<GradientColorBlockWidget>("PLAYER_GRADIENT");

			SetupPlayerColor(player, template, playerColor, playerGradient);

			var res = player.PlayerActor.Trait<PlayerResources>();
			var cashText = new CachedTransform<int, string>(i => "$" + i);
			template.Get<LabelWidget>("CASH").GetText = () => cashText.Update(res.GetCashAndResources());

			var powerRes = player.PlayerActor.TraitOrDefault<PowerManager>();
			if (powerRes != null)
			{
				var power = template.Get<LabelWidget>("POWER");
				var powerText = new CachedTransform<(int PowerDrained, int PowerProvided), string>(p => p.PowerDrained + "/" + p.PowerProvided);
				power.GetText = () => powerText.Update((powerRes.PowerDrained, powerRes.PowerProvided));
				power.GetColor = () => GetPowerColor(powerRes.PowerState);
			}

			var stats = player.PlayerActor.TraitOrDefault<PlayerStatistics>();
			if (stats == null)
				return template;

			var killsText = new CachedTransform<int, string>(i => i.ToString(NumberFormatInfo.CurrentInfo));
			template.Get<LabelWidget>("KILLS").GetText = () => killsText.Update(stats.UnitsKilled + stats.BuildingsKilled);

			var deathsText = new CachedTransform<int, string>(i => i.ToString(NumberFormatInfo.CurrentInfo));
			template.Get<LabelWidget>("DEATHS").GetText = () => deathsText.Update(stats.UnitsDead + stats.BuildingsDead);

			var destroyedText = new CachedTransform<int, string>(i => "$" + i.ToString(NumberFormatInfo.CurrentInfo));
			template.Get<LabelWidget>("ASSETS_DESTROYED").GetText = () => destroyedText.Update(stats.KillsCost);

			var lostText = new CachedTransform<int, string>(i => "$" + i.ToString(NumberFormatInfo.CurrentInfo));
			template.Get<LabelWidget>("ASSETS_LOST").GetText = () => lostText.Update(stats.DeathsCost);

			var experienceText = new CachedTransform<int, string>(i => i.ToString(NumberFormatInfo.CurrentInfo));
			template.Get<LabelWidget>("EXPERIENCE").GetText = () => experienceText.Update(stats.Experience);

			var actionsText = new CachedTransform<double, string>(d => AverageOrdersPerMinute(d));
			template.Get<LabelWidget>("ACTIONS_MIN").GetText = () => actionsText.Update(stats.OrderCount);

			return template;
		}

		static void SetupPlayerColor(Player player, ScrollItemWidget template, ColorBlockWidget colorBlockWidget, GradientColorBlockWidget gradientColorBlockWidget)
		{
			var pColor = player.Color;
			var color = Color.FromArgb(128, pColor.R, pColor.G, pColor.B);
			var hoverColor = Color.FromArgb(192, pColor.R, pColor.G, pColor.B);

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
				headerLabel.Bounds.X += offset;
		}

		static void AddPlayerFlagAndName(ScrollItemWidget template, Player player)
		{
			var flag = template.Get<ImageWidget>("FLAG");
			flag.GetImageCollection = () => "flags";
			flag.GetImageName = () => player.Faction.InternalName;

			var playerName = template.Get<LabelWidget>("PLAYER");
			WidgetUtils.BindPlayerNameAndStatus(playerName, player);

			playerName.GetColor = () => player.Color;
		}

		string AverageOrdersPerMinute(double orders)
		{
			return (world.WorldTick == 0 ? 0 : orders / (world.WorldTick / 1500.0)).ToString("F1", NumberFormatInfo.CurrentInfo);
		}

		string Vision(int revealedCells)
		{
			return (Math.Ceiling(revealedCells * 100d / world.Map.ProjectedCells.Length) / 100).ToString("P0", NumberFormatInfo.CurrentInfo);
		}

		static Color GetPowerColor(PowerState state)
		{
			if (state == PowerState.Critical)
				return Color.Red;

			if (state == PowerState.Low)
				return Color.Orange;

			return Color.LimeGreen;
		}

		// HACK The height of the templates and the scrollpanel needs to be kept in synch
		bool ShowScrollBar => players.Count() + (hasTeams ? teams.Count() : 0) > 10;

		sealed class StatsDropDownOption
		{
			public string Title;
			public Func<bool> IsSelected;
			public Action OnClick;
		}
	}
}
