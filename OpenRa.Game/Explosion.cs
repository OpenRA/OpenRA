using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Graphics;
using IjwFramework.Types;
using OpenRa.Game.GameRules;

namespace OpenRa.Game
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

		public IEnumerable<Tuple<Sprite, float2, int>> Render()
		{
			yield return Tuple.New(anim.Image, pos.ToFloat2() - 0.5f * anim.Image.size, 0);
		}

		public Player Owner { get { return null; } }
	}
}
