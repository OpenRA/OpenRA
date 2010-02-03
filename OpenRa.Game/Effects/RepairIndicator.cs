using System.Collections.Generic;
using OpenRa.Graphics;
using OpenRa.Traits;

namespace OpenRa.Effects
{
	class RepairIndicator : IEffect
	{
		int framesLeft = (int)(Rules.General.RepairRate * 25 * 60 / 2);
		Actor a;
		Animation anim = new Animation("select");

		public RepairIndicator(Actor a) { this.a = a; anim.PlayRepeating("repair"); }

		public void Tick( World world )
		{
			if (--framesLeft == 0 || a.IsDead)
				world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<Renderable> Render()
		{
			yield return new Renderable(anim.Image, 
				a.CenterLocation - .5f * anim.Image.size, "chrome");
		}
	}
}
