using System.Collections.Generic;
using System.Linq;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.Traits
{
    class Submarine : IRenderModifier, INotifyAttack, ITick
    {
        int remainingSurfaceTime = 2;		/* setup for initial dive */

        public Submarine(Actor self) { }

        public void Attacking(Actor self)
        {
            if (remainingSurfaceTime <= 0)
                OnSurface();

            remainingSurfaceTime = (int)(Rules.General.SubmergeDelay * 60 * 25);
        }

        public IEnumerable<Tuple<Sprite, float2, int>>
            ModifyRender(Actor self, IEnumerable<Tuple<Sprite, float2, int>> rs)
        {
            if (remainingSurfaceTime > 0)
                return rs;

            if (self.Owner == Game.LocalPlayer)
                return rs.Select(a => Tuple.New(a.a, a.b, 8));
            else
                return new Tuple<Sprite, float2, int>[] { };
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
