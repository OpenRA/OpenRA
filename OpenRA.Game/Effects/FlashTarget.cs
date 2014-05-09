#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;

namespace OpenRA.Effects
{
	public class FlashTarget : IEffect
	{
		Actor target;
		Player player;
		int remainingTicks;

		public FlashTarget(Actor target)
			: this(target, null, 4) { }

		public FlashTarget(Actor target, int ticks)
			: this(target, null, ticks) { }

		public FlashTarget(Actor target, Player asPlayer)
			: this(target, asPlayer, 4) { }

		public FlashTarget(Actor target, Player asPlayer, int ticks)
		{
			this.target = target;
			player = asPlayer;
			remainingTicks = ticks;
			foreach (var e in target.World.Effects.OfType<FlashTarget>().Where(a => a.target == target).ToArray())
				target.World.Remove(e);
		}

		public void Tick(World world)
		{
			if (--remainingTicks == 0 || !target.IsInWorld)
				world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (target.IsInWorld && remainingTicks % 2 == 0)
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
