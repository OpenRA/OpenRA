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
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Effects
{
	class CrateEffect : IEffect
	{
		Actor a;
		Animation anim = new Animation("crate-effects");

		public CrateEffect(Actor a, string seq)
		{
			this.a = a;
			anim.PlayThen(seq,
				() => a.World.AddFrameEndTask(w => w.Remove(this)));
		}

		public void Tick( World world )
		{
			anim.Tick();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (a.IsInWorld)
				yield return new SpriteRenderable(anim.Image,
					a.CenterPosition, 0, wr.Palette("effect"), 1f);
		}
	}
}
