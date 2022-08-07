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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class RearmableInfo : TraitInfo
	{
		[ActorReference]
		[FieldLoader.Require]
		[Desc("Actors that this actor can dock to and get rearmed by.")]
		public readonly HashSet<string> RearmActors = new HashSet<string> { };

		[Desc("Name(s) of AmmoPool(s) that use this trait to rearm.")]
		public readonly HashSet<string> AmmoPools = new HashSet<string> { "primary" };

		public override object Create(ActorInitializer init) { return new Rearmable(this); }
	}

	public class Rearmable : INotifyCreated, INotifyDockClient
	{
		public readonly RearmableInfo Info;

		public Rearmable(RearmableInfo info)
		{
			Info = info;
		}

		public AmmoPool[] RearmableAmmoPools { get; private set; }

		void INotifyCreated.Created(Actor self)
		{
			RearmableAmmoPools = self.TraitsImplementing<AmmoPool>().Where(p => Info.AmmoPools.Contains(p.Info.Name)).ToArray();
		}

		void INotifyDockClient.Docked(Actor self, Actor dock)
		{
			// Reset the ReloadDelay to avoid any issues with early cancellation
			// from previous reload attempts (explicit order, host building died, etc).
			foreach (var pool in RearmableAmmoPools)
				pool.RemainingTicks = pool.Info.ReloadDelay;
		}

		void INotifyDockClient.Undocked(Actor self, Actor dock) { }

		public bool RearmTick(Actor self)
		{
			foreach (var ammoPool in RearmableAmmoPools)
			{
				if (!ammoPool.HasFullAmmo)
				{
					if (--ammoPool.RemainingTicks <= 0)
					{
						ammoPool.RemainingTicks = ammoPool.Info.ReloadDelay;
						if (!string.IsNullOrEmpty(ammoPool.Info.RearmSound))
							Game.Sound.PlayToPlayer(SoundType.World, self.Owner, ammoPool.Info.RearmSound, self.CenterPosition);

						ammoPool.GiveAmmo(self, ammoPool.Info.ReloadCount);
					}

					return false;
				}
			}

			return true;
		}
	}
}
