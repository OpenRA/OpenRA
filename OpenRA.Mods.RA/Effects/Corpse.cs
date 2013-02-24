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
	public class Corpse : IEffect
	{
		readonly Animation anim;
		readonly float2 pos;
		readonly RenderSimple rs;
		readonly Player p;

		public Corpse(Actor fromActor, string sequence)
		{
			p = fromActor.Owner;
			rs = fromActor.Trait<RenderSimple>();
			anim = new Animation(rs.GetImage(fromActor));
			anim.PlayThen(sequence,
				() => fromActor.World.AddFrameEndTask(w => w.Remove(this)));

			pos = fromActor.CenterLocation.ToFloat2();
		}

		public void Tick( World world ) { anim.Tick(); }

		public IEnumerable<Renderable> Render(WorldRenderer wr)
		{
			yield return new Renderable(anim.Image, pos - .5f * anim.Image.size, rs.Palette(p, wr), (int)pos.Y);
		}
	}
}
