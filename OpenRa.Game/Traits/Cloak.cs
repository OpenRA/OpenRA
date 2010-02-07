using System.Collections.Generic;
using System.Linq;
using OpenRa.Graphics;

namespace OpenRa.Traits
{
	class CloakInfo : ITraitInfo
	{
		public readonly float CloakDelay = 1.2f; // Seconds
		public readonly string CloakSound = "ironcur9.aud";
		public readonly string UncloakSound = "ironcur9.aud";
		public object Create(Actor self) { return new Cloak(self); }
	}

	class Cloak : IRenderModifier, INotifyAttack, ITick
	{
		[Sync]
		int remainingUncloakTime = 2;		/* setup for initial cloak */

		Actor self;
		public Cloak(Actor self)
		{
			this.self = self;
		}

		public void Attacking(Actor self)
		{
			if (remainingUncloakTime <= 0)
				OnCloak();

			remainingUncloakTime = (int)(self.Info.Traits.Get<CloakInfo>().CloakDelay * 25);
		}

		public IEnumerable<Renderable>
			ModifyRender(Actor self, IEnumerable<Renderable> rs)
		{
			if (remainingUncloakTime > 0)
				return rs;

			if (self.Owner == self.World.LocalPlayer)
				return rs.Select(a => a.WithPalette("shadow"));
			else
				return new Renderable[] { };
		}

		public void Tick(Actor self)
		{
			if (remainingUncloakTime > 0)
				if (--remainingUncloakTime <= 0)
					OnUncloak();
		}

		void OnCloak()
		{
			Sound.Play(self.Info.Traits.Get<CloakInfo>().CloakSound);
		}

		void OnUncloak()
		{
			Sound.Play(self.Info.Traits.Get<CloakInfo>().UncloakSound);
		}
	}
}
