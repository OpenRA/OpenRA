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

			// shift is "force" too, players want to see the lines when in waypoint mode.
			bool force = Game.GetModifierKeys().HasModifier(Modifiers.Alt)
			          || Game.GetModifierKeys().HasModifier(Modifiers.Shift);

			if ((lifetime <= 0 || --lifetime <= 0) && !force)
				yield break;

			if (!(force || Game.Settings.Game.DrawTargetLine))
				yield break;

			WPos prev = self.CenterPosition;
			for (var a = self.CurrentActivity; a != null; a = a.NextActivity)
			{
				if (a is OpenRA.Mods.Common.Activities.Move.MovePart)
					a = ((OpenRA.Mods.Common.Activities.Move.MovePart)a).Move;

				foreach (var target in a.GetTargets(self))
				{
					if (a.IsCanceled || target.Type == TargetType.Invalid)
						continue;

					yield return new TargetLineRenderable(new[] { prev, target.CenterPosition }, a.TargetLineColor);
					prev = target.CenterPosition;
				}

				if (a is ResupplyAircraft)
					break; /// If we don't break, we get infinite loop.
				if (a is EnterTransport)
					break;
				if (a is HarvestResource)
					break;
			}
		}
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