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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class HeliReturnToBase : Activity
	{
		readonly Aircraft aircraft;
		readonly RepairableInfo repairableInfo;
		readonly Rearmable rearmable;
		readonly bool alwaysLand;
		readonly bool abortOnResupply;
		Actor dest;

		public HeliReturnToBase(Actor self, bool abortOnResupply, Actor dest = null, bool alwaysLand = true)
		{
			aircraft = self.Trait<Aircraft>();
			repairableInfo = self.Info.TraitInfoOrDefault<RepairableInfo>();
			rearmable = self.TraitOrDefault<Rearmable>();
			this.alwaysLand = alwaysLand;
			this.abortOnResupply = abortOnResupply;
			this.dest = dest;
		}

		public Actor ChooseResupplier(Actor self, bool unreservedOnly)
		{
			if (rearmable == null)
				return null;

			return self.World.Actors.Where(a => a.Owner == self.Owner
				&& rearmable.Info.RearmActors.Contains(a.Info.Name)
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

			var landingProcedures = new List<Activity>();
			var exit = dest.FirstExitOrDefault(null);
			var offset = exit != null ? exit.Info.SpawnOffset : WVec.Zero;

			landingProcedures.Add(new HeliFly(self, Target.FromPos(dest.CenterPosition + offset)));

			if (ShouldLandAtBuilding(self, dest))
			{
				aircraft.MakeReservation(dest);

				if (aircraft.Info.TurnToDock)
					landingProcedures.Add(new Turn(self, initialFacing));

				landingProcedures.Add(new HeliLand(self, false));
				landingProcedures.Add(new ResupplyAircraft(self));
				if (!abortOnResupply)
					landingProcedures.Add(NextActivity);
			}
			else
				landingProcedures.Add(NextActivity);

			return ActivityUtils.SequenceActivities(landingProcedures.ToArray());
		}

		bool ShouldLandAtBuilding(Actor self, Actor dest)
		{
			if (alwaysLand)
				return true;

			if (repairableInfo != null && repairableInfo.RepairBuildings.Contains(dest.Info.Name) && self.GetDamageState() != DamageState.Undamaged)
				return true;

			return rearmable != null && rearmable.Info.RearmActors.Contains(dest.Info.Name)
					&& rearmable.RearmableAmmoPools.Any(p => !p.FullAmmo());
		}
	}
}
