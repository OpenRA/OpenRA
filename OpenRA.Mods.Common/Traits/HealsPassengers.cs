#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Heals all viable passengers simultaneously then optionally unloads fully-healed actors.",
		"Example use: Hospital structure.")]
	public class HealsPassengersInfo : ITraitInfo, Requires<CargoInfo>
	{
		[Desc("Ticks in-between healing actors.")]
		public readonly int HealFrequency = 25;

		[Desc("Amount to heal passengers on each iteration.")]
		public readonly int HealAmount = 10;

		[Desc("Should this actor automatically unload passengers that have been fully healed?")]
		public readonly bool UnloadUndamagedPassengers = true;

		[Desc("Allow undamages actors to enter?",
			"These actors will not be affected in any way by the healing effects of this trait.")]
		public readonly bool AllowUndamagedPassengers = false;

		public object Create(ActorInitializer init) { return new HealsPassengers(init.Self, this); }
	}

	public class HealsPassengers : ITick, IPreventsCargoLoading
	{
		readonly HealsPassengersInfo info;
		readonly Cargo cargo;

		IEnumerable<Actor> damagedPassengers;
		int countdownTicks;

		public HealsPassengers(Actor self, HealsPassengersInfo info)
		{
			this.info = info;
			cargo = self.Trait<Cargo>();
			countdownTicks = info.HealFrequency;
		}

		public bool CanLoadPassenger(Actor self, Actor toLoad)
		{
			return info.AllowUndamagedPassengers || toLoad.IsDamaged();
		}

		public void Tick(Actor self)
		{
			if (!cargo.Passengers.Any())
				return;

			damagedPassengers = cargo.Passengers.Where(HealthExts.IsDamaged);
			if (!damagedPassengers.Any())
				return;

			if (--countdownTicks > 0)
				return;

			countdownTicks = info.HealFrequency;

			foreach (var toHeal in damagedPassengers)
			{
				var health = toHeal.Trait<Health>();
				health.InflictDamage(toHeal, self, -info.HealAmount, null, true);

				if (info.UnloadUndamagedPassengers && health.HP >= health.MaxHP)
					self.QueueActivity(new UnloadCargo(self, toHeal));
			}
		}
	}
}