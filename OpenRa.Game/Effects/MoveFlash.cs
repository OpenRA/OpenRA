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
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Effects
{
	class MoveFlash : IEffect
	{
		Animation anim = new Animation("moveflsh");
		float2 pos;

		public MoveFlash( World world, float2 pos )
		{
			this.pos = pos;
			anim.PlayThen( "idle", 
				() => world.AddFrameEndTask( 
					w => w.Remove( this ) ) );
		}

		public void Tick( World world ) { anim.Tick(); }

		public IEnumerable<Renderable> Render()
		{
			yield return new Renderable(anim.Image, pos - .5f * anim.Image.size, "shadow");
		}
	}
}
