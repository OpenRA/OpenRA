#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Collections.Generic;
using OpenRa.Graphics;
using OpenRa.Traits;

namespace OpenRa.Effects
{
	class Corpse : IEffect
	{
		readonly Animation anim;
		readonly float2 pos;
		readonly Player owner;

		public Corpse(Actor fromActor, int death)
		{
			anim = new Animation(fromActor.traits.GetOrDefault<RenderSimple>().GetImage(fromActor));
			anim.PlayThen("die{0}".F(death + 1),
				() => fromActor.World.AddFrameEndTask(w => w.Remove(this)));

			pos = fromActor.CenterLocation;
			owner = fromActor.Owner;
		}

		public void Tick( World world ) { anim.Tick(); }

		public IEnumerable<Renderable> Render()
		{
			yield return new Renderable(anim.Image, pos - .5f * anim.Image.size, owner.Palette);
		}
	}
}
