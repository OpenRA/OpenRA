using System.Collections.Generic;
using OpenRa.Game.Graphics;
using OpenRa.Game.Traits;

namespace OpenRa.Game.Effects
{
	class PowerDownIndicator : IEffect
	{
		Actor a;
		Building b;
		Animation anim = new Animation("powerdown");

		public PowerDownIndicator(Actor a)
		{
			this.a = a;
			this.b = a.traits.Get<Building>();
			anim.PlayRepeating("disabled");
		}

		public void Tick()
		{
			if (!b.Disabled || a.IsDead)
				Game.world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<Renderable> Render()
		{
			foreach (var r in a.Render())
				yield return r.WithPalette(PaletteType.Disabled);
			
			if (b.ManuallyDisabled)
				yield return new Renderable(anim.Image,
				a.CenterLocation - .5f * anim.Image.size, PaletteType.Chrome);
		}
	}
}
