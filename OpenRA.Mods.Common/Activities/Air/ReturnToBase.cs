#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class ReturnToBase : Activity
	{
		readonly Aircraft aircraft;
		readonly RepairableInfo repairableInfo;
		readonly Rearmable rearmable;
		readonly bool alwaysLand;
		readonly bool abortOnResupply;
		bool resupplied;
		Actor dest;
		int facing = -1;

		public ReturnToBase(Actor self, bool abortOnResupply, Actor dest = null, bool alwaysLand = true)
		{
			this.dest = dest;
			this.alwaysLand = alwaysLand;
			this.abortOnResupply = abortOnResupply;
			aircraft = self.Trait<Aircraft>();
			repairableInfo = self.Info.TraitInfoOrDefault<RepairableInfo>();
			rearmable = self.TraitOrDefault<Rearmable>();
		}

		public static Actor ChooseResupplier(Actor self, bool unreservedOnly)
		{
			var rearmInfo = self.Info.TraitInfoOrDefault<RearmableInfo>();
			if (rearmInfo == null)
				return null;

			return self.World.ActorsHavingTrait<Reservable>()
				.Where(a => !a.IsDead
					&& a.Owner == self.Owner
					&& rearmInfo.RearmActors.Contains(a.Info.Name)
					&& (!unreservedOnly || Reservable.IsAvailableFor(a, self)))
				.ClosestTo(self);
		}

		bool ShouldLandAtBuilding(Actor self, Actor dest)
		{
			if (alwaysLand)
				return true;

			if (repairableInfo != null && repairableInfo.RepairActors.Contains(dest.Info.Name) && self.GetDamageState() != DamageState.Undamaged)
				return true;

			return rearmable != null && rearmable.Info.RearmActors.Contains(dest.Info.Name)
					&& rearmable.RearmableAmmoPools.Any(p => !p.FullAmmo());
		}

		public override Activity Tick(Actor self)
		{
			if (ChildActivity != null)
			{
				ChildActivity = ActivityUtils.RunActivity(self, ChildActivity);
				if (ChildActivity != null)
					return this;
			}

			// Refuse to take off if it would land immediately again.
			// Special case: Don't kill other deploy hotkey activities.
			if (aircraft.ForceLanding)
				return NextActivity;

			// If a Cancel was triggered at this point, it's unlikely that previously queued child activities finished,
			// so 'resupplied' needs to be set to false, else it + abortOnResupply might cause another Cancel
			// that would cancel any other activities that were queued after the first Cancel was triggered.
			// TODO: This is a mess, we need to somehow make the activity cancelling a bit less tricky.
			if (resupplied && IsCanceling)
				resupplied = false;

			if (resupplied && abortOnResupply)
				self.CancelActivity();

			if (resupplied || IsCanceling || self.IsDead)
				return NextActivity;

			if (dest == null || dest.IsDead || !Reservable.IsAvailableFor(dest, self))
				dest = ChooseResupplier(self, true);

			if (dest == null)
			{
				var nearestResupplier = ChooseResupplier(self, false);

				if (nearestResupplier != null)
				{
					if (aircraft.Info.CanHover)
					{
						var distanceFromResupplier = (nearestResupplier.CenterPosition - self.CenterPosition).HorizontalLength;
						var distanceLength = aircraft.Info.WaitDistanceFromResupplyBase.Length;

						// If no pad is available, move near one and wait
						if (distanceFromResupplier > distanceLength)
						{
							var randomPosition = WVec.FromPDF(self.World.SharedRandom, 2) * distanceLength / 1024;
							var target = Target.FromPos(nearestResupplier.CenterPosition + randomPosition);

							QueueChild(self, new Fly(self, target, WDist.Zero, aircraft.Info.WaitDistanceFromResupplyBase, targetLineColor: Color.Green), true);
						}

						return this;
					}

					QueueChild(self, new Fly(self, Target.FromActor(nearestResupplier), WDist.Zero, aircraft.Info.WaitDistanceFromResupplyBase, targetLineColor: Color.Green),
							true);
					QueueChild(self, new FlyCircle(self, aircraft.Info.NumberOfTicksToVerifyAvailableAirport), true);
					return this;
				}

				// Prevent an infinite loop in case we'd return to the activity that called ReturnToBase in the first place. Go idle instead.
				self.CancelActivity();
				return NextActivity;
			}

			if (ShouldLandAtBuilding(self, dest))
			{
				var exit = dest.FirstExitOrDefault(null);
				var offset = exit != null ? exit.Info.SpawnOffset : WVec.Zero;
				if (aircraft.Info.TurnToDock)
					facing = aircraft.Info.InitialFacing;
				if (!aircraft.Info.VTOL)
					facing = 192;

				aircraft.MakeReservation(dest);
				QueueChild(self, new Land(self, Target.FromActor(dest), offset, facing), true);
				QueueChild(self, new Resupply(self, dest, WDist.Zero), true);
				if (aircraft.Info.TakeOffOnResupply && !alwaysLand)
					QueueChild(self, new TakeOff(self));
			}
			else
				QueueChild(self, new Fly(self, Target.FromActor(dest)), true);

			resupplied = true;
			return this;
		}
	}
}
