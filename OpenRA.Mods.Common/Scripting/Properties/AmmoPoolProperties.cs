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

using System.Linq;
using Eluant;
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("AmmoPool")]
	public class AmmoPoolProperties : ScriptActorProperties, Requires<AmmoPoolInfo>
	{
		readonly Actor self;
		readonly AmmoPool[] ammoPools;

		public AmmoPoolProperties(ScriptContext context, Actor self)
			: base(context, self)
		{
			this.self = self;
			ammoPools = self.TraitsImplementing<AmmoPool>().ToArray();
		}

		[Desc("Returns the count of the actor's specified ammopool.")]
		public int AmmoCount(string poolName = "primary")
		{
			var pool = ammoPools.FirstOrDefault(a => a.Info.Name == poolName);
			if (pool == null)
				throw new LuaException("Invalid ammopool name {0} queried on actor {1}.".F(poolName, self));

			return pool.GetAmmoCount();
		}

		[Desc("Returns the maximum count of ammo the actor can load.")]
		public int MaximumAmmoCount(string poolName = "primary")
		{
			var pool = ammoPools.FirstOrDefault(a => a.Info.Name == poolName);
			if (pool == null)
				throw new LuaException("Invalid ammopool name {0} queried on actor {1}.".F(poolName, self));

			return pool.Info.Ammo;
		}

		[Desc("Adds the specified amount of ammo to the specified ammopool.",
			"(Use a negative amount to remove ammo.)")]
		public void Reload(string poolName = "primary", int amount = 1)
		{
			var pool = ammoPools.FirstOrDefault(a => a.Info.Name == poolName);
			if (pool == null)
				throw new LuaException("Invalid ammopool name {0} queried on actor {1}.".F(poolName, self));

			if (amount > 0)
			{
				while (amount-- > 0)
					if (!pool.GiveAmmo())
						return;
			}
			else
			{
				while (amount++ < 0)
					if (!pool.TakeAmmo())
						return;
			}
		}
	}
}
