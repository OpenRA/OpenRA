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
using OpenRA.Effects;
using OpenRA.Graphics;

namespace OpenRA.Mods.Common.Effects
{
	public class CrateEffect : IEffect
	{
		readonly string palette;
		readonly Actor a;
		readonly Animation anim;

		public CrateEffect(Actor a, string seq, string palette)
		{
			this.a = a;
			this.palette = palette;

			anim = new Animation(a.World, "crate-effects");
			anim.PlayThen(seq, () => a.World.AddFrameEndTask(w => w.Remove(this)));
		}

		public void Tick(World world)
		{
			anim.Tick();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (!a.IsInWorld || a.World.FogObscures(a.CenterPosition))
				return SpriteRenderable.None;

			return anim.Render(a.CenterPosition, wr.Palette(palette));
		}
	}
}
