#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Activities;
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
		Color c;
		int lifetime;

		public DrawLineToTarget(Actor self, DrawLineToTargetInfo info)
		{
			this.info = info;
		}

		public void SetTarget(Actor self, Target target, Color c, bool display)
		{
			this.c = c;

			if (display)
				lifetime = info.Delay;
		}

		public void SetTargets(Actor self, Color c, bool display)
		{
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
				return new IRenderable[0];

			if (!(force || Game.Settings.Game.DrawTargetLine))
				return new IRenderable[0];

			if (self.GetCurrentActivity() == null)
				return new IRenderable[0];

			if (self.GetCurrentActivity().GetTargets(self).Count() <= 0)
				return new IRenderable[0];

			var validTargets = new List<WPos>();
			validTargets.Add(self.CenterPosition);

			foreach (var target in self.GetCurrentActivity().GetTargets(self))
			{
				validTargets.Add(target.CenterPosition);
			}

			return new[] { (IRenderable)new TargetLineRenderable(validTargets, c) };
		}

		void INotifyBecomingIdle.OnBecomingIdle(Actor a)
		{
		}
	}

	public static class LineTargetExts
	{
		public static void SetTargetLines(this Actor self, Color color)
		{
			var line = self.TraitOrDefault<DrawLineToTarget>();
			if (line != null)
				self.World.AddFrameEndTask(w => line.SetTargets(self, color, true));
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
