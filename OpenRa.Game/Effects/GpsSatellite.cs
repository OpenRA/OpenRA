using System.Collections.Generic;
using OpenRa.Game.Graphics;
using OpenRa.Game.Traits;

namespace OpenRa.Game.Effects
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

		public void Tick()
		{
			anim.Tick();
			offset.Y -= heightPerTick;
			
			if (offset.Y < 0)
				Game.world.AddFrameEndTask(w => w.Remove(this));
		}
		
		public IEnumerable<Renderable> Render()
		{
			yield return new Renderable(anim.Image,offset, PaletteType.Gold);
		}
	}
}
