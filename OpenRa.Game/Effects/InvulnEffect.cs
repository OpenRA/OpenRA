using System.Collections.Generic;
using OpenRa.Graphics;
using OpenRa.Traits;

namespace OpenRa.Effects
{
	class InvulnEffect : IEffect
	{
		Actor a;
		IronCurtainable b;

		public InvulnEffect(Actor a)
		{
			this.a = a;
			this.b = a.traits.Get<IronCurtainable>();
		}

		public void Tick( World world )
		{
			if (a.IsDead || b.GetDamageModifier() > 0)
				world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<Renderable> Render()
		{
			foreach (var r in a.Render())
				yield return r.WithPalette(PaletteType.Invuln);
		}
	}
}
