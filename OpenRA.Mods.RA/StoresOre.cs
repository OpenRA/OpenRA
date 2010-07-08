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
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class StoresOreInfo : ITraitInfo
	{
		public readonly int PipCount = 0;
		public readonly PipType PipColor = PipType.Yellow;
		public readonly int Capacity = 0;
		public object Create(ActorInitializer init) { return new StoresOre(init.self, this); }
	}

	class StoresOre : IPips, INotifyCapture, INotifyDamage, IExplodeModifier, IStoreOre
	{		
		readonly PlayerResources Player;
		readonly StoresOreInfo Info;
		
		public StoresOre(Actor self, StoresOreInfo info)
		{
			Player = self.Owner.PlayerActor.traits.Get<PlayerResources>();
			Info = info;
		}
		
		public int Capacity { get { return Info.Capacity; } }
		
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
				Player.TakeOre(Stored(self));		// Lose the stored ore
		}
		
		public IEnumerable<PipType> GetPips(Actor self)
		{
			return Graphics.Util.MakeArray( Info.PipCount, 
				i => (Player.GetSiloFullness() > i * 1.0f / Info.PipCount) 
					? Info.PipColor : PipType.Transparent );
		}

		public bool ShouldExplode(Actor self) { return Player.GetSiloFullness() > 0; }
	}
}
