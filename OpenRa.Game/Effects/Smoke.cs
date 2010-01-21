using System.Collections.Generic;
using OpenRa.Graphics;
using OpenRa.Traits;

namespace OpenRa.Effects
{
	class Smoke : IEffect
	{
		readonly int2 pos;
		readonly Animation anim = new Animation("smokey");

		public Smoke(World world, int2 pos)
		{
			this.pos = pos;
			anim.PlayThen("idle",
				() => world.AddFrameEndTask(w => w.Remove(this)));
		}

		public void Tick( World world )
		{
			anim.Tick();
		}

		public IEnumerable<Renderable> Render()
		{
			yield return new Renderable(anim.Image, pos - .5f * anim.Image.size, 0);
		}
	}
}
