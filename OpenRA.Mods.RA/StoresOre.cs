#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
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
		readonly StoresOreInfo Info;
		
		[Sync]
		public int Stored { get { return Player.OreCapacity == 0 ? 0 : Info.Capacity * Player.Ore / Player.OreCapacity; } }
		
		PlayerResources Player;
		public StoresOre(Actor self, StoresOreInfo info)
		{
			Player = self.Owner.PlayerActor.Trait<PlayerResources>();
			Info = info;
		}
		
		public int Capacity { get { return Info.Capacity; } }
		
		public void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			var ore = Stored;
			Player.TakeOre(ore);
			Player = newOwner.PlayerActor.Trait<PlayerResources>();
			Player.GiveOre(ore);
		}
		
		public void Damaged(Actor self, AttackInfo e)
		{
			if (self.IsDead())
				Player.TakeOre(Stored);	// Lose the stored ore
		}
		
		public IEnumerable<PipType> GetPips(Actor self)
		{
			return Graphics.Util.MakeArray( Info.PipCount, 
				i => ( Player.Ore * Info.PipCount > i * Player.OreCapacity ) 
					? Info.PipColor : PipType.Transparent );
		}

		public bool ShouldExplode(Actor self) { return Stored > 0; }
	}
}
