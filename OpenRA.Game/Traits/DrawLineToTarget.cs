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
using System.Collections.Generic;

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
		List<Target> targets;
		Color c;
		int lifetime;

		public DrawLineToTarget(Actor self, DrawLineToTargetInfo info) { this.self = self; this.Info = info; }

		public void SetTarget(Actor self, Target target, Color c, bool display)
		{
			this.targets = new List<Target> { target };
			this.c = c;

			if (display)
				lifetime = Info.Ticks;
		}

		public void SetTargets(Actor self, List<Target> targets, Color c, bool display)
		{
			this.targets = targets;
			this.c = c;

			if (display)
				lifetime = Info.Ticks;
		}

		public void RenderAfterWorld(WorldRenderer wr)
		{
			var force = Game.GetModifierKeys().HasModifier(Modifiers.Alt);
			if ((lifetime <= 0 || --lifetime <= 0) && !force)
				return;

			if (targets == null || targets.Count == 0)
				return;

			var from = wr.ScreenPxPosition(self.CenterPosition);
			var wlr = Game.Renderer.WorldLineRenderer;

			foreach (var target in targets)
			{
				if (target.Type == TargetType.Invalid)
					continue;

				var to = wr.ScreenPxPosition(target.CenterPosition);
				wlr.DrawLine(from, to, c, c);
				DrawTargetMarker(wlr, from);
				DrawTargetMarker(wlr, to);
			}
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
		public static void SetTargetLines(this Actor self, List<Target> targets, Color color)
		{
			var line = self.TraitOrDefault<DrawLineToTarget>();
			if (line != null)
			{
				self.World.AddFrameEndTask(w =>
				{
					line.SetTargets(self, targets, color, false);
				});
			}
		}

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
				if (self.Destroyed)
					return;

				if (target.Type == TargetType.Actor && display)
					w.Add(new FlashTarget(target.Actor));

				var line = self.TraitOrDefault<DrawLineToTarget>();
				if (line != null)
					line.SetTarget(self, target, color, display);
			});
		}

		public static void SetTargetLine(this Actor self, FrozenActor target, Color color, bool display)
		{
			if (self.Owner != self.World.LocalPlayer)
				return;

			self.World.AddFrameEndTask(w =>
			{
				if (self.Destroyed)
					return;

				target.Flash();

				var line = self.TraitOrDefault<DrawLineToTarget>();
				if (line != null)
					line.SetTarget(self, Target.FromPos(target.CenterPosition), color, display);
			});
		}
	}
}

