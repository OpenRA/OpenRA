#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Activities;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Renders target lines between order waypoints.")]
	public class DrawLineToTargetInfo : ITraitInfo
	{
		[Desc("Delay (in ticks) before the target lines disappear.")]
		public readonly int Delay = 60;

		[Desc("Width (in pixels) of the target lines.")]
		public readonly int LineWidth = 2;

		[Desc("Width (in pixels) of the end node markers.")]
		public readonly int MarkerWidth = 3;

		public virtual object Create(ActorInitializer init) { return new DrawLineToTarget(init.Self, this); }
	}

	public class DrawLineToTarget : IRenderAboveShroudWhenSelected, INotifySelected
	{
		readonly DrawLineToTargetInfo info;
		int lifetime;

		public DrawLineToTarget(Actor self, DrawLineToTargetInfo info)
		{
			this.info = info;
		}

		public void ShowTargetLines(Actor a)
		{
			if (a.IsIdle)
				return;

			// Reset the order line timeout.
			lifetime = info.Delay;
		}

		void INotifySelected.Selected(Actor a)
		{
			ShowTargetLines(a);
		}

		IEnumerable<IRenderable> IRenderAboveShroudWhenSelected.RenderAboveShroud(Actor self, WorldRenderer wr)
		{
			if (self.Owner != self.World.LocalPlayer)
				yield break;

			// Players want to see the lines when in waypoint mode.
			var force = Game.GetModifierKeys().HasModifier(Modifiers.Shift);

			if (--lifetime <= 0 && !force)
				yield break;

			if (!(force || Game.Settings.Game.DrawTargetLine))
				yield break;

			var prev = self.CenterPosition;
			var a = self.CurrentActivity;
			for (; a != null; a = a.NextActivity)
			{
				if (a.IsCanceling)
					continue;

				foreach (var n in a.TargetLineNodes(self))
				{
					if (n.Target.Type != TargetType.Invalid)
					{
						yield return new TargetLineRenderable(new[] { prev, n.Target.CenterPosition }, n.Color, info.LineWidth, info.MarkerWidth);
						prev = n.Target.CenterPosition;
					}
				}
			}
		}

		bool IRenderAboveShroudWhenSelected.SpatiallyPartitionable { get { return false; } }
	}

	public static class LineTargetExts
	{
		public static void ShowTargetLines(this Actor self)
		{
			if (self.Owner != self.World.LocalPlayer)
				return;

			// Draw after frame end so that all the queueing of activities are done before drawing.
			var line = self.TraitOrDefault<DrawLineToTarget>();
			if (line != null)
				self.World.AddFrameEndTask(w => line.ShowTargetLines(self));
		}
	}
}
