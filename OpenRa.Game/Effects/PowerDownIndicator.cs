using System.Collections.Generic;
using OpenRa.Graphics;
using OpenRa.Traits;

namespace OpenRa.Effects
{
	class PowerDownIndicator : IEffect
	{
		Actor a;
		Building b;
		Animation anim = new Animation("powerdown");
		bool removeNextFrame = false;
		bool indicatorState = true;
		int stateTicks = 0;
		
		public PowerDownIndicator(Actor a)
		{
			this.a = a;
			this.b = a.traits.Get<Building>();
			anim.PlayRepeating("disabled");
		}

		public void Tick( World world )
		{
			if (removeNextFrame == true)
				world.AddFrameEndTask(w => w.Remove(this));
			
			// Fix off-by one frame bug with undisabling causing low-power
			if (!b.Disabled || a.IsDead)
				removeNextFrame = true;
			
			// Flash power icon
			if (++stateTicks == 15)
			{
				stateTicks = 0;
				indicatorState = !indicatorState;
			}
		}

		public IEnumerable<Renderable> Render()
		{
			foreach (var r in a.Render())
				yield return r.WithPalette(PaletteType.Disabled);
			
			if (b.ManuallyDisabled && indicatorState)
				yield return new Renderable(anim.Image,
				a.CenterLocation - .5f * anim.Image.size, PaletteType.Chrome);
		}
	}
}
