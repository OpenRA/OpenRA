using System.Collections.Generic;
using OpenRa.Effects;
using OpenRa.Graphics;
using OpenRa.Traits;

namespace OpenRa.Mods.RA.Effects
{
	class GpsSatellite : IEffect
	{
		readonly float heightPerTick = 10;
		float2 offset;
		Animation anim = new Animation("sputnik");

		public GpsSatellite(float2 offset)
		{
			this.offset = offset;
			anim.PlayRepeating("idle");
		}

		public void Tick( World world )
		{
			anim.Tick();
			offset.Y -= heightPerTick;
			
			if (offset.Y < 0)
				world.AddFrameEndTask(w => w.Remove(this));
		}
		
		public IEnumerable<Renderable> Render()
		{
			yield return new Renderable(anim.Image,offset, "effect");
		}
	}
}
