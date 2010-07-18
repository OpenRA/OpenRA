#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
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
		float2 offset = new float2(-4,0);

		public CrateEffect(Actor a, string seq, int2 offset)
			: this(a, seq)
		{
			this.offset = offset;
		}

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
		
		public IEnumerable<Renderable> Render()
		{
			yield return new Renderable(anim.Image,
				a.CenterLocation - .5f * anim.Image.size + offset, "effect");
		}
	}
}
