using System.Collections.Generic;
using OpenRa.Graphics;
using OpenRa.Traits;

namespace OpenRa.Effects
{
	public class Explosion : IEffect
	{
		Animation anim;
		int2 pos;

		public Explosion( World world, int2 pixelPos, int style, bool isWater)
		{
			this.pos = pixelPos;
			var variantSuffix = isWater ? "w" : "";
			anim = new Animation("explosion");
				anim.PlayThen(style.ToString() + variantSuffix, 
					() => world.AddFrameEndTask(w => w.Remove(this)));
		}

		public void Tick( World world ) { anim.Tick(); }

		public IEnumerable<Renderable> Render()
		{
			yield return new Renderable(anim.Image, pos - .5f * anim.Image.size, 0);
		}

		public Player Owner { get { return null; } }
	}
}
