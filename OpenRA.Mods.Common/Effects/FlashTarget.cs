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
using System.Linq;
using OpenRA.Effects;
using OpenRA.Graphics;

namespace OpenRA.Mods.Common.Effects
{
	public class FlashTarget : IEffect
	{
		readonly Actor target;
		readonly Player player;
		readonly int count;
		readonly int interval;
		int tick;

		public FlashTarget(Actor target, Player asPlayer = null, int count = 2, int interval = 2, int delay = 0)
		{
			this.target = target;
			player = asPlayer;
			this.count = count;
			this.interval = interval;
			tick = -delay;

			target.World.RemoveAll(effect =>
			{
				var flashTarget = effect as FlashTarget;
				return flashTarget != null && flashTarget.target == target;
			});
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
				var palette = wr.Palette(player == null ? "highlight" : "highlight" + player.InternalName);
				return target.Render(wr)
					.Where(r => !r.IsDecoration)
					.Select(r => r.WithPalette(palette));
			}

			return SpriteRenderable.None;
		}
	}
}
