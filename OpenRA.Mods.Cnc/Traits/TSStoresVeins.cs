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

using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	public class TSStoresVeinsInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new TSStoresVeins(init.Self, this); }
	}

	public class TSStoresVeins : INotifyKilled
	{
		readonly TSStoresVeinsInfo info;
		TSPlayerResources player;

		public int Stored
		{
			get
			{
				return player.Veins;
			}
		}

		public TSStoresVeins(Actor self, TSStoresVeinsInfo info)
		{
			this.info = info;
			player = self.Owner.PlayerActor.Trait<TSPlayerResources>();
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			// Lose the stored resources.
			player.Veins = 0;
		}

	}
}
