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
	[Desc("Actor has a limited amount of ammo, after using it all the actor must reload in some way.")]
	public class AmmoPoolInfo : TraitInfo
	{
		[Desc("Name of this ammo pool, used to link reload traits to this pool.")]
		public readonly string Name = "primary";

		[Desc("Name(s) of armament(s) that use this pool.")]
		public readonly string[] Armaments = { "primary", "secondary" };

		[Desc("How much ammo does this pool contain when fully loaded.")]
		public readonly int Ammo = 1;

		[Desc("Initial ammo the actor is created with. Defaults to Ammo.")]
		public readonly int InitialAmmo = -1;

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

		public override object Create(ActorInitializer init) { return new AmmoPool(this); }
	}

	public class AmmoPool : INotifyCreated, INotifyAttack, ISync
	{
		public readonly AmmoPoolInfo Info;
		readonly Stack<int> tokens = new Stack<int>();

		// HACK: Temporarily needed until Rearm activity is gone for good
		[Sync]
		public int RemainingTicks;

		[Sync]
		public int CurrentAmmoCount { get; private set; }

		public bool HasAmmo => CurrentAmmoCount > 0;
		public bool HasFullAmmo => CurrentAmmoCount == Info.Ammo;

		public AmmoPool(AmmoPoolInfo info)
		{
			Info = info;
			CurrentAmmoCount = Info.InitialAmmo < Info.Ammo && Info.InitialAmmo >= 0 ? Info.InitialAmmo : Info.Ammo;
		}

		public bool GiveAmmo(Actor self, int count)
		{
			if (CurrentAmmoCount >= Info.Ammo || count < 0)
				return false;

			CurrentAmmoCount = (CurrentAmmoCount + count).Clamp(0, Info.Ammo);
			UpdateCondition(self);
			return true;
		}

		public bool TakeAmmo(Actor self, int count)
		{
			if (CurrentAmmoCount <= 0 || count < 0)
				return false;

			CurrentAmmoCount = (CurrentAmmoCount - count).Clamp(0, Info.Ammo);
			UpdateCondition(self);
			return true;
		}

		void INotifyCreated.Created(Actor self)
		{
			UpdateCondition(self);

			// HACK: Temporarily needed until Rearm activity is gone for good
			RemainingTicks = Info.ReloadDelay;
		}

		void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel)
		{
			if (a != null && Info.Armaments.Contains(a.Info.Name))
				TakeAmmo(self, a.Info.AmmoUsage);
		}

		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel) { }

		void UpdateCondition(Actor self)
		{
			if (string.IsNullOrEmpty(Info.AmmoCondition))
				return;

			while (CurrentAmmoCount > tokens.Count && tokens.Count < Info.Ammo)
				tokens.Push(self.GrantCondition(Info.AmmoCondition));

			while (CurrentAmmoCount < tokens.Count && tokens.Count > 0)
				self.RevokeCondition(tokens.Pop());
		}
	}
}
