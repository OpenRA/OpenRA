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

using System;
using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Effects
{
	class IonCannon : IEffect
	{
		int2 Target;
		Animation anim;

		public IonCannon(World world, int2 location)
		{
			Target = location;
			anim = new Animation("ionsfx");
			anim.PlayThen("idle",
				() => world.AddFrameEndTask(w => w.Remove(this)));
		}

		public void Tick(World world)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<Renderable> Render()
		{
			yield return new Renderable(anim.Image,
				Target - new float2(.5f * anim.Image.size.X, anim.Image.size.Y - Game.CellSize),
				"effect");
		}
	}
}
