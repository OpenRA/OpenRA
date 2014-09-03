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
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Network;
using OpenRA.Traits;
using OpenRA.Widgets;
using OpenRA.Mods.RA.Power;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class ObserverStatsDropDownOption {
		public string Title;
		public Func<bool> IsSelected;
		public Action OnClick;
		public Hotkey Key;
	}

	public class ObserverStatsHotkeyMgr : Widget
	{
		public List<ObserverStatsDropDownOption> DropdownOptions;

		public ObserverStatsHotkeyMgr()
		{
			Visible = false;
		}

		public override bool HandleKeyPressOuter(KeyInput e)
		{
			if (e.Event == KeyInputEvent.Down)
			{
				var option = DropdownOptions.Find(o => (o.Key.Key == e.Key));
				if (option != null)
				{
					option.OnClick();

					return true;
				}
			}

			return false;
		}
	}

	public class ObserverStatsLogic
	{
		SpacingWidget productionPlayerTemplate;
		SpacingWidget productionPanel;
		SpacingWidget supportPowerPlayerTemplate;
		SpacingWidget supportPowerPanel;
		BackgroundWidget earnedThisMinuteGraphPanel;

		DropDownButtonWidget statsDropDown;
		IEnumerable<Player> players;
		World world;

		TableWidget ecotable;
		TableWidget controltable;
		TableWidget combattable;

		ObserverStatsWidget mainWidget;
		SpacingWidget bottomBar;
		SpacingWidget bottomRowTemplate;
		BackgroundWidget bottomRowBackground;

		ObserverStatsHotkeyMgr hotkeyManager;

		[ObjectCreator.UseCtor]
		public ObserverStatsLogic(World world, WorldRenderer worldRenderer, Widget widget)
		{
			this.world = world;
			players = world.Players.Where(p => !p.NonCombatant);

			mainWidget = (ObserverStatsWidget)widget;

			productionPlayerTemplate = widget.Get<SpacingWidget>("PRODUCTION_PLAYER_TEMPLATE");
			productionPanel = widget.Get<SpacingWidget>("PRODUCTION_PANEL");

			supportPowerPlayerTemplate = widget.Get<SpacingWidget>("SUPPORT_POWER_PLAYER_TEMPLATE");
			supportPowerPanel = widget.Get<SpacingWidget>("SUPPORTPWR_PANEL");

			ecotable = widget.Get<TableWidget>("ECOTABLE");
			controltable = widget.Get<TableWidget>("CONTROLTABLE");
			combattable = widget.Get<TableWidget>("COMBATTABLE");

			earnedThisMinuteGraphPanel = widget.Get<BackgroundWidget>("EARNED_THIS_MIN_GRAPH_PANEL");

			if (ecotable.Title == "")
			{
				ecotable.Title = "Economy";
			}

			if (controltable.Title == "")
			{
				controltable.Title = "Control";
			}

			if (combattable.Title == "")
			{
				combattable.Title = "Combat";
			}

			bottomBar = widget.Get<SpacingWidget>("OBSERVER_STATS_BOTTOM_PANEL");
			bottomRowTemplate = bottomBar.Get<SpacingWidget>("OBSSTATSBOTTOM_TEMPLATE");
			bottomRowBackground = bottomRowTemplate.Get<BackgroundWidget>("OBSSTATSBOTTOM_TEMPLATE_CONTAINER");

			hotkeyManager = new ObserverStatsHotkeyMgr();
			mainWidget.AddChild(hotkeyManager);
			hotkeyManager.DropdownOptions = GetDropDownOptions();

			statsDropDown = widget.Get<DropDownButtonWidget>("STATS_DROPDOWN");
			statsDropDown.OnMouseDown = _ =>
			{
				statsDropDown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 315, hotkeyManager.DropdownOptions, SetupDropDownItem);
			};

			productionPanel.Children.Clear();
			supportPowerPanel.Children.Clear();

			productionPanel.Layout = new ListLayout(productionPanel);
			supportPowerPanel.Layout = new ListLayout(supportPowerPanel);
			bottomBar.Layout = new ListLayout(this.bottomBar);

			// calculate width op row based on X and width's of individual widgets on the bar
			int sumofwidth = 0;
			foreach (var w in this.bottomRowBackground.Children)
			{
				sumofwidth += w.Bounds.X - sumofwidth + w.Bounds.Width;
			}

			bottomRowBackground.Bounds.Width = sumofwidth;
			bottomRowTemplate.Bounds.Width = bottomRowBackground.Bounds.Width;
			bottomBar.Bounds.Width = bottomRowTemplate.Bounds.Width;

			HideTables();
			InitTables();

			// initial selection
			statsDropDown.GetText = () => mainWidget.DefaultSelectedOption;
			var option = hotkeyManager.DropdownOptions.Find(o => o.Title == statsDropDown.GetText());
			if (option != null) {
				option.OnClick();
			}
		}

		static Color GetPowerColor(PowerState state)
		{
			if (state == PowerState.Critical) return Color.Red;
			if (state == PowerState.Low) return Color.Orange;
			return Color.LimeGreen;
		}

		ScrollItemWidget SetupDropDownItem(ObserverStatsDropDownOption option, ScrollItemWidget template)
		{
			var item = ScrollItemWidget.Setup(template, option.IsSelected, option.OnClick);
			item.Get<LabelWidget>("LABEL").GetText = () => option.Title;
			return item;
		}

		void InitEarnedThisMinuteGraph()
		{
			var graphcontainer = earnedThisMinuteGraphPanel.Get<BackgroundWidget>("EARNED_THIS_MIN_GRAPH_CONTAINER");
			var graph = graphcontainer.Get<LineGraphWidget>("EARNED_THIS_MIN_GRAPH");
			graph.GetSeries = () =>
				players.Select(p => new LineGraphSeries(
					p.PlayerName,
					p.Color.RGB,
					(p.PlayerActor.TraitOrDefault<PlayerStatistics>() ?? new PlayerStatistics(p.PlayerActor)).EarnedSamples.Select(s => (float)s)
					));
		}

		List<ObserverStatsDropDownOption> GetDropDownOptions()
		{
			return new List<ObserverStatsDropDownOption>
				{
					new ObserverStatsDropDownOption
					{
						Title = "",
						IsSelected = () => false,
						Key = Game.Settings.Keys.ObserverStatsNone,
						OnClick = () =>
						{
							HideTables();
							statsDropDown.GetText = () => "";
						}
					},
					new ObserverStatsDropDownOption
					{
						Title = controltable.Title,
						IsSelected = () => controltable.Visible,
						Key = Game.Settings.Keys.ObserverStatsControl,
						OnClick = () =>
						{
							HideTables();
							statsDropDown.GetText = () => controltable.Title;
							controltable.Visible = true;
						}
					},
					new ObserverStatsDropDownOption
					{
						Title = combattable.Title,
						IsSelected = () => combattable.Visible,
						Key = Game.Settings.Keys.ObserverStatsCombat,
						OnClick = () => 
						{
							HideTables();
							statsDropDown.GetText = () => combattable.Title;
							combattable.Visible = true;
						}
					},
					new ObserverStatsDropDownOption
					{
						Title = ecotable.Title,
						IsSelected = () => ecotable.Visible,
						Key = Game.Settings.Keys.ObserverStatsEconomy,
						OnClick = () => 
						{
							HideTables();
							statsDropDown.GetText = () => ecotable.Title;
							ecotable.Visible = true;
						}
					},
					new ObserverStatsDropDownOption
					{
						Title = "Production",
						IsSelected = () => productionPanel.Visible,
						Key = Game.Settings.Keys.ObserverStatsProduction,
						OnClick = () => 
						{
							HideTables();
							statsDropDown.GetText = () => "Production";
							productionPanel.Visible = true;
						}
					},
					new ObserverStatsDropDownOption
					{
						Title = "Support Powers",
						IsSelected = () => false,
						Key = Game.Settings.Keys.ObserverStatsSupportPowers,
						OnClick = () => 
						{
							HideTables();
							statsDropDown.GetText = () => "Support Power";
							supportPowerPanel.Visible = true;
						}
					},
					new ObserverStatsDropDownOption
					{
						Title = "Earnings Graph",
						IsSelected = () => false,
						Key = Game.Settings.Keys.ObserverStatsGraph,
						OnClick = () =>
						{
							HideTables();
							earnedThisMinuteGraphPanel.Visible = true;
							statsDropDown.GetText = () => "Earnings Graph";
						}
					},
					new ObserverStatsDropDownOption
					{
						Title = "Summary",
						IsSelected = () => false,
						Key = Game.Settings.Keys.ObserverStatsSummary,
						OnClick = () => 
						{
							HideTables();
							statsDropDown.GetText = () => "Summary";
							bottomBar.Visible = true;
						}
					}
				};
		}

		void InitTables()
		{
			ecotable.InitTable();
			combattable.InitTable();
			controltable.InitTable();
			bottomBar.Children.Clear();

			InitEarnedThisMinuteGraph();

			int startY = bottomBar.Bounds.Y + bottomRowTemplate.Bounds.Y;
			int offset = startY;
			RectangleWidget teamEncaps = null;

			var teams = players.GroupBy(p => (world.LobbyInfo.ClientWithIndex(p.ClientIndex) ?? new Session.Client()).Team).OrderBy(g => g.Key);
			foreach (var t in teams)
			{
				var team = t;

				teamEncaps = new RectangleWidget();
				teamEncaps.IsVisible = () => bottomBar.Visible;

				if ((int)t.Key % 2 == 1)
				{
					// odd color
					teamEncaps.GetColor = () => mainWidget.BarsTeamEmphasizeColor1;
				}
				else
				{
					// even color
					teamEncaps.GetColor = () => mainWidget.BarsTeamEmphasizeColor2;
				}

				teamEncaps.Thickness = () => mainWidget.BarsTeamEmphasizeThickness;

				teamEncaps.Bounds.X = bottomBar.Bounds.X - 1;
				teamEncaps.Bounds.Y = startY;
				teamEncaps.Bounds.Width = bottomRowBackground.Bounds.Width + 1;

				foreach (var p in team)
				{
					var player = p;

					EconomyStats(player);
					CombatStats(player);
					ControlStats(player);
					ProductionStats(player);
					SupportPowerStats(player);
					ObserverStatsBottomRow(player);

					if (team.Key == 0)
					{
						// no team -> separate bars a little more
						bottomBar.ContentHeight += mainWidget.BarsNoTeamExtraSpacing;
						bottomRowTemplate.Bounds.Y += mainWidget.BarsNoTeamExtraSpacing;
					}
				}

				bottomBar.ContentHeight += mainWidget.BarsTeamsSpacing;
				bottomRowTemplate.Bounds.Y += mainWidget.BarsTeamsSpacing;

				if (teamEncaps != null) {
					teamEncaps.Bounds.Height = bottomBar.ContentHeight - mainWidget.BarsTeamsSpacing - 1 - (startY - offset);
					startY += teamEncaps.Bounds.Height + mainWidget.BarsTeamsSpacing;

					if (team.Key > 0) {
						// team.key 0 is not a team
						mainWidget.AddChild(teamEncaps);
					}
				}
			}

			bottomBar.Bounds.Height = bottomBar.ContentHeight;

			// if you set Spacing@OBSERVER_STATS_BOTTOM_PANEL:   Y to WINDOW_BOTTOM, it will go to the bottom and scale upwards instead of down
			if (bottomBar.Y == "WINDOW_BOTTOM")
			{
				bottomBar.Bounds.Y = Game.Renderer.Resolution.Height - bottomBar.ContentHeight - 10;
			}
		}

		void HideTables()
		{
			ecotable.Visible = false;
			combattable.Visible = false;
			controltable.Visible = false;

			productionPanel.Visible = false;
			supportPowerPanel.Visible = false;

			bottomBar.Visible = false;

			earnedThisMinuteGraphPanel.Visible = false;
		}

		void AssignGameStats(Widget widget, Player player)
		{
			LobbyUtils.AddPlayerFlagAndName(widget, player);

			var stats = player.PlayerActor.TraitOrDefault<PlayerStatistics>();
			var res = player.PlayerActor.Trait<PlayerResources>();
			var powerManager = player.PlayerActor.Trait<PowerManager>();

			LabelWidget lbl;

			if (stats != null)
			{
				lbl = widget.GetOrNull<LabelWidget>("CONTROL");
				if (lbl != null) lbl.GetText = () => MapControl(stats.MapControl);

				lbl = widget.GetOrNull<LabelWidget>("KILLS_COST");
				if (lbl != null) lbl.GetText = () => "$" + stats.KillsCost;

				lbl = widget.GetOrNull<LabelWidget>("DEATHS_COST");
				if (lbl != null) lbl.GetText = () => "$" + stats.DeathsCost;

				lbl = widget.GetOrNull<LabelWidget>("KILLS_COST_NOSIGN");
				if (lbl != null) lbl.GetText = () => "" + stats.KillsCost;

				lbl = widget.GetOrNull<LabelWidget>("DEATHS_COST_NOSIGN");
				if (lbl != null) lbl.GetText = () => "" + stats.DeathsCost;

				lbl = widget.GetOrNull<LabelWidget>("UNITS_KILLED");
				if (lbl != null) lbl.GetText = () => stats.UnitsKilled.ToString();

				lbl = widget.GetOrNull<LabelWidget>("UNITS_DEAD");
				if (lbl != null) lbl.GetText = () => stats.UnitsDead.ToString();

				lbl = widget.GetOrNull<LabelWidget>("BUILDINGS_KILLED");
				if (lbl != null) lbl.GetText = () => stats.BuildingsKilled.ToString();

				lbl = widget.GetOrNull<LabelWidget>("BUILDINGS_DEAD");
				if (lbl != null) lbl.GetText = () => stats.BuildingsDead.ToString();

				lbl = widget.GetOrNull<LabelWidget>("ACTIONS_MIN");
				if (lbl != null) lbl.GetText = () => AverageOrdersPerMinute(stats.OrderCount);

				lbl = widget.GetOrNull<LabelWidget>("ACTIONS_MIN_TXT");
				if (lbl != null) lbl.GetText = () => AverageOrdersPerMinute(stats.OrderCount) + " APM";
			}

			if (res != null)
			{
				lbl = widget.GetOrNull<LabelWidget>("CASH");
				if (lbl != null) lbl.GetText = () => "" + (res.DisplayCash + res.DisplayResources);

				lbl = widget.GetOrNull<LabelWidget>("INCOME");
				if (lbl != null) lbl.GetText = () => "" + res.Earned;

				lbl = widget.GetOrNull<LabelWidget>("SPENT");
				if (lbl != null) lbl.GetText = () => "" + res.Spent;

				lbl = widget.GetOrNull<LabelWidget>("EARNED_MIN");
				if (lbl != null) lbl.GetText = () => AverageEarnedPerMinute(res.Earned);
			}

			if (powerManager != null)
			{
				var energy = widget.GetOrNull<LabelWidget>("ENERGY");
				if (energy != null)
				{
					energy.GetText = () => powerManager.PowerDrained + "/" + powerManager.PowerProvided;
					energy.GetColor = () => GetPowerColor(powerManager.PowerState);
				}
			}
	
			lbl = widget.GetOrNull<LabelWidget>("HARVESTERS");
			if (lbl != null) lbl.GetText = () => world.Actors.Count(a => a.Owner == player && !a.IsDead() && a.HasTrait<Harvester>()).ToString();

			lbl = widget.GetOrNull<LabelWidget>("ARMYVALUE");
			if (lbl != null) lbl.GetText = () => world.Actors.Count(a => a.Owner == player && !a.IsDead() && a.HasTrait<AttackBase>() && !a.HasTrait<Building>()).ToString();

			lbl = widget.GetOrNull<LabelWidget>("STATICDEFVALUE");
			if (lbl != null) lbl.GetText = () => world.Actors.Count(a => a.Owner == player && !a.IsDead() && a.HasTrait<AttackBase>() && a.HasTrait<Building>()).ToString();

			lbl = widget.GetOrNull<LabelWidget>("BUILDINGVALUE");
			if (lbl != null) lbl.GetText = () => world.Actors.Count(a => a.Owner == player && !a.IsDead() && a.HasTrait<Building>()).ToString();
		}

		void CombatStats(Player player)
		{
			var row = combattable.NewRow();
			row.SetTextColor(player.Color.RGB);

			AssignGameStats(row, player);
		}

		void EconomyStats(Player player)
		{
			var row = ecotable.NewRow();
			row.SetTextColor(player.Color.RGB);

			AssignGameStats(row, player);
		}

		void ControlStats(Player player)
		{
			var row = controltable.NewRow();
			row.SetTextColor(player.Color.RGB);

			AssignGameStats(row, player);
		}

		void ProductionStats(Player player)
		{
			var template = new SpacingWidget(productionPlayerTemplate);
			productionPanel.AddChild(template);

			LobbyUtils.AddPlayerFlagAndName(template, player);

			var prodicons = template.Get<ObserverProductionIconsWidget>("PRODUCTION_ICONS");
			prodicons.GetPlayer = () => player;
			prodicons.IconScale = 1.0f;
			prodicons.IconSpacing = 0;
			prodicons.ShowTimeLeft = false;
		}

		void SupportPowerStats(Player player)
		{
			var template = new SpacingWidget(supportPowerPlayerTemplate);
			supportPowerPanel.AddChild(template);

			LobbyUtils.AddPlayerFlagAndName(template, player);

			var powericons = template.Get<ObserverSupportPowerIconsWidget>("SUPPORT_POWER_ICONS");
			powericons.GetPlayer = () => player;
			powericons.IconScale = 1.0f;
			powericons.IconSpacing = 0;
		}

		void ObserverStatsBottomRow(Player player)
		{
			var template = new SpacingWidget(this.bottomRowTemplate);
			bottomBar.AddChild(template);

			var panel = template.Get<BackgroundWidget>("OBSSTATSBOTTOM_TEMPLATE_CONTAINER");

			Color playerColor = player.Color.RGB;
			panel.MixColorFadeToBlackColor = playerColor;
			panel.MixColorFadeToBlack = true;

			AssignGameStats(template, player);

			// utils.AddPlayerFlagAndName() sets playername color to player's color, but that clashes with our gradient here
			var lbl = panel.GetOrNull<LabelWidget>("PLAYER");
			lbl.GetColor = () => Color.White;
		}

		string MapControl(double control)
		{
			return Math.Round(control * 100) + "%";
		}

		string AverageOrdersPerMinute(double orders)
		{
			return "" + Math.Round(world.WorldTick == 0 ? 0 : orders / (world.WorldTick / 1500.0));
		}

		string AverageEarnedPerMinute(double earned)
		{
			return "" + Math.Round(world.WorldTick == 0 ? 0 : earned / (world.WorldTick / 1500.0));
		}
	}
}
