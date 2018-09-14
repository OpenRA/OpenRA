#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class HeliReturnToBase : Activity
	{
		readonly Aircraft aircraft;
		readonly bool alwaysLand;
		readonly bool abortOnResupply;
		Actor dest;

		public HeliReturnToBase(Actor self, bool abortOnResupply, Actor dest = null, bool alwaysLand = true)
		{
			aircraft = self.Trait<Aircraft>();
			this.alwaysLand = alwaysLand;
			this.abortOnResupply = abortOnResupply;
			this.dest = dest;
		}

		public Actor ChooseResupplier(Actor self, bool unreservedOnly)
		{
			var rearmBuildings = aircraft.Info.RearmBuildings;
			return self.World.Actors.Where(a => a.Owner == self.Owner
				&& rearmBuildings.Contains(a.Info.Name)
				&& (!unreservedOnly || !Reservable.IsReserved(a)))
				.ClosestTo(self);
		}

		public override Activity Tick(Actor self)
		{
			// Refuse to take off if it would land immediately again.
			// Special case: Don't kill other deploy hotkey activities.
			if (aircraft.ForceLanding)
				return NextActivity;

			if (IsCanceled)
				return NextActivity;

			if (dest == null || dest.IsDead || Reservable.IsReserved(dest))
				dest = ChooseResupplier(self, true);

			var initialFacing = aircraft.Info.InitialFacing;

			if (dest == null || dest.IsDead)
			{
				var nearestResupplier = ChooseResupplier(self, false);

				// If a heli was told to return and there's no (available) RearmBuilding, going to the probable next queued activity (HeliAttack)
				// would be pointless (due to lack of ammo), and possibly even lead to an infinite loop due to HeliAttack.cs:L79.
				if (nearestResupplier == null && aircraft.Info.LandWhenIdle)
				{
					if (aircraft.Info.TurnToLand)
						return ActivityUtils.SequenceActivities(new Turn(self, initialFacing), new HeliLand(self, true));

					return new HeliLand(self, true);
				}
				else if (nearestResupplier == null && !aircraft.Info.LandWhenIdle)
					return null;
				else
				{
					var distanceFromResupplier = (nearestResupplier.CenterPosition - self.CenterPosition).HorizontalLength;
					var distanceLength = aircraft.Info.WaitDistanceFromResupplyBase.Length;

					// If no pad is available, move near one and wait
					if (distanceFromResupplier > distanceLength)
					{
						var randomPosition = WVec.FromPDF(self.World.SharedRandom, 2) * distanceLength / 1024;

						var target = Target.FromPos(nearestResupplier.CenterPosition + randomPosition);

						return ActivityUtils.SequenceActivities(new HeliFly(self, target, WDist.Zero, aircraft.Info.WaitDistanceFromResupplyBase), this);
					}

					return this;
				}
			}

			var exit = dest.Info.FirstExitOrDefault(null);
			var offset = (exit != null) ? exit.SpawnOffset : WVec.Zero;

			if (ShouldLandAtBuilding(self, dest))
			{
				aircraft.MakeReservation(dest);

				return ActivityUtils.SequenceActivities(
					new HeliFly(self, Target.FromPos(dest.CenterPosition + offset)),
					new Turn(self, initialFacing),
					new HeliLand(self, false),
					new ResupplyAircraft(self),
					!abortOnResupply ? NextActivity : null);
			}

			return ActivityUtils.SequenceActivities(
				new HeliFly(self, Target.FromPos(dest.CenterPosition + offset)),
				NextActivity);
		}

		bool ShouldLandAtBuilding(Actor self, Actor dest)
		{
			if (alwaysLand)
				return true;

			if (aircraft.Info.RepairBuildings.Contains(dest.Info.Name) && self.GetDamageState() != DamageState.Undamaged)
				return true;

			return aircraft.Info.RearmBuildings.Contains(dest.Info.Name) && self.TraitsImplementing<AmmoPool>()
					.Any(p => !p.AutoReloads && !p.FullAmmo());
		}
	}
}
