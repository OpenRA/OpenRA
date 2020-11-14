#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class StrategicProgressWidget : Widget
	{
		readonly World world;
		bool initialised = false;

		[ObjectCreator.UseCtor]
		public StrategicProgressWidget(World world)
		{
			IsVisible = () => true;
			this.world = world;
		}

		public override void Draw()
		{
			if (!initialised)
				Init();

			if (!IsVisible()) return;

			var rb = RenderBounds;
			var offset = int2.Zero;

			var svc = world.Players.Select(p => p.PlayerActor.TraitOrDefault<StrategicVictoryConditions>()).FirstOrDefault();

			var totalWidth = svc.Total * 32;
			var curX = -totalWidth / 2;

			foreach (var a in svc.AllPoints)
			{
				WidgetUtils.DrawRGBA(ChromeProvider.GetImage("strategic", "critical_unowned"), offset + new float2(rb.Left + curX, rb.Top));

				if (world.LocalPlayer != null && a.Owner.RelationshipWith(world.LocalPlayer) == PlayerRelationship.Ally)
					WidgetUtils.DrawRGBA(ChromeProvider.GetImage("strategic", "player_owned"), offset + new float2(rb.Left + curX, rb.Top));
				else if (!a.Owner.NonCombatant)
					WidgetUtils.DrawRGBA(ChromeProvider.GetImage("strategic", "enemy_owned"), offset + new float2(rb.Left + curX, rb.Top));

				curX += 32;
			}

			offset += new int2(0, 32);

			if (world.LocalPlayer == null) return;
			var pendingWinner = FindFirstWinningPlayer(world);
			if (pendingWinner == null) return;
			var winnerSvc = pendingWinner.PlayerActor.Trait<StrategicVictoryConditions>();

			var isVictory = pendingWinner.RelationshipWith(world.LocalPlayer) == PlayerRelationship.Ally;
			var tc = "Strategic {0} in {1}".F(
				isVictory ? "victory" : "defeat",
				WidgetUtils.FormatTime(winnerSvc.TicksLeft, world.Timestep));

			var font = Game.Renderer.Fonts["Bold"];

			var size = font.Measure(tc);
			font.DrawTextWithContrast(tc, offset + new float2(rb.Left - size.X / 2 + 1, rb.Top + 1), Color.White, Color.Black, 1);
			offset += new int2(0, size.Y + 1);
		}

		public Player FindFirstWinningPlayer(World world)
		{
			// loop through all players, see who is 'winning' and get the one with the shortest 'time to win'
			var shortest = int.MaxValue;
			Player shortestPlayer = null;

			foreach (var p in world.Players.Where(p => !p.NonCombatant))
			{
				var svc = p.PlayerActor.Trait<StrategicVictoryConditions>();

				if (svc.Holding && svc.TicksLeft > 0 && svc.TicksLeft < shortest)
				{
					shortest = svc.TicksLeft;
					shortestPlayer = p;
				}
			}

			return shortestPlayer;
		}

		void Init()
		{
			var visible = world.ActorsHavingTrait<StrategicVictoryConditions>().Any() &&
				world.ActorsHavingTrait<StrategicPoint>().Any();

			IsVisible = () => visible;
			initialised = true;
		}
	}
}
