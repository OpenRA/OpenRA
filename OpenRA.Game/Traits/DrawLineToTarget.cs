#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
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

		public virtual object Create(ActorInitializer init) { return new DrawLineToTarget(init.self, this); }
	}

	public class DrawLineToTarget : IPostRenderSelection
	{
		Actor self;
		DrawLineToTargetInfo Info;
		Target target;
		Color c;
		int lifetime;

		public DrawLineToTarget(Actor self, DrawLineToTargetInfo info) { this.self = self; this.Info = info; }

		public void SetTarget(Actor self, Target target, Color c, bool display)
		{
			this.target = target;
			this.c = c;

			if (display)
				lifetime = Info.Ticks;
		}

		public void RenderAfterWorld(WorldRenderer wr)
		{
			if (self.IsIdle) return;

			var force = Game.GetModifierKeys().HasModifier(Modifiers.Alt);
			if ((lifetime <= 0 || --lifetime <= 0) && !force)
				return;

			if (!target.IsValid)
				return;

			var move = self.TraitOrDefault<IMove>();
			var origin = move != null ? self.CenterLocation - new int2(0, move.Altitude) : self.CenterLocation;

			var wlr = Game.Renderer.WorldLineRenderer;

			wlr.DrawLine(origin, target.CenterLocation, c, c);
			DrawTargetMarker(wlr, target.CenterLocation);
			DrawTargetMarker(wlr, origin);
		}

		void DrawTargetMarker(LineRenderer wlr, float2 p)
		{
			wlr.DrawLine(p + new float2(-1, -1), p + new float2(-1, 1), c, c);
			wlr.DrawLine(p + new float2(-1, 1), p + new float2(1, 1), c, c);
			wlr.DrawLine(p + new float2(1, 1), p + new float2(1, -1), c, c);
			wlr.DrawLine(p + new float2(1, -1), p + new float2(-1, -1), c, c);
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
					line.SetTarget(self, target, color, display);
			});
		}
	}
}

