#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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
	[Desc("Used for silos.")]
	class StoresResourcesInfo : ITraitInfo
	{
		[FieldLoader.Require]
		[Desc("Number of little squares used to display how filled unit is.")]
		public readonly int PipCount = 0;
		public readonly PipType PipColor = PipType.Yellow;
		[FieldLoader.Require]
		public readonly int Capacity = 0;
		public object Create(ActorInitializer init) { return new StoresResources(init.Self, this); }
	}

	class StoresResources : IPips, INotifyOwnerChanged, INotifyCapture, IExplodeModifier, IStoreResources, ISync, INotifyActorDisposing
	{
		readonly StoresResourcesInfo info;

		[Sync] public int Stored { get { return player.ResourceCapacity == 0 ? 0 : (int)((long)info.Capacity * player.Resources / player.ResourceCapacity); } }

		PlayerResources player;
		public StoresResources(Actor self, StoresResourcesInfo info)
		{
			player = self.Owner.PlayerActor.Trait<PlayerResources>();
			this.info = info;
		}

		public int Capacity { get { return info.Capacity; } }

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			player = newOwner.PlayerActor.Trait<PlayerResources>();
		}

		public void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			var resources = Stored;
			oldOwner.PlayerActor.Trait<PlayerResources>().TakeResources(resources);
			newOwner.PlayerActor.Trait<PlayerResources>().GiveResources(resources);
		}

		bool disposed;
		public void Disposing(Actor self)
		{
			if (disposed)
				return;

			player.TakeResources(Stored); // lose the stored resources
			disposed = true;
		}

		public IEnumerable<PipType> GetPips(Actor self)
		{
			return Enumerable.Range(0, info.PipCount).Select(i =>
				player.Resources * info.PipCount > i * player.ResourceCapacity
				? info.PipColor : PipType.Transparent);
		}

		public bool ShouldExplode(Actor self) { return Stored > 0; }
	}
}
