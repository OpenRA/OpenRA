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

using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Used for silos defined on the player actor.")]
	class PlayerSiloInfo : ITraitInfo
	{
		[FieldLoader.Require]
		public readonly int Capacity = 0;
		public object Create(ActorInitializer init) { return new PlayerSilo(init.Self, this); }
	}

	class PlayerSilo : IStoreResources, ISync
	{
		readonly PlayerSiloInfo info;

		[Sync] public int Stored { get { return player.ResourceCapacity == 0 ? 0 : (int)((long)info.Capacity * player.Resources / player.ResourceCapacity); } }

		PlayerResources player;
		public PlayerSilo(Actor self, PlayerSiloInfo info)
		{
			player = self.Trait<PlayerResources>();
			this.info = info;
		}

		int IStoreResources.Capacity
		{
			get { return info.Capacity; }
		}
	}
}
