#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Effects
{
	public class FlashTarget : IEffect
	{
		readonly Actor target;
		readonly int count;
		readonly int interval;

		readonly TintModifiers modifiers;
		readonly float3 tint;
		readonly float? alpha;

		int tick;

		FlashTarget(Actor target, int count, int interval, int delay)
		{
			this.target = target;
			this.count = count;
			this.interval = interval;
			tick = -delay;

			target.World.RemoveAll(effect =>
			{
				return effect is FlashTarget flashTarget && flashTarget.target == target;
			});
		}

		public FlashTarget(Actor target, Color color, float alpha = 0.5f, int count = 2, int interval = 2, int delay = 0)
			: this(target, count, interval, delay)
		{
			modifiers = TintModifiers.ReplaceColor;
			tint = new float3(color.R, color.G, color.B) / 255f;
			this.alpha = alpha;
		}

		public FlashTarget(Actor target, float3 tint, int count = 2, int interval = 2, int delay = 0)
			: this(target, count, interval, delay)
		{
			this.tint = tint;
		}

		public void Tick(World world)
		{
			if (++tick >= count * interval || !target.IsInWorld)
				world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (target.IsInWorld && tick >= 0 && tick % interval == 0)
			{
				return target.Render(wr)
					.Where(r => !r.IsDecoration && r is IModifyableRenderable)
					.Select(r =>
					{
						var mr = (IModifyableRenderable)r;
						mr = mr.WithTint(tint, mr.TintModifiers | modifiers);
						if (alpha.HasValue)
							mr = mr.WithAlpha(alpha.Value);

						return mr;
					});
			}

			return SpriteRenderable.None;
		}
	}
}
