#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets
{
	public class StrategicProgressWidget : Widget
	{
		bool Initialised = false;
		readonly World world;

		[ObjectCreator.UseCtor]
		public StrategicProgressWidget([ObjectCreator.Param] World world)
		{ 
			IsVisible = () => true;
			this.world = world;
		}

		public override void Draw()
		{
			if (!Initialised)
				Init();

			if (!IsVisible()) return;
			int2 offset = int2.Zero;

			var svc = world.Players.Select(p => p.PlayerActor.TraitOrDefault<StrategicVictoryConditions>()).FirstOrDefault();

			var totalWidth = (svc.Total + svc.TotalCritical) * 32;
			int curX = -(totalWidth / 2);

			foreach (var a in world.ActorsWithTrait<StrategicPoint>().Where(a => a.Trait.Critical))
			{
				WidgetUtils.DrawRGBA(ChromeProvider.GetImage("strategic", "unowned"), offset + new float2(RenderBounds.Left + curX, RenderBounds.Top));

				if (WorldUtils.AreMutualAllies(a.Actor.Owner, world.LocalPlayer))
					WidgetUtils.DrawRGBA(ChromeProvider.GetImage("strategic", "player_owned"), offset + new float2(RenderBounds.Left + curX, RenderBounds.Top));
				else if (!a.Actor.Owner.NonCombatant)
					WidgetUtils.DrawRGBA(ChromeProvider.GetImage("strategic", "enemy_owned"), offset + new float2(RenderBounds.Left + curX, RenderBounds.Top));
				curX += 32;
			}

			foreach (var a in world.ActorsWithTrait<StrategicPoint>().Where(a => !a.Trait.Critical))
			{
				WidgetUtils.DrawRGBA(ChromeProvider.GetImage("strategic", "critical_unowned"), offset + new float2(RenderBounds.Left + curX, RenderBounds.Top));

				if (WorldUtils.AreMutualAllies(a.Actor.Owner, world.LocalPlayer))
					WidgetUtils.DrawRGBA(ChromeProvider.GetImage("strategic", "player_owned"), offset + new float2(RenderBounds.Left + curX, RenderBounds.Top));
				else if (!a.Actor.Owner.NonCombatant)
					WidgetUtils.DrawRGBA(ChromeProvider.GetImage("strategic", "enemy_owned"), offset + new float2(RenderBounds.Left + curX, RenderBounds.Top));

				curX += 32;
			}
			offset += new int2(0, 32);

			if (world.LocalPlayer == null) return;
			var pendingWinner = FindFirstWinningPlayer(world);
			if (pendingWinner == null) return;
			var winnerSvc = pendingWinner.PlayerActor.Trait<StrategicVictoryConditions>();

			var isVictory = pendingWinner == world.LocalPlayer || !WorldUtils.AreMutualAllies(pendingWinner, world.LocalPlayer);
			var tc = "Strategic {0} in {1}".F(
				isVictory ? "victory" : "defeat",
				WidgetUtils.FormatTime(Math.Max(winnerSvc.CriticalTicksLeft, winnerSvc.TicksLeft)));

			var size = Game.Renderer.Fonts["Bold"].Measure(tc);

			Game.Renderer.Fonts["Bold"].DrawText(tc, offset + new float2(RenderBounds.Left - size.X / 2 + 1, RenderBounds.Top + 1), Color.Black);
			Game.Renderer.Fonts["Bold"].DrawText(tc, offset + new float2(RenderBounds.Left - size.X / 2, RenderBounds.Top), Color.WhiteSmoke);
			offset += new int2(0, size.Y + 1);
		}

		public Player FindFirstWinningPlayer(World world)
		{
			// loop through all players, see who is 'winning' and get the one with the shortest 'time to win'
			int shortest = int.MaxValue;
			Player shortestPlayer = null;

			foreach (var p in world.Players.Where(p => !p.NonCombatant))
			{
				var svc = p.PlayerActor.Trait<StrategicVictoryConditions>();

				if (svc.HoldingCritical && svc.CriticalTicksLeft > 0 && svc.CriticalTicksLeft < shortest)
				{
					shortest = svc.CriticalTicksLeft;
					shortestPlayer = p;
				}

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
			var visible = world.ActorsWithTrait<StrategicVictoryConditions>().Any() &&
				world.ActorsWithTrait<StrategicPoint>().Any();

			IsVisible = () => visible;
			Initialised = true;
		}
	}
}
