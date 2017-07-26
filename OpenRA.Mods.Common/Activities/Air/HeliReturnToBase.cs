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
using System.Drawing;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class HeliReturnToBase : Activity, IDockActivity
	{
		readonly Aircraft heli;
		readonly bool alwaysLand;
		readonly bool abortOnResupply;
		Actor dest;

		public HeliReturnToBase(Actor self, bool abortOnResupply, Actor dest = null, bool alwaysLand = true)
		{
			heli = self.Trait<Aircraft>();
			this.alwaysLand = alwaysLand;
			this.abortOnResupply = abortOnResupply;
			this.dest = dest;
		}

		IEnumerable<Actor> GetHelipads(Actor self)
		{
			return self.World.ActorsHavingTrait<DockManager>().Where(a =>
				a.Owner == self.Owner &&
				heli.Info.RearmBuildings.Contains(a.Info.Name) &&
				!a.IsDead &&
				!a.Disposed);
		}

		IEnumerable<Actor> GetDockableHelipads(Actor self)
		{
			foreach (var pad in GetHelipads(self))
			{
				var dockManager = pad.Trait<DockManager>();
				if (dockManager.HasFreeServiceDock(self))
					yield return pad;
			}
		}

		protected override void OnFirstRun(Actor self)
		{
			// Release first, before trying to dock.
			var dc = self.TraitOrDefault<DockClient>();
			if (dc != null)
				dc.Release();
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled)
				return NextActivity;

			// Check status and make dest correct.
			// Priorities:
			// 1. closest reloadable hpad
			// 2. closest hpad
			// 3. null
			if (dest == null || dest.IsDead || dest.Disposed)
			{
				var hpads = GetHelipads(self);
				var dockableHpads = hpads.Where(p => p.Trait<DockManager>().HasFreeServiceDock(self));
				if (dockableHpads.Any())
					dest = dockableHpads.ClosestTo(self);
				else if (hpads.Any())
					dest = hpads.ClosestTo(self);
				else
					dest = null;
			}

			// Owner doesn't have any feasible helipad, in this case.
			if (dest == null)
			{
				// Probably the owner is having a crisis lol.
				// Doesn't matter if the unit just sits there or do what ever NextActivity is.
				return ActivityUtils.SequenceActivities(
					new Turn(self, heli.Info.InitialFacing),
					new HeliLand(self, true),
					NextActivity);
			}

			// Do we need to land and reload/repair?
			if (!ShouldLandAtBuilding(self, dest))
			{
				// Move near the hpad then do next activity.
				return ActivityUtils.SequenceActivities(
					new HeliFly(self, Target.FromActor(dest), new WDist(2048), new WDist(4096)),
					NextActivity);
			}

			// Can't dock :(
			if (!dest.Trait<DockManager>().HasFreeServiceDock(self))
			{
				// If no pad is available, move near one and wait
				var distanceLength = (dest.CenterPosition - self.CenterPosition).HorizontalLength;
				var randomPosition = WVec.FromPDF(self.World.SharedRandom, 2) * distanceLength / 1024;
				var target = Target.FromPos(dest.CenterPosition + randomPosition);

				Queue(ActivityUtils.SequenceActivities(
					new HeliFly(self, target, WDist.Zero, heli.Info.WaitDistanceFromResupplyBase),
					new Wait(29),
					new HeliReturnToBase(self, abortOnResupply, null, alwaysLand)));
				return NextActivity;
			}

			// Do the docking.
			dest.Trait<DockManager>().ReserveDock(dest, self, this);
			return NextActivity;
		}

		bool ShouldLandAtBuilding(Actor self, Actor dest)
		{
			if (alwaysLand)
				return true;

			if (heli.Info.RepairBuildings.Contains(dest.Info.Name) && self.GetDamageState() != DamageState.Undamaged)
				return true;

			return heli.Info.RearmBuildings.Contains(dest.Info.Name) && self.TraitsImplementing<AmmoPool>()
					.Any(p => !p.Info.SelfReloads && !p.FullAmmo());
		}

		Activity IDockActivity.ApproachDockActivities(Actor host, Actor client, Dock dock)
		{
			return ActivityUtils.SequenceActivities(
				new HeliFly(client, Target.FromPos(dock.CenterPosition)),
				new Turn(client, dock.Info.DockAngle),
				new HeliLand(client, false));
		}

		Activity IDockActivity.DockActivities(Actor host, Actor client, Dock dock)
		{
			client.SetTargetLine(Target.FromCell(client.World, dock.Location), Color.Green, false);

			// Let's reload. The assumption here is that for aircrafts, there are no waiting docks.
			return new ResupplyAircraft(client);
		}

		Activity IDockActivity.ActivitiesAfterDockDone(Actor host, Actor client, Dock dock)
		{
			var rp = host.Trait<RallyPoint>();

			// Take off and move to RP.
			// I know this depreciates AbortOnResupply activity but it is a bug to reuse NextActivity!
			client.SetTargetLine(Target.FromCell(client.World, rp.Location), Color.Green, false);
			return client.Trait<IMove>().MoveTo(rp.Location, 2);

			// Old code:
			// client.Info.TraitInfo<AircraftInfo>().AbortOnResupply ? null : client.CurrentActivity.NextActivity));
		}

		Activity IDockActivity.ActivitiesOnDockFail(Actor client)
		{
			// Stay idle
			return null;
		}
	}
}
