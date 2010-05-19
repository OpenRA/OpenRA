#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Traits
{
	class CloakInfo : ITraitInfo
	{
		public readonly float InitialDelay = .4f;	// seconds
		public readonly float CloakDelay = 1.2f; // Seconds
		public readonly string CloakSound = "subshow1.aud";
		public readonly string UncloakSound = "subshow1.aud";
		public object Create(Actor self) { return new Cloak(self); }
	}

	class Cloak : IRenderModifier, INotifyAttack, ITick, INotifyDamage
	{
		[Sync]
		int remainingTime;
		
		Actor self;
		public Cloak(Actor self)
		{
			remainingTime = (int)(self.Info.Traits.Get<CloakInfo>().InitialDelay * 25);
			this.self = self;
		}

		void DoSurface()
		{
			if (remainingTime <= 0)
				OnSurface();

			remainingTime = (int)(self.Info.Traits.Get<CloakInfo>().CloakDelay * 25);
		}

		public void Attacking(Actor self) { DoSurface(); }
		public void Damaged(Actor self, AttackInfo e) { DoSurface(); }

		public IEnumerable<Renderable>
			ModifyRender(Actor self, IEnumerable<Renderable> rs)
		{
			if (remainingTime > 0)
				return rs;

			if (self.Owner == self.World.LocalPlayer)
				return rs.Select(a => a.WithPalette("shadow"));
			else
				return new Renderable[] { };
		}

		public void Tick(Actor self)
		{
			if (remainingTime > 0)
				if (--remainingTime <= 0)
					OnDive();
		}

		void OnSurface()
		{
			Sound.Play(self.Info.Traits.Get<CloakInfo>().UncloakSound, self.CenterLocation);
		}

		void OnDive()
		{
			Sound.Play(self.Info.Traits.Get<CloakInfo>().CloakSound, self.CenterLocation);
		}

		public bool Cloaked { get { return remainingTime > 0; } }
	}
}
