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
		[Desc("Name of this ammo pool, used to link reload traits to this pool.")]
		public readonly string Name = "primary";

		[Desc("Name(s) of armament(s) that use this pool.")]
		public readonly string[] Armaments = { "primary", "secondary" };

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

		[GrantedConditionReference]
		[Desc("The condition to grant to self for each ammo point in this pool.")]
		public readonly string AmmoCondition = null;

		public object Create(ActorInitializer init) { return new AmmoPool(init.Self, this); }
	}

	public class AmmoPool : INotifyCreated, INotifyAttack, IPips, ISync
	{
		public readonly AmmoPoolInfo Info;
		readonly Stack<int> tokens = new Stack<int>();
		ConditionManager conditionManager;

		// HACK: Temporarily needed until Rearm activity is gone for good
		[Sync] public int RemainingTicks;

		[Sync] int currentAmmo;

		public AmmoPool(Actor self, AmmoPoolInfo info)
		{
			Info = info;
			currentAmmo = Info.InitialAmmo < Info.Ammo && Info.InitialAmmo >= 0 ? Info.InitialAmmo : Info.Ammo;
		}

		public int GetAmmoCount() { return currentAmmo; }
		public bool FullAmmo() { return currentAmmo == Info.Ammo; }
		public bool HasAmmo() { return currentAmmo > 0; }

		public bool GiveAmmo(Actor self, int count)
		{
			if (currentAmmo >= Info.Ammo || count < 0)
				return false;

			currentAmmo = (currentAmmo + count).Clamp(0, Info.Ammo);
			UpdateCondition(self);
			return true;
		}

		public bool TakeAmmo(Actor self, int count)
		{
			if (currentAmmo <= 0 || count < 0)
				return false;

			currentAmmo = (currentAmmo - count).Clamp(0, Info.Ammo);
			UpdateCondition(self);
			return true;
		}

		// This mostly serves to avoid complicated ReloadAmmoPool look-ups in various other places.
		// TODO: Investigate removing this when the Rearm activity is replaced with a condition-based solution.
		public bool AutoReloads { get; private set; }

		void INotifyCreated.Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();
			AutoReloads = self.TraitsImplementing<ReloadAmmoPool>().Any(r => r.Info.AmmoPool == Info.Name && r.Info.RequiresCondition == null);

			UpdateCondition(self);

			// HACK: Temporarily needed until Rearm activity is gone for good
			RemainingTicks = Info.ReloadDelay;
		}

		void INotifyAttack.Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			if (a != null && Info.Armaments.Contains(a.Info.Name))
				TakeAmmo(self, 1);
		}

		void INotifyAttack.PreparingAttack(Actor self, Target target, Armament a, Barrel barrel) { }

		void UpdateCondition(Actor self)
		{
			if (conditionManager == null || string.IsNullOrEmpty(Info.AmmoCondition))
				return;

			while (currentAmmo > tokens.Count && tokens.Count < Info.Ammo)
				tokens.Push(conditionManager.GrantCondition(self, Info.AmmoCondition));

			while (currentAmmo < tokens.Count && tokens.Count > 0)
				conditionManager.RevokeCondition(self, tokens.Pop());
		}

		public IEnumerable<PipType> GetPips(Actor self)
		{
			var pips = Info.PipCount >= 0 ? Info.PipCount : Info.Ammo;

			return Enumerable.Range(0, pips).Select(i =>
				(currentAmmo * pips) / Info.Ammo > i ?
				Info.PipType : Info.PipTypeEmpty);
		}
	}
}
