using System.Collections.Generic;
using OpenRa.Graphics;
using OpenRa.Traits;
using OpenRa.Effects;

namespace OpenRa.Mods.RA.Effects
{
	class CrateEffectSpeedUpgrade : IEffect
	{
		Actor a;
		Animation anim = new Animation("crate-effects");
		float2 doorOffset = new float2(-4,0);

		public CrateEffectSpeedUpgrade(Actor a)
		{
			this.a = a;
			anim.PlayThen("speed",
				() => a.World.AddFrameEndTask(w => w.Remove(this)));
		}

		public void Tick( World world )
		{
			anim.Tick();
		}
		
		public IEnumerable<Renderable> Render()
		{
			yield return new Renderable(anim.Image,
				a.CenterLocation - .5f * anim.Image.size + doorOffset, PaletteType.Gold);
		}
	}
}
