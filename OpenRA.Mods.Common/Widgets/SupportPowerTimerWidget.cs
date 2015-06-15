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
		public readonly string Font = "Bold";
		[Translate] public readonly string NoTeamLabel = "No Team";
		[Translate] public readonly string TeamLabel = "Team {0}";

		readonly int updatesPerSecond = 2;	// Range: 1 - 26	// 26 means no delay
											// Values are in game time.
											// Higher is more responsive
											// but also more computationally intensive.
		readonly World world;
		readonly Dictionary<Player, SupportPowerManager> init;
		struct Timer
		{
			public string PowerName;
			public string RemainingTime;
			public Color TimerColor;
		}

		struct TextBlock
		{
			public string Label;	// Used only if the text block is a team label.
			public int Team;
			public string PlayerName;
			public Color PlayerColor;
			public List<Timer> Timers;
		}

		List<TextBlock> temp, texts;
		int yIncrement, padding, leftColumnWidth, middleColumnWidth, lastTick, waitTicks;
		Player cachedPlayer;
		SpriteFont font;

		[ObjectCreator.UseCtor]
		public SupportPowerTimerWidget(World world)
		{
			init = new Dictionary<Player, SupportPowerManager>();
			foreach (var tp in world.ActorsWithTrait<SupportPowerManager>()
				.Where(p => !p.Actor.IsDead && !p.Actor.Owner.NonCombatant))
				init[tp.Actor.Owner] = tp.Trait;

			texts = new List<TextBlock>();
			font = Game.Renderer.Fonts[Font];
			yIncrement = font.Measure(" ").Y + 5;
			padding = font.Measure("   ").X;
			this.world = world;
			waitTicks = 25 / updatesPerSecond;
		}

		bool AddTextBlock(Player p, int team, TextBlock? label, List<TextBlock> list)
		{
			var spiArray = init[p].Powers.Values.Where(i => i.Instances.Any() && i.Info.DisplayTimer && !i.Disabled).ToArray();
			if (spiArray.Length == 0)
				return false;

			if (label != null)
				list.Add((TextBlock)label);

			var tb = new TextBlock();
			tb.Timers = new List<Timer>();
			foreach (var spi in spiArray)
			{
				var t = new Timer();
				t.PowerName = spi.Info.Description;
				t.RemainingTime = WidgetUtils.FormatTime(spi.RemainingTime, false);
				t.TimerColor = spi.Ready && Game.LocalTick % 50 > 25 ? Color.White : p.Color.RGB;
				tb.Timers.Add(t);
			}

			tb.Team = team;
			tb.PlayerName = p.PlayerName;
			tb.PlayerColor = p.Color.RGB;
			list.Add(tb);
			return true;
		}

		void OrderTexts(bool hasSelf, int selfTeam)
		{
			var grouping = temp.GroupBy(t => t.Team).OrderBy(g => g.Key);
			foreach (var team in grouping)
			{
				if (!hasSelf || team.Key != -selfTeam)
				{
					var tb = new TextBlock();
					tb.Label = team.Key == 0 ? NoTeamLabel : TeamLabel.F(team.Key < 0 ? -team.Key : team.Key);
					texts.Add(tb);
				}

				foreach (var tb in team)
					texts.Add(tb);
			}
		}

		void UpdateColumnWidth()
		{
			if (texts.Count == 0)
				return;

			leftColumnWidth = texts.Max(tb => font.Measure(tb.PlayerName ?? tb.Label).X);
			middleColumnWidth = texts.Max(tb => tb.Timers == null ? 0 : tb.Timers.Max(t => font.Measure(t.PowerName).X));
		}

		void GenerateTexts(Player player, bool showSelf)
		{
			temp = new List<TextBlock>();
			texts = new List<TextBlock>();
			var hasSelf = false;
			var selfTeam = world.LobbyInfo.ClientWithIndex(player.ClientIndex).Team;
			foreach (var p in init.Keys)
			{
				if (p != player)
				{
					var pTeam = world.LobbyInfo.ClientWithIndex(p.ClientIndex).Team;
					if (pTeam == selfTeam)
						pTeam = -pTeam;

					AddTextBlock(p, pTeam, null, temp);
				}
				else if (showSelf)
				{
					var tb = new TextBlock();
					tb.Label = selfTeam == 0 ? NoTeamLabel : TeamLabel.F(selfTeam);
					hasSelf = AddTextBlock(p, selfTeam, tb, texts);
				}
			}

			OrderTexts(hasSelf, selfTeam);
			UpdateColumnWidth();
		}

		void MainLogic()
		{
			if (world.LocalPlayer != null && world.LocalPlayer.WinState == WinState.Undefined)
			{
				// Viewer is a player.
				if (cachedPlayer != world.LocalPlayer || world.WorldTick - lastTick >= waitTicks)
				{
					GenerateTexts(world.LocalPlayer, false);
					cachedPlayer = world.LocalPlayer;
					lastTick = world.WorldTick;
				}

				return;
			}

			if (world.RenderPlayer != null && world.RenderPlayer.InternalName != "Everyone")
			{
				// Viewer is an observer who selected a player's view.
				if (cachedPlayer != world.RenderPlayer || world.WorldTick - lastTick >= waitTicks)
				{
					GenerateTexts(world.RenderPlayer, true);
					cachedPlayer = world.RenderPlayer;
					lastTick = world.WorldTick;
				}

				return;
			}

			// Viewer is an observer who selected "Disable Shroud" or "All Players" view.
			if ((cachedPlayer == null || cachedPlayer.InternalName == "Everyone") && world.WorldTick - lastTick < waitTicks)
				return;

			temp = new List<TextBlock>();
			texts = new List<TextBlock>();
			foreach (var p in init.Keys)
				AddTextBlock(p, world.LobbyInfo.ClientWithIndex(p.ClientIndex).Team, null, temp);

			OrderTexts(false, 0);
			UpdateColumnWidth();
			cachedPlayer = world.RenderPlayer;
			lastTick = world.WorldTick;
		}

		public override void Draw()
		{
			if (!IsVisible())
				return;

			MainLogic();

			if (texts.Count == 0)
				return;

			var b = new float2(Bounds.Location);
			var pos = new float2(0, 0);

			foreach (var tb in texts)
			{
				if (tb.Label != null)
				{
					pos.X = 0;
					pos.Y += 6;
					font.DrawTextWithContrast(tb.Label, b + pos, Color.White, Color.Black, 1);
					pos.Y += yIncrement + 3;
					continue;
				}

				pos.X = leftColumnWidth - font.Measure(tb.PlayerName).X;
				font.DrawTextWithContrast(tb.PlayerName, b + pos, tb.PlayerColor, Color.Black, 1);

				foreach (var timer in tb.Timers)
				{
					pos.X = leftColumnWidth + padding;
					font.DrawTextWithContrast(timer.PowerName, b + pos, timer.TimerColor, Color.Black, 1);

					pos.X += middleColumnWidth + padding;
					font.DrawTextWithContrast(timer.RemainingTime, b + pos, timer.TimerColor, Color.Black, 1);

					pos.Y += yIncrement;
				}

				pos.Y += 3;
			}
		}
	}
}
