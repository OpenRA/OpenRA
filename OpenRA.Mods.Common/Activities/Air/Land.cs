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
		readonly bool requireSpace;
		readonly Actor ignoreActor;
		readonly WDist landAltitude;
		readonly bool ignoreTarget;

		bool landingInitiated;
		bool soundPlayed;

		public Land(Actor self, Target t, bool requireSpace, WDist landAltitude, Actor ignoreActor = null)
		{
			target = t;
			aircraft = self.Trait<Aircraft>();
			this.requireSpace = requireSpace;
			this.ignoreActor = ignoreActor;
			this.landAltitude = landAltitude != WDist.Zero ? landAltitude : aircraft.Info.LandAltitude;
		}

		public Land(Actor self, Target t, bool requireSpace, Actor ignoreActor = null)
			: this(self, t, requireSpace, WDist.Zero, ignoreActor) { }

		public Land(Actor self, bool requireSpace, WDist landAltitude, Actor ignoreActor = null)
			: this(self, Target.FromPos(self.CenterPosition), requireSpace, landAltitude, ignoreActor)
		{
			ignoreTarget = true;
		}

		public Land(Actor self, bool requireSpace, Actor ignoreActor = null)
			: this(self, requireSpace, WDist.Zero, ignoreActor) { }

		public override Activity Tick(Actor self)
		{
			if (ChildActivity != null)
			{
				ChildActivity = ActivityUtils.RunActivity(self, ChildActivity);
				if (ChildActivity != null)
					return this;
			}

			if (!ignoreTarget && !target.IsValidFor(self))
			{
				Cancel(self);
				return NextActivity;
			}

			if (IsCanceling)
			{
				aircraft.RemoveInfluence();
				return NextActivity;
			}

			if (requireSpace && !landingInitiated)
			{
				var landingCell = !aircraft.Info.VTOL ? self.World.Map.CellContaining(target.CenterPosition) : self.Location;
				if (!aircraft.CanLand(landingCell, ignoreActor))
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
			if (aircraft.Info.VTOL)
			{
				var landAlt = !ignoreTarget ? self.World.Map.DistanceAboveTerrain(target.CenterPosition) : landAltitude;
				if (!soundPlayed && aircraft.Info.LandingSounds.Length > 0 && altitude != landAlt)
					PlayLandingSound(self);

				if (HeliFly.AdjustAltitude(self, aircraft, landAlt))
					return this;

				return NextActivity;
			}

			if (!soundPlayed && aircraft.Info.LandingSounds.Length > 0 && altitude != landAltitude)
				PlayLandingSound(self);

			var d = target.CenterPosition - self.CenterPosition;

			// The next move would overshoot, so just set the final position
			var move = aircraft.FlyStep(aircraft.Facing);
			if (d.HorizontalLengthSquared < move.HorizontalLengthSquared)
			{
				aircraft.SetPosition(self, target.CenterPosition);
				return NextActivity;
			}

			var landingAlt = self.World.Map.DistanceAboveTerrain(target.CenterPosition);
			Fly.FlyToward(self, aircraft, d.Yaw.Facing, landingAlt);

			return this;
		}

		void PlayLandingSound(Actor self)
		{
			Game.Sound.Play(SoundType.World, aircraft.Info.LandingSounds, self.World, aircraft.CenterPosition);
			soundPlayed = true;
		}
	}
}
