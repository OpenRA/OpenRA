using System.Collections.Generic;
using System.Linq;
using OpenRa.Graphics;

namespace OpenRa.Traits
{
	class SubmarineInfo : ITraitInfo
	{
		public readonly float SubmergeDelay = 1.2f; // Seconds
		public readonly string SubmergeSound = "subshow1.aud";
		public readonly string SurfaceSound = "subshow1.aud";
		public object Create(Actor self) { return new Submarine(self); }
	}

	class Submarine : IRenderModifier, INotifyAttack, ITick, INotifyDamage
	{
		[Sync]
		int remainingSurfaceTime = 2;		/* setup for initial dive */
		
		Actor self;
		public Submarine(Actor self)
		{
			this.self = self;
		}

		void DoSurface()
		{
			if (remainingSurfaceTime <= 0)
				OnSurface();

			remainingSurfaceTime = (int)(self.Info.Traits.Get<SubmarineInfo>().SubmergeDelay * 25);
		}

		public void Attacking(Actor self) { DoSurface(); }
		public void Damaged(Actor self, AttackInfo e) { DoSurface(); }

		public IEnumerable<Renderable>
			ModifyRender(Actor self, IEnumerable<Renderable> rs)
		{
			if (remainingSurfaceTime > 0)
				return rs;

			if (self.Owner == self.World.LocalPlayer)
				return rs.Select(a => a.WithPalette("shadow"));
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
			Sound.Play(self.Info.Traits.Get<SubmarineInfo>().SurfaceSound);
		}

		void OnDive()
		{
			Sound.Play(self.Info.Traits.Get<SubmarineInfo>().SubmergeSound);
		}
	}
}
