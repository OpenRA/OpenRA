#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Adds capacity to a player's harvested resource limit.")]
	public class StoresResourcesInfo : TraitInfo
	{
		[FieldLoader.Require]
		public readonly int Capacity = 0;

		public override object Create(ActorInitializer init) { return new StoresResources(init.Self, this); }
	}

	public class StoresResources : INotifyOwnerChanged, INotifyCapture, IStoreResources, ISync, INotifyKilled, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		readonly StoresResourcesInfo info;
		PlayerResources player;

		[Sync]
		public int Stored => player.ResourceCapacity == 0 ? 0 : (int)((long)info.Capacity * player.Resources / player.ResourceCapacity);

		public StoresResources(Actor self, StoresResourcesInfo info)
		{
			this.info = info;
			player = self.Owner.PlayerActor.Trait<PlayerResources>();
		}

		int IStoreResources.Capacity => info.Capacity;

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			player = newOwner.PlayerActor.Trait<PlayerResources>();
		}

		void INotifyCapture.OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner, BitSet<CaptureType> captureTypes)
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

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			player.AddStorage(info.Capacity);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			player.RemoveStorage(info.Capacity);
		}
	}
}
