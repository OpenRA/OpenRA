using System.Collections.Generic;
using OpenRa.Game.Graphics;
using OpenRa.Game.Traits;

namespace OpenRa.Game.Effects
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

		public void Tick()
		{
			if (a.IsDead || b.GetDamageModifier() > 0)
				Game.world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<Renderable> Render()
		{
			foreach (var r in a.Render())
				yield return r.WithPalette(PaletteType.Invuln);
		}
	}
}
