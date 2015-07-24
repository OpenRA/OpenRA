#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class SupportPowerTimerWidget : Widget
	{
		[Translate] public readonly string NoTeamText = "No Team";
		[Translate] public readonly string TeamText = "Team {0}";

		readonly Dictionary<Player, SupportPowerManager> spManagers;
		readonly SortedDictionary<int, Player[]> playersByTeam;

		readonly string fontName = "Bold";
		readonly SpriteFont font;
		readonly float yIncrement, powerPos, timePos;

		// Represents one line of text
		struct Text
		{
			public Color PlayerColor;
			public string Player;
			public string Power;
			public string RemainingTime;
			public bool Ready;
			public string Team;		// Used only for team label
		}

		readonly List<Text> display;
		readonly bool noPowers;
		bool flashing, needTeamLabel, showPlayer;
		Color timerColor;

		readonly World world;

		[ObjectCreator.UseCtor]
		public SupportPowerTimerWidget(World world)
		{
			spManagers = new Dictionary<Player, SupportPowerManager>();
			foreach (var tp in world.ActorsWithTrait<SupportPowerManager>()
				.Where(tp => !tp.Actor.IsDead && !tp.Actor.Owner.NonCombatant))
				spManagers[tp.Actor.Owner] = tp.Trait;

			var availablePowers = world.Map.Rules.Actors.Values
				.SelectMany(ai => ai.Traits.WithInterface<SupportPowerInfo>())
				.Where(i => i.DisplayTimer).ToArray();

			noPowers = !spManagers.Keys.Any() || !availablePowers.Any();
			if (noPowers)
				return;

			playersByTeam = new SortedDictionary<int, Player[]>();
			foreach (var team in spManagers.Keys.GroupBy(p => world.LobbyInfo.ClientWithIndex(p.ClientIndex).Team))
				playersByTeam[team.Key] = team.ToArray();

			font = Game.Renderer.Fonts[fontName];
			yIncrement = font.Measure(" ").Y + 5;
			var padding = font.Measure("  ").X;
			var maxPlayerWidth = spManagers.Keys.Max(p => font.Measure(p.PlayerName).X);
			var maxPowerWidth = availablePowers.Max(i => font.Measure(i.Description).X);
			powerPos = maxPlayerWidth + padding;
			timePos = powerPos + maxPowerWidth + padding;

			display = new List<Text>();
			this.world = world;
		}

		public override void Draw()
		{
			if (!IsVisible() || noPowers)
				return;

			// Generate texts once in every 10 WorldTicks,
			// which is 0.4 seconds in game time.
			if (world.WorldTick % 10 == 0)
			{
				display.Clear();
				foreach (var team in playersByTeam)
				{
					needTeamLabel = true;
					foreach (var player in team.Value)
					{
						if (!player.IsAlliedWith(world.RenderPlayer) || player == world.RenderPlayer)
							continue;

						var powers = spManagers[player].Powers.Values
							.Where(i => i.Instances.Any() && i.Info.DisplayTimer && !i.Disabled).ToArray();

						if (!powers.Any())
							continue;

						showPlayer = true;
						if (needTeamLabel)
						{
							var teamLabel = new Text();
							teamLabel.Team = team.Key == 0 ? NoTeamText : string.Format(TeamText, team.Key);
							display.Add(teamLabel);
							needTeamLabel = false;
						}

						foreach (var power in powers)
						{
							var timer = new Text();
							timer.PlayerColor = player.Color.RGB;
							timer.Ready = power.Ready;
							if (showPlayer)
							{
								timer.Player = player.PlayerName;
								showPlayer = false;
							}

							timer.Power = power.Info.Description;
							timer.RemainingTime = WidgetUtils.FormatTime(power.RemainingTime, false);
							display.Add(timer);
						}
					}
				}
			}

			if (!display.Any())
				return;

			flashing = Game.LocalTick % 50 < 25;
			var widgetBounds = new float2(Bounds.Location);
			var position = new float2(0, 0);
			foreach (var text in display)
			{
				if (text.Team != null)
				{
					font.DrawTextWithContrast(text.Team, widgetBounds + position, Color.White, Color.Black, 1);
					position.Y += yIncrement;
					continue;
				}

				if (text.Player != null)
					font.DrawTextWithContrast(text.Player, widgetBounds + position, text.PlayerColor, Color.Black, 1);

				timerColor = text.Ready && flashing ? Color.White : text.PlayerColor;
				position.X = powerPos;
				font.DrawTextWithContrast(text.Power, widgetBounds + position, timerColor, Color.Black, 1);
				position.X = timePos;
				font.DrawTextWithContrast(text.RemainingTime, widgetBounds + position, timerColor, Color.Black, 1);

				position.X = 0;
				position.Y += yIncrement;
			}
		}
	}
}
