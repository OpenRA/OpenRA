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
			lifetime = 10; ////How do I handle the lifetime now?

			if ((lifetime <= 0 || --lifetime <= 0) && !force)
				return new IRenderable[0];

			if (!(force || Game.Settings.Game.DrawTargetLine))
				return new IRenderable[0];

			var current_activity = self.GetCurrentActivity();

			if (current_activity == null)
				return new IRenderable[0];

			var validTargets = new List<WPos>();
			validTargets.Add(self.CenterPosition);

			Color color = Color.Gray;

			var activityIterator = current_activity;

			while (activityIterator != null)
			{
				if (activityIterator is OpenRA.Mods.Common.Activities.Move.MovePart)
					activityIterator = ((OpenRA.Mods.Common.Activities.Move.MovePart)activityIterator).Move;

				foreach (var pair in activityIterator.GetTargets(self))
				{
					Target target = pair.Key;
					if (!activityIterator.IsCanceled && target.Type != TargetType.Invalid)
					{
						validTargets.Add(target.CenterPosition);
						color = pair.Value;
					}
				}

				activityIterator = activityIterator.NextActivity;
			}

			return new[] { (IRenderable)new TargetLineRenderable(validTargets, color) };
		}
	}
}
