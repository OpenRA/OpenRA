using System.Collections.Generic;
using OpenRa.Game.Graphics;
using OpenRa.Game.Traits;

namespace OpenRa.Game.Effects
{
	class Smoke : IEffect
	{
		readonly int2 pos;
		readonly Animation anim = new Animation("smokey");

		public Smoke(int2 pos)
		{
			this.pos = pos;
			anim.PlayThen("idle",
				() => Game.world.AddFrameEndTask(w => w.Remove(this)));
		}

		public void Tick()
		{
			anim.Tick();
		}

		public IEnumerable<Renderable> Render()
		{
			yield return new Renderable(anim.Image, pos - .5f * anim.Image.size, 0);
		}
	}
}
