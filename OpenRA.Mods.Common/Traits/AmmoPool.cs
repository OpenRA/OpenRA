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
	[Desc("Actor has a limited amount of ammo, after using it all the actor must reload in some way.")]
	public class AmmoPoolInfo : ITraitInfo
	{
		[Desc("Name of this ammo pool, used to link armaments to this pool.")]
		public readonly string Name = "primary";

		[Desc("How much ammo does this pool contain when fully loaded.")]
		public readonly int Ammo = 1;

		[Desc("Initial ammo the actor is created with. Defaults to Ammo.")]
		public readonly int InitialAmmo = -1;

		[Desc("Defaults to value in Ammo. 0 means no visible pips.")]
		public readonly int PipCount = -1;

		[Desc("PipType to use for loaded ammo.")]
		public readonly PipType PipType = PipType.Green;

		[Desc("PipType to use for empty ammo.")]
		public readonly PipType PipTypeEmpty = PipType.Transparent;

		[Desc("How much ammo is reloaded after a certain period.")]
		public readonly int ReloadCount = 1;

		[Desc("Sound to play for each reloaded ammo magazine.")]
		public readonly string RearmSound = null;

		[Desc("Time to reload per ReloadCount on airfield etc.")]
		public readonly int ReloadDelay = 50;

		[Desc("Whether or not ammo is replenished on its own.")]
		public readonly bool SelfReloads = false;

		[Desc("Time to reload per ReloadCount when actor 'SelfReloads'.")]
		public readonly int SelfReloadDelay = 50;

		[Desc("Whether or not reload timer should be reset when ammo has been fired.")]
		public readonly bool ResetOnFire = false;

		public object Create(ActorInitializer init) { return new AmmoPool(init.Self, this); }
	}

	public class AmmoPool : INotifyAttack, IPips, ITick, ISync
	{
		public readonly AmmoPoolInfo Info;
		[Sync] public int CurrentAmmo;
		[Sync] public int RemainingTicks;
		public int PreviousAmmo;

		public AmmoPool(Actor self, AmmoPoolInfo info)
		{
			Info = info;
			if (Info.InitialAmmo < Info.Ammo && Info.InitialAmmo >= 0)
				CurrentAmmo = Info.InitialAmmo;
			else
				CurrentAmmo = Info.Ammo;

			RemainingTicks = Info.SelfReloadDelay;
			PreviousAmmo = GetAmmoCount();
		}

		public int GetAmmoCount() { return CurrentAmmo; }
		public bool FullAmmo() { return CurrentAmmo == Info.Ammo; }
		public bool HasAmmo() { return CurrentAmmo > 0; }

		public bool GiveAmmo()
		{
			if (CurrentAmmo >= Info.Ammo)
				return false;

			++CurrentAmmo;
			return true;
		}

		public bool TakeAmmo()
		{
			if (CurrentAmmo <= 0)
				return false;

			--CurrentAmmo;
			return true;
		}

		void INotifyAttack.Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			if (a != null && a.Info.AmmoPoolName == Info.Name)
				TakeAmmo();
		}

		void INotifyAttack.PreparingAttack(Actor self, Target target, Armament a, Barrel barrel) { }

		public void Tick(Actor self)
		{
			if (!Info.SelfReloads)
				return;

			// Resets the tick counter if ammo was fired.
			if (Info.ResetOnFire && GetAmmoCount() < PreviousAmmo)
			{
				RemainingTicks = Info.SelfReloadDelay;
				PreviousAmmo = GetAmmoCount();
			}

			if (!FullAmmo() && --RemainingTicks == 0)
			{
				RemainingTicks = Info.SelfReloadDelay;

				for (var i = 0; i < Info.ReloadCount; i++)
					GiveAmmo();

				PreviousAmmo = GetAmmoCount();
			}
		}

		public IEnumerable<PipType> GetPips(Actor self)
		{
			var pips = Info.PipCount >= 0 ? Info.PipCount : Info.Ammo;

			return Enumerable.Range(0, pips).Select(i =>
				(CurrentAmmo * pips) / Info.Ammo > i ?
				Info.PipType : Info.PipTypeEmpty);
		}
	}
}
