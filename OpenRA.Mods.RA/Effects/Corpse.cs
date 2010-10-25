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
	public class Corpse : IEffect
	{
		readonly Animation anim;
		readonly float2 pos;
		readonly Player owner;

		public Corpse(Actor fromActor, int death)
		{
			anim = new Animation(fromActor.TraitOrDefault<RenderSimple>().GetImage(fromActor));
			anim.PlayThen("die{0}".F(death + 1),
				() => fromActor.World.AddFrameEndTask(w => w.Remove(this)));

			pos = fromActor.CenterLocation;
			owner = fromActor.Owner;
		}

		public void Tick( World world ) { anim.Tick(); }

		public IEnumerable<Renderable> Render()
		{
			yield return new Renderable(anim.Image, pos - .5f * anim.Image.size, owner.Palette, (int)pos.Y);
		}
	}
}
