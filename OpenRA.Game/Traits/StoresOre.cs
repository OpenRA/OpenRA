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
 
namespace OpenRA.Traits
{
	class StoresOreInfo : ITraitInfo
	{
		public readonly int PipCount = 0;
		public readonly PipType PipColor = PipType.Yellow;
		public readonly int Capacity = 0;
		public readonly string DeathWeapon = null;
		public object Create(Actor self) { return new StoresOre(self, this); }
	}

	class StoresOre : IPips, INotifyCapture, INotifyDamage
	{		
		readonly PlayerResources Player;
		readonly StoresOreInfo Info;
		
		public StoresOre(Actor self, StoresOreInfo info)
		{
			Player = self.Owner.PlayerActor.traits.Get<PlayerResources>();
			Info = info;
		}
		
		public void OnCapture(Actor self, Actor captor)
		{
			var ore = Stored(self);
			Player.TakeOre(ore);
			Player.GiveOre(ore);
		}
		
		int Stored(Actor self)
		{
			return (int)(Player.GetSiloFullness() * Info.Capacity);
		}
		
		public void Damaged(Actor self, AttackInfo e)
		{
			if (self.IsDead && Player.GetSiloFullness() > 0)
			{
				if (Info.DeathWeapon != null)
				{
					Combat.DoExplosion(e.Attacker, Info.DeathWeapon,
						self.CenterLocation.ToInt2(), 0);
				}
				
				// Lose the stored ore
				Player.TakeOre(Stored(self));
			}
		}
		
		public IEnumerable<PipType> GetPips(Actor self)
		{
			return Graphics.Util.MakeArray( Info.PipCount, 
				i => (Player.GetSiloFullness() > i * 1.0f / Info.PipCount) 
					? Info.PipColor : PipType.Transparent );
		}
	}
}
