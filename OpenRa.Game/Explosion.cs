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

		public Explosion(int2 pixelPos, int style)
		{
			this.pos = pixelPos;

			anim = new Animation("explosion");
			if (style != 0)
				anim.PlayThen(style.ToString(), 
					() => Game.world.AddFrameEndTask(w => w.Remove(this)));
			else
				Game.world.AddFrameEndTask(w => w.Remove(this));
		}

		public void Tick() { anim.Tick(); }

		public IEnumerable<Pair<Sprite, float2>> Render()
		{
			yield return Pair.New(anim.Image, pos.ToFloat2() - 0.5f * anim.Image.size);
		}

		public Player Owner { get { return null; } }
	}
}
