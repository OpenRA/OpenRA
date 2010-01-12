using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Graphics;
using OpenRa.Game.Traits;

namespace OpenRa.Game.Effects
{
	class Corpse : IEffect
	{
		readonly Animation anim;
		readonly float2 pos;
		readonly Player owner;

		public Corpse(Actor fromActor, int death)
		{
			anim = new Animation(fromActor.traits.GetOrDefault<RenderSimple>().GetImage(fromActor));
			anim.PlayThen("die{0}".F(death + 1),
				() => Game.world.AddFrameEndTask(w => w.Remove(this)));

			pos = fromActor.CenterLocation;
			owner = fromActor.Owner;
		}

		public void Tick() { anim.Tick(); }

		public IEnumerable<Renderable> Render()
		{
			yield return new Renderable(anim.Image, pos - .5f * anim.Image.size, owner.Palette);
		}
	}
}
