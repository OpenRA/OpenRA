#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	class StoresOreInfo : ITraitInfo
	{
		[Desc("Number of little squares used to display how filled unit is.")]
		public readonly int PipCount = 0;
		public readonly PipType PipColor = PipType.Yellow;
		public readonly int Capacity = 0;
		public object Create(ActorInitializer init) { return new StoresOre(init.self, this); }
	}

	class StoresOre : IPips, INotifyCapture, INotifyKilled, IExplodeModifier, IStoreOre, ISync
	{
		readonly StoresOreInfo Info;

		[Sync] public int Stored { get { return Player.OreCapacity == 0 ? 0 : Info.Capacity * Player.Ore / Player.OreCapacity; } }

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

		public void Killed(Actor self, AttackInfo e)
		{
			Player.TakeOre(Stored);	// Lose the stored ore
		}

		public IEnumerable<PipType> GetPips(Actor self)
		{
			return Exts.MakeArray( Info.PipCount,
				i => ( Player.Ore * Info.PipCount > i * Player.OreCapacity )
					? Info.PipColor : PipType.Transparent );
		}

		public bool ShouldExplode(Actor self) { return Stored > 0; }
	}
}
