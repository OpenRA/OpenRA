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
			Sound.Play(self.Info.Traits.Get<SubmarineInfo>().SurfaceSound, self.CenterLocation);
		}

		void OnDive()
		{
			Sound.Play(self.Info.Traits.Get<SubmarineInfo>().SubmergeSound, self.CenterLocation);
		}
	}
}
