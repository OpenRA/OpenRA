using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Graphics;
using OpenRa.Game.Traits;

namespace OpenRa.Game.Effects
{
	class MoveFlash : IEffect
	{
		Animation anim = new Animation("moveflsh");
		float2 pos;

		public MoveFlash( float2 pos )
		{
			this.pos = pos;
			anim.PlayThen( "idle", 
				() => Game.world.AddFrameEndTask( 
					w => w.Remove( this ) ) );
		}

		public void Tick() { anim.Tick(); }

		public IEnumerable<Renderable> Render()
		{
			yield return new Renderable(anim.Image, pos - .5f * anim.Image.size, 8);
		}
	}
}
