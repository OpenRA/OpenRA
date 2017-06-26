#region Copyright & License Information
/*
 * By Boolbada of OP Mod
 * Follows OpenRA's license as follows:
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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

/* Works without base engine modification
 * I like this module, I see many possibilities... This could be used in a hacky way as OCLs in C&C Genera.s
 */

namespace OpenRA.Mods.Yupgi_alert.Traits
{
	public class AirstrikeOnFireInfo : ITraitInfo
	{
		[WeaponReference]
		[Desc("Name of the armament that triggers the airstrike")]
		public readonly string Armament = null;

		[Desc("Which airstrike module to use?")]
		public readonly string OrderName = null;

		public object Create(ActorInitializer init) { return new AirstrikeOnFire(init, this); }
	}

	public class AirstrikeOnFire : INotifyAttack
	{
		readonly AirstrikeOnFireInfo info;
		readonly AirstrikePower ap;

		public AirstrikeOnFire(ActorInitializer init, AirstrikeOnFireInfo info)
		{
			this.info = info;

			var aps = init.Self.TraitsImplementing<AirstrikePower>();
			if (!aps.Any())
				return;
			ap = aps.Where(a => a.Info.OrderName == info.OrderName).First();
		}

		public void Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			if (a.Info.Name != info.Armament)
				return;

			if (ap == null)
				return;

			ap.SendAirstrike(self, target.CenterPosition);
			self.CurrentActivity.Cancel(self); // Cancel current activity and proceed to next.
		}

		public void PreparingAttack(Actor self, Target target, Armament a, Barrel barrel)
		{
			// do nothing
		}
	}
}