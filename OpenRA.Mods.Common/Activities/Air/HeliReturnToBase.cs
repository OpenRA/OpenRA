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

using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class HeliReturnToBase : Activity
	{
		readonly Aircraft heli;
		readonly bool alwaysLand;
		readonly bool idleOnPad;
		readonly bool abortOnResupply;

		public HeliReturnToBase(Actor self, bool abortOnResupply, bool alwaysLand = true, bool idleOnPad = false)
		{
			heli = self.Trait<Aircraft>();
			this.alwaysLand = alwaysLand;
			this.idleOnPad = idleOnPad;
			this.abortOnResupply = abortOnResupply;
		}

		public Actor ChooseHelipad(Actor self)
		{
			var rearmBuildings = heli.Info.RearmBuildings;
			return self.World.Actors.Where(a => a.Owner == self.Owner).FirstOrDefault(
				a => rearmBuildings.Contains(a.Info.Name) && !Reservable.IsReserved(a));
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled)
				return NextActivity;

			var dest = ChooseHelipad(self);
			var initialFacing = heli.Info.InitialFacing;

			if (dest == null)
			{
				var rearmBuildings = heli.Info.RearmBuildings;
				var nearestHpad = self.World.ActorsHavingTrait<Reservable>()
					.Where(a => a.Owner == self.Owner && rearmBuildings.Contains(a.Info.Name))
					.ClosestTo(self);

				if (nearestHpad == null)
					return ActivityUtils.SequenceActivities(new Turn(self, initialFacing), new HeliLand(self, true), NextActivity);
				else
				{
					var distanceFromHelipad = (nearestHpad.CenterPosition - self.CenterPosition).HorizontalLength;
					var distanceLength = heli.Info.WaitDistanceFromResupplyBase.Length;

					// If no pad is available, move near one and wait
					if (distanceFromHelipad > distanceLength)
					{
						var randomPosition = WVec.FromPDF(self.World.SharedRandom, 2) * distanceLength / 1024;

						var target = Target.FromPos(nearestHpad.CenterPosition + randomPosition);

						return ActivityUtils.SequenceActivities(new HeliFly(self, target, WDist.Zero, heli.Info.WaitDistanceFromResupplyBase), this);
					}

					return this;
				}
			}

			var exit = dest.Info.TraitInfos<ExitInfo>().FirstOrDefault();
			var offset = (exit != null) ? exit.SpawnOffset : WVec.Zero;

			if (ShouldLandAtBuilding(self, dest))
			{
				heli.MakeReservation(dest);

				return ActivityUtils.SequenceActivities(
					new HeliFly(self, Target.FromPos(dest.CenterPosition + offset)),
					new Turn(self, initialFacing),
					new HeliLand(self, false),
					new ResupplyAircraft(self, idleOnPad),
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

			if (heli.Info.RepairBuildings.Contains(dest.Info.Name) && self.GetDamageState() != DamageState.Undamaged)
				return true;

			// allow mechanics to heal damaged units at their rearming pad when "Return to Base" order is issued
			return heli.Info.RearmBuildings.Contains(dest.Info.Name) && self.TraitsImplementing<AmmoPool>()
				.Any(p => !p.Info.SelfReloads && (!p.FullAmmo() || (idleOnPad && self.GetDamageState() != DamageState.Undamaged)));
		}
	}
}
