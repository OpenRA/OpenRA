using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using OpenRA.Graphics;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets
{
	public class StrategicProgressWidget : Widget
	{
		bool Initialised = false;
		readonly World world;
		readonly WorldRenderer worldRenderer;
		[ObjectCreator.UseCtor]
		public StrategicProgressWidget([ObjectCreator.Param] World world, [ObjectCreator.Param] WorldRenderer worldRenderer)
		{ 
			IsVisible = () => true;
			this.world = world;
			this.worldRenderer = worldRenderer;
		}

		public override void DrawInner()
		{
			if (!Initialised)
				Init();
			
			if (!IsVisible()) return;
			int2 offset = int2.Zero;

			var svc = world.players.Select(p => p.Value.PlayerActor.TraitOrDefault<StrategicVictoryConditions>()).FirstOrDefault();

			var totalWidth = (svc.Total + svc.TotalCritical)*32;
			int curX = -(totalWidth / 2);

			foreach (var a in world.Actors.Where(a => !a.Destroyed && a.HasTrait<StrategicPoint>() && !a.TraitOrDefault<StrategicPoint>().Critical))
			{
				WidgetUtils.DrawRGBA(ChromeProvider.GetImage("strategic", "unowned"), offset + new float2(RenderBounds.Left + curX, RenderBounds.Top));
				
				if (a.Owner == worldRenderer.world.LocalPlayer || (a.Owner.Stances[world.LocalPlayer] == Stance.Ally && world.LocalPlayer.Stances[a.Owner] == Stance.Ally)) 
					WidgetUtils.DrawRGBA(ChromeProvider.GetImage("strategic", "player_owned"), offset + new float2(RenderBounds.Left + curX, RenderBounds.Top));
				else if (!a.Owner.NonCombatant)
					WidgetUtils.DrawRGBA(ChromeProvider.GetImage("strategic", "enemy_owned"), offset + new float2(RenderBounds.Left + curX, RenderBounds.Top));
				curX += 32;
			}

			foreach (var a in world.Actors.Where(a => !a.Destroyed && a.HasTrait<StrategicPoint>() && a.TraitOrDefault<StrategicPoint>().Critical))
			{
				WidgetUtils.DrawRGBA(ChromeProvider.GetImage("strategic", "critical_unowned"), offset + new float2(RenderBounds.Left + curX, RenderBounds.Top));
				
				if (a.Owner == world.LocalPlayer || (a.Owner.Stances[world.LocalPlayer] == Stance.Ally && world.LocalPlayer.Stances[a.Owner] == Stance.Ally))
					WidgetUtils.DrawRGBA(ChromeProvider.GetImage("strategic", "player_owned"), offset + new float2(RenderBounds.Left + curX, RenderBounds.Top));
				else if (!a.Owner.NonCombatant)
					WidgetUtils.DrawRGBA(ChromeProvider.GetImage("strategic", "enemy_owned"), offset + new float2(RenderBounds.Left + curX, RenderBounds.Top));
				
				curX += 32;
			}
			offset += new int2(0, 32);
			
			var pendingWinner = FindFirstWinningPlayer(world);
			if (pendingWinner == null) return;
			svc = pendingWinner.PlayerActor.TraitOrDefault<StrategicVictoryConditions>();

			if (world.LocalPlayer != null)
			{
				var tc = "";

				if (pendingWinner != world.LocalPlayer && (pendingWinner.Stances[world.LocalPlayer] != Stance.Ally || world.LocalPlayer.Stances[pendingWinner] != Stance.Ally))
				{
					// losing
					tc = "Strategic defeat in " +
						 ((svc.CriticalTicksLeft > svc.TicksLeft) ? WidgetUtils.FormatTime(svc.CriticalTicksLeft) : WidgetUtils.FormatTime(svc.TicksLeft));
				}else
				{
					// winning
					tc = "Strategic victory in " +
						 ((svc.CriticalTicksLeft > svc.TicksLeft) ? WidgetUtils.FormatTime(svc.CriticalTicksLeft) : WidgetUtils.FormatTime(svc.TicksLeft));
				}

				var size = Game.Renderer.BoldFont.Measure(tc);

				Game.Renderer.BoldFont.DrawText(tc, offset + new float2(RenderBounds.Left - size.X / 2 + 1, RenderBounds.Top + 1), Color.Black);
				Game.Renderer.BoldFont.DrawText(tc, offset + new float2(RenderBounds.Left - size.X / 2, RenderBounds.Top), Color.WhiteSmoke);
				offset += new int2(0, size.Y + 1);
			}

		}

		public Player FindFirstWinningPlayer(World world)
		{
			// loop through all players, see who is 'winning' and get the one with the shortest 'time to win'
			int shortest = int.MaxValue;
			Player shortestPlayer = null;

			foreach (var p in world.players.Select(p => p.Value).Where(p => !p.NonCombatant))
			{
				var svc = p.PlayerActor.TraitOrDefault<StrategicVictoryConditions>();

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

		private void Init()
		{
			IsVisible = () => (world.Actors.Where(a => a.HasTrait<StrategicVictoryConditions>()).Any() && world.Actors.Where(a => a.HasTrait<StrategicPoint>()).Any());
			Initialised = true;
		}
	}
}
