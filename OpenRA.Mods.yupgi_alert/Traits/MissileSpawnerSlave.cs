#region Copyright & License Information
/*
 * Modded by Boolbada of OP Mod.
 * Modded from cargo.cs but a lot changed.
 * 
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Yupgi_alert.Activities;
using OpenRA.Traits;

/*
Works without base engine modification.
However, Mods.Common\Activities\Air\Land.cs is modified to support the air units to land "mid air!"
See landHeight private variable to track the changes.
*/

namespace OpenRA.Mods.Yupgi_alert.Traits
{
	[Desc("This unit is \"slaved\" to a missile spawner master.")]
	public class MissileSpawnerSlaveInfo : BaseSpawnerSlaveInfo
	{
		public override object Create(ActorInitializer init) { return new MissileSpawnerSlave(init, this); }
	}

	public class MissileSpawnerSlave : BaseSpawnerSlave
	{
		public CarrierSlaveInfo Info { get; private set; }

		public MissileSpawnerSlave(ActorInitializer init, MissileSpawnerSlaveInfo info) : base(init, info) { }
	}
}
