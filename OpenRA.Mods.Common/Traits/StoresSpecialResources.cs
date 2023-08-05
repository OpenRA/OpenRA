#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
	[Desc("Adds a special resources capacity to a player's harvested resource limit.")]
	public class StoresSpecialResourcesInfo : TraitInfo
	{
		[FieldLoader.Require]
		public readonly string Type = null;

		[FieldLoader.Require]
		public readonly int Capacity = 0;

		public override object Create(ActorInitializer init) { return new StoresSpecialResources(init.Self, this); }
	}

	public class StoresSpecialResources : INotifyOwnerChanged, INotifyCapture, IStoreResources, ISync, INotifyKilled, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		readonly StoresSpecialResourcesInfo info;
		PlayerResources player;

		[Sync]
		public int Stored
		{
			get
			{
				if (player.SpecialResourcesCapacity.ContainsKey(info.Type))
				{
					return player.SpecialResourcesCapacity[info.Type] == 0 ? 0 : (int)((long)info.Capacity * player.HasSpecialResources(info.Type) / player.SpecialResourcesCapacity[info.Type]);
				}
				else
					return 0;
			}
		}

		public StoresSpecialResources(Actor self, StoresSpecialResourcesInfo info)
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
			var oldpr = oldOwner.PlayerActor.Trait<PlayerResources>();
			var newpr = newOwner.PlayerActor.Trait<PlayerResources>();
			newpr.GiveSpecialResources(resources, info.Type);
			oldpr.TakeSpecialResources(resources, info.Type);
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			// Lose the stored resources
			player.TakeSpecialResources(Stored, info.Type);
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			player.AddSpecialStorage(info.Capacity, info.Type);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			player.RemoveSpecialStorage(info.Capacity, info.Type);
		}
	}
}
