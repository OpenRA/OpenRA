#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Adds capacity to a player's harvested resource limit.")]
	class StoresResourcesInfo : ITraitInfo
	{
		[FieldLoader.Require]
		public readonly int Capacity = 0;

		[FieldLoader.Require]
		[Desc("Number of little squares used to display how filled unit is.")]
		public readonly int PipCount = 0;

		public readonly PipType PipColor = PipType.Yellow;

		public object Create(ActorInitializer init) { return new StoresResources(init.Self, this); }
	}

	class StoresResources : IPips, INotifyOwnerChanged, INotifyCapture, IExplodeModifier, IStoreResources, ISync, INotifyKilled
	{
		readonly StoresResourcesInfo info;
		PlayerResources player;

		[Sync] public int Stored { get { return player.ResourceCapacity == 0 ? 0 : (int)((long)info.Capacity * player.Resources / player.ResourceCapacity); } }

		public StoresResources(Actor self, StoresResourcesInfo info)
		{
			this.info = info;
			player = self.Owner.PlayerActor.Trait<PlayerResources>();
		}

		int IStoreResources.Capacity { get { return info.Capacity; } }

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			player = newOwner.PlayerActor.Trait<PlayerResources>();
		}

		void INotifyCapture.OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			var resources = Stored;
			oldOwner.PlayerActor.Trait<PlayerResources>().TakeResources(resources);
			newOwner.PlayerActor.Trait<PlayerResources>().GiveResources(resources);
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			// Lose the stored resources
			player.TakeResources(Stored);
		}

		IEnumerable<PipType> IPips.GetPips(Actor self)
		{
			return Enumerable.Range(0, info.PipCount).Select(i =>
				player.Resources * info.PipCount > i * player.ResourceCapacity
				? info.PipColor : PipType.Transparent);
		}

		bool IExplodeModifier.ShouldExplode(Actor self) { return Stored > 0; }
	}
}
