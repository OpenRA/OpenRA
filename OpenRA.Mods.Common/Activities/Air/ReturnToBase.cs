#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
		Actor dest;
		WAngle? facing;

		public ReturnToBase(Actor self, Actor dest = null, bool alwaysLand = false)
		{
			this.dest = dest;
			this.alwaysLand = alwaysLand;
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
					&& rearmable.RearmableAmmoPools.Any(p => !p.HasFullAmmo);
		}

		public override bool Tick(Actor self)
		{
			// Refuse to take off if it would land immediately again.
			// Special case: Don't kill other deploy hotkey activities.
			if (aircraft.ForceLanding)
				return true;

			if (IsCanceling || self.IsDead)
				return true;

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

							QueueChild(new Fly(self, target, WDist.Zero, aircraft.Info.WaitDistanceFromResupplyBase, targetLineColor: Color.Green));
						}

						return false;
					}

					QueueChild(new Fly(self, Target.FromActor(nearestResupplier), WDist.Zero, aircraft.Info.WaitDistanceFromResupplyBase, targetLineColor: Color.Green));
					QueueChild(new FlyIdle(self, aircraft.Info.NumberOfTicksToVerifyAvailableAirport));
					return false;
				}

				// Prevent an infinite loop in case we'd return to the activity that called ReturnToBase in the first place. Go idle instead.
				self.CancelActivity();
				return true;
			}

			if (ShouldLandAtBuilding(self, dest))
			{
				var exit = dest.NearestExitOrDefault(self.CenterPosition);
				var offset = WVec.Zero;
				if (exit != null)
				{
					offset = exit.Info.SpawnOffset;
					facing = exit.Info.Facing;
				}

				aircraft.MakeReservation(dest);
				QueueChild(new Land(self, Target.FromActor(dest), offset, facing, Color.Green));
				QueueChild(new Resupply(self, dest, WDist.Zero, alwaysLand));
				return true;
			}

			QueueChild(new Fly(self, Target.FromActor(dest), targetLineColor: Color.Green));
			return true;
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			if (ChildActivity == null)
				yield return new TargetLineNode(Target.FromActor(dest), aircraft.Info.TargetLineColor);
			else
				foreach (var n in ChildActivity.TargetLineNodes(self))
					yield return n;
		}
	}
}
