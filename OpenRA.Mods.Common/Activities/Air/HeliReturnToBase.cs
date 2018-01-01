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

using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class HeliReturnToBase : Activity
	{
		readonly Aircraft aircraft;
		readonly Repairable[] repairables;
		readonly Rearmable[] rearmables;
		readonly bool alwaysLand;
		readonly bool abortOnResupply;
		Actor dest;

		public HeliReturnToBase(Actor self, bool abortOnResupply, Actor dest = null, bool alwaysLand = true)
		{
			aircraft = self.Trait<Aircraft>();
			repairables = self.TraitsImplementing<Repairable>().ToArray();
			rearmables = self.TraitsImplementing<Rearmable>().ToArray();
			this.alwaysLand = alwaysLand;
			this.abortOnResupply = abortOnResupply;
			this.dest = dest;
		}

		public Actor ChooseResupplier(Actor self, bool unreservedOnly)
		{
			var rearms = rearmables.Where(r => !r.IsTraitDisabled);
			if (!rearms.Any())
				return null;

			return self.World.Actors.Where(a => a.Owner == self.Owner
				&& rearms.Any(r => r.Info.RearmActors.Contains(a.Info.Name))
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

				if (nearestResupplier == null)
					return ActivityUtils.SequenceActivities(new Turn(self, initialFacing), new HeliLand(self, true), NextActivity);
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

			if (repairables.Any(r => !r.IsTraitDisabled && r.Info.RepairActors.Contains(dest.Info.Name)) && self.GetDamageState() != DamageState.Undamaged)
				return true;

			return rearmables.Any(r => !r.IsTraitDisabled && r.Info.RearmActors.Contains(dest.Info.Name))
				&& self.TraitsImplementing<AmmoPool>().Any(p => !p.AutoReloads && !p.FullAmmo());
		}
	}
}
