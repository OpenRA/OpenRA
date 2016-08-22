#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class DrawLineToTargetInfo : ITraitInfo
	{
		public readonly int Delay = 60;

		public virtual object Create(ActorInitializer init) { return new DrawLineToTarget(init.Self, this); }
	}

	public class DrawLineToTarget : IRenderAboveShroudWhenSelected, INotifySelected, INotifyBecomingIdle
	{
		readonly DrawLineToTargetInfo info;
		List<Target> targets;
		Color c;
		int lifetime;

		public DrawLineToTarget(Actor self, DrawLineToTargetInfo info)
		{
			this.info = info;
		}

		public void SetTarget(Actor self, Target target, Color c, bool display)
		{
			targets = new List<Target> { target };
			this.c = c;

			if (display)
				lifetime = info.Delay;
		}

		public void SetTargets(Actor self, List<Target> targets, Color c, bool display)
		{
			this.targets = targets;
			this.c = c;

			if (display)
				lifetime = info.Delay;
		}

		void INotifySelected.Selected(Actor a)
		{
			if (a.IsIdle)
				return;

			// Reset the order line timeout.
			lifetime = info.Delay;
		}

		IEnumerable<IRenderable> IRenderAboveShroudWhenSelected.RenderAboveShroud(Actor self, WorldRenderer wr)
		{
			var force = Game.GetModifierKeys().HasModifier(Modifiers.Alt);
			if ((lifetime <= 0 || --lifetime <= 0) && !force)
				yield break;

			if (!(force || Game.Settings.Game.DrawTargetLine))
				yield break;

			if (targets == null || targets.Count == 0)
				yield break;

			foreach (var target in targets)
			{
				if (target.Type == TargetType.Invalid)
					continue;

				yield return new TargetLineRenderable(new[] { self.CenterPosition, target.CenterPosition }, c);
			}
		}

		void INotifyBecomingIdle.OnBecomingIdle(Actor a)
		{
			if (a.IsIdle)
				targets = null;
		}
	}

	public static class LineTargetExts
	{
		public static void SetTargetLines(this Actor self, List<Target> targets, Color color)
		{
			var line = self.TraitOrDefault<DrawLineToTarget>();
			if (line != null)
				self.World.AddFrameEndTask(w => line.SetTargets(self, targets, color, false));
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
				if (self.Disposed)
					return;

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
				if (self.Disposed)
					return;

				target.Flash();

				var line = self.TraitOrDefault<DrawLineToTarget>();
				if (line != null)
					line.SetTarget(self, Target.FromPos(target.CenterPosition), color, display);
			});
		}
	}
}