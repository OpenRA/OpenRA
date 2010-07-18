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
