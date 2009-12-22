using System.Collections.Generic;
using System.Linq;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.Traits
{
    class Submarine : IRenderModifier, INotifyAttack, ITick, INotifyDamage
    {
        int remainingSurfaceTime = 2;		/* setup for initial dive */

        public Submarine(Actor self) { }

		void DoSurface()
		{
			if (remainingSurfaceTime <= 0)
				OnSurface();

			remainingSurfaceTime = (int)(Rules.General.SubmergeDelay * 60 * 25);
		}

        public void Attacking(Actor self) { DoSurface(); }
		public void Damaged(Actor self, AttackInfo e) { DoSurface(); }

        public IEnumerable<Renderable>
            ModifyRender(Actor self, IEnumerable<Renderable> rs)
        {
            if (remainingSurfaceTime > 0)
                return rs;

			if (self.Owner == Game.LocalPlayer)
				return rs.Select(a => a.WithPalette(8));
			else
				return new Renderable[] { };
        }

        public void Tick(Actor self)
        {
            if (remainingSurfaceTime > 0)
                if (--remainingSurfaceTime <= 0)
                    OnDive();
        }

        void OnSurface()
        {
            Sound.Play("subshow1.aud");
        }

        void OnDive()
        {
            Sound.Play("subshow1.aud");		/* is this the right sound?? */
        }
	}
}
