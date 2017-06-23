#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
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
		[Desc("Name of this ammo pool, used to link armaments and reload traits to this pool.")]
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

		// HACK: Temporarily kept until Rearm activity is gone for good
		[Desc("Time to reload per ReloadCount on airfield etc.")]
		public readonly int ReloadDelay = 50;

		public object Create(ActorInitializer init) { return new AmmoPool(init.Self, this); }
	}

	public class AmmoPool : INotifyCreated, INotifyAttack, IPips, ISync
	{
		public readonly AmmoPoolInfo Info;
		ConditionManager conditionManager;
		int emptyToken = ConditionManager.InvalidConditionToken;

		bool selfReloads;

		// HACK: Temporarily needed until Rearm activity is gone for good
		[Sync] public int RemainingTicks;
		[Sync] int currentAmmo;

		public AmmoPool(Actor self, AmmoPoolInfo info)
		{
			Info = info;
			if (Info.InitialAmmo < Info.Ammo && Info.InitialAmmo >= 0)
				currentAmmo = Info.InitialAmmo;
			else
				currentAmmo = Info.Ammo;
		}

		public int GetAmmoCount() { return currentAmmo; }
		public bool FullAmmo() { return currentAmmo == Info.Ammo; }
		public bool HasAmmo() { return currentAmmo > 0; }

		public bool GiveAmmo()
		{
			if (currentAmmo >= Info.Ammo)
				return false;

			++currentAmmo;
			return true;
		}

		public bool TakeAmmo()
		{
			if (currentAmmo <= 0)
				return false;

			--currentAmmo;
			return true;
		}

		// This mostly serves to avoid complicated ReloadAmmoPool look-ups in various other places.
		// TODO: Investigate removing this when the Rearm activity is replaced with a condition-based solution.
		public bool SelfReloads { get { return selfReloads; } }

		void INotifyCreated.Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();
			selfReloads = self.TraitsImplementing<ReloadAmmoPool>().Any(r => r.Info.AmmoPool == Info.Name && r.Info.RequiresCondition == null);

			// HACK: Temporarily needed until Rearm activity is gone for good
			RemainingTicks = Info.ReloadDelay;
		}

		void INotifyAttack.Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			if (a != null && a.Info.AmmoPoolName == Info.Name)
				TakeAmmo();
		}

		void INotifyAttack.PreparingAttack(Actor self, Target target, Armament a, Barrel barrel) { }

		public IEnumerable<PipType> GetPips(Actor self)
		{
			var pips = Info.PipCount >= 0 ? Info.PipCount : Info.Ammo;

			return Enumerable.Range(0, pips).Select(i =>
				(currentAmmo * pips) / Info.Ammo > i ?
				Info.PipType : Info.PipTypeEmpty);
		}
	}
}
