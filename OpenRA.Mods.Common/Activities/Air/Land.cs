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

using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class Land : Activity
	{
		readonly Target target;
		readonly Aircraft aircraft;
		readonly WVec offset;

		bool landingInitiated;
		bool soundPlayed;

		public Land(Actor self, Target t, WVec offset)
		{
			target = t;
			aircraft = self.Trait<Aircraft>();
			this.offset = offset;
		}

		public Land(Actor self, Target t)
			: this(self, t, WVec.Zero) { }

		public Land(Actor self)
			: this(self, Target.FromPos(Aircraft.GroundPosition(self)), WVec.Zero) { }

		public override Activity Tick(Actor self)
		{
			if (ChildActivity != null)
			{
				ChildActivity = ActivityUtils.RunActivity(self, ChildActivity);
				if (ChildActivity != null)
					return this;
			}

			if (IsCanceling || target.Type == TargetType.Invalid)
			{
				// We must return the actor to a sensible height before continuing.
				// If the aircraft lands when idle and is idle, continue landing,
				// otherwise climb back to CruiseAltitude.
				// TODO: Remove this after fixing all activities to work properly with arbitrary starting altitudes.
				var continueLanding = aircraft.Info.LandWhenIdle && self.CurrentActivity.IsCanceling && self.CurrentActivity.NextActivity == null;
				if (!continueLanding)
				{
					var dat = self.World.Map.DistanceAboveTerrain(aircraft.CenterPosition);
					if (dat > aircraft.LandAltitude && dat < aircraft.Info.CruiseAltitude)
					{
						QueueChild(self, new TakeOff(self), true);
						return this;
					}

					aircraft.RemoveInfluence();
					return NextActivity;
				}
			}

			if (!landingInitiated)
			{
				var landingCell = !aircraft.Info.VTOL ? self.World.Map.CellContaining(target.CenterPosition + offset) : self.Location;
				if (!aircraft.CanLand(landingCell, target.Actor))
				{
					// Maintain holding pattern.
					if (!aircraft.Info.CanHover)
						QueueChild(self, new FlyCircle(self, 25), true);

					self.NotifyBlocker(landingCell);
					return this;
				}

				aircraft.AddInfluence(landingCell);
				aircraft.EnteringCell(self);
				landingInitiated = true;
			}

			var altitude = self.World.Map.DistanceAboveTerrain(self.CenterPosition);
			var landAltitude = self.World.Map.DistanceAboveTerrain(target.CenterPosition + offset) + aircraft.LandAltitude;

			if (!soundPlayed && aircraft.Info.LandingSounds.Length > 0 && altitude != landAltitude)
			{
				Game.Sound.Play(SoundType.World, aircraft.Info.LandingSounds, self.World, aircraft.CenterPosition);
				soundPlayed = true;
			}

			// For VTOLs we assume we've already arrived at the target location and just need to move downward
			if (aircraft.Info.VTOL)
			{
				if (Fly.VerticalTakeOffOrLandTick(self, aircraft, aircraft.Facing, landAltitude))
					return this;

				return NextActivity;
			}

			var d = (target.CenterPosition + offset) - self.CenterPosition;

			// The next move would overshoot, so just set the final position
			var move = aircraft.FlyStep(aircraft.Facing);
			if (d.HorizontalLengthSquared < move.HorizontalLengthSquared)
			{
				var landingAltVec = new WVec(WDist.Zero, WDist.Zero, aircraft.LandAltitude);
				aircraft.SetPosition(self, target.CenterPosition + offset + landingAltVec);
				return NextActivity;
			}

			var landingAlt = self.World.Map.DistanceAboveTerrain(target.CenterPosition + offset) + aircraft.LandAltitude;
			Fly.FlyTick(self, aircraft, d.Yaw.Facing, landingAlt);

			return this;
		}
	}
}
