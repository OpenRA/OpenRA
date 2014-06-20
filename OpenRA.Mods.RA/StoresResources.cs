#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class StoresResourcesInfo : ITraitInfo
	{
		[Desc("Number of little squares used to display how filled unit is.")]
		public readonly int PipCount = 0;
		public readonly PipType PipColor = PipType.Yellow;
		public readonly int Capacity = 0;
		public object Create(ActorInitializer init) { return new StoresResources(init.self, this); }
	}

	class StoresResources : IPips, INotifyCapture, INotifyKilled, IExplodeModifier, IStoreResources, ISync
	{
		readonly StoresResourcesInfo Info;

		[Sync] public int Stored { get { return Player.ResourceCapacity == 0 ? 0 : Info.Capacity * Player.Resources / Player.ResourceCapacity; } }

		PlayerResources Player;
		public StoresResources(Actor self, StoresResourcesInfo info)
		{
			Player = self.Owner.PlayerActor.Trait<PlayerResources>();
			Info = info;
		}

		public int Capacity { get { return Info.Capacity; } }

		public void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			var resources = Stored;
			Player.TakeResources(resources);
			Player = newOwner.PlayerActor.Trait<PlayerResources>();
			Player.GiveResources(resources);
		}

		public void Killed(Actor self, AttackInfo e)
		{
			Player.TakeResources(Stored); // lose the stored resources
		}

		public IEnumerable<PipType> GetPips(Actor self)
		{
			return Exts.MakeArray(Info.PipCount,
				i => (Player.Resources * Info.PipCount > i * Player.ResourceCapacity)
					? Info.PipColor : PipType.Transparent);
		}

		public bool ShouldExplode(Actor self) { return Stored > 0; }
	}
}
