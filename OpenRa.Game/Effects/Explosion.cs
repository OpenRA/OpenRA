using System.Collections.Generic;
using OpenRa.Graphics;
using OpenRa.Traits;

namespace OpenRa.Effects
{
	class Explosion : IEffect
	{
		Animation anim;
		int2 pos;

		public Explosion(int2 pixelPos, int style, bool isWater)
		{
			this.pos = pixelPos;
			var variantSuffix = isWater ? "w" : "";
			anim = new Animation("explosion");
				anim.PlayThen(style.ToString() + variantSuffix, 
					() => Game.world.AddFrameEndTask(w => w.Remove(this)));
		}

		public void Tick() { anim.Tick(); }

		public IEnumerable<Renderable> Render()
		{
			yield return new Renderable(anim.Image, pos - .5f * anim.Image.size, 0);
		}

		public Player Owner { get { return null; } }
	}
}
