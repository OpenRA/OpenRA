#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Effects;

namespace OpenRA.Traits
{
	public class DrawLineToTargetInfo : ITraitInfo
	{
		public readonly int Ticks = 60;

		public virtual object Create(ActorInitializer init) { return new DrawLineToTarget(this); }
	}

	public class DrawLineToTarget : IPostRenderSelection
	{
		DrawLineToTargetInfo Info;
		public DrawLineToTarget(DrawLineToTargetInfo info)
		{
			this.Info = info;
		}

		Target target;
		int lifetime;
		Color c;

		public void SetTarget(Actor self, Target target, Color c)
		{
			this.target = target;
			lifetime = Info.Ticks;
			this.c = c;
		}

		public void SetTargetSilently(Actor self, Target target, Color c)
		{
			this.target = target;
			this.c = c;
		}

		public void RenderAfterWorld(WorldRenderer wr, Actor self)
		{
			if (self.IsIdle) return;

			var force = Game.GetModifierKeys().HasModifier(Modifiers.Alt);
			if ((lifetime <= 0 || --lifetime <= 0) && !force)
				return;

			if (!target.IsValid)
				return;

			var p = target.CenterLocation;
			var move = self.TraitOrDefault<IMove>();
			var origin = move != null ? self.CenterLocation - new float2(0, move.Altitude) : self.CenterLocation;

			Game.Renderer.LineRenderer.DrawLine(origin, p, c, c);
			for (bool b = false; !b; p = origin, b = true)
			{
				Game.Renderer.LineRenderer.DrawLine(p + new float2(-1, -1), p + new float2(-1, 1), c, c);
				Game.Renderer.LineRenderer.DrawLine(p + new float2(-1, 1), p + new float2(1, 1), c, c);
				Game.Renderer.LineRenderer.DrawLine(p + new float2(1, 1), p + new float2(1, -1), c, c);
				Game.Renderer.LineRenderer.DrawLine(p + new float2(1, -1), p + new float2(-1, -1), c, c);
			}
		}
	}
	
	public static class LineTargetExts
	{
		public static void SetTargetLine(this Actor self, Target target, Color color)
		{
			self.SetTargetLine(target, color, true);
		}
		
		public static void SetTargetLine(this Actor self, Target target, Color color, bool display)
		{
			if (self.Owner != self.World.LocalPlayer)
				return;

			self.World.AddFrameEndTask(w =>
			{
				if (self.Destroyed) return;
				if (target.IsActor && display)
					w.Add(new FlashTarget(target.Actor));
				
				var line = self.TraitOrDefault<DrawLineToTarget>();
				if (line != null)
					if (display) 
						line.SetTarget(self, target, color);
					else
						line.SetTargetSilently(self, target, color);
			});
		}
	}
}

