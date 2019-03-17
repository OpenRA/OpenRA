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

		bool landingInitiated;
		bool soundPlayed;

		public Land(Actor self, Target t, bool requireSpace, Actor ignoreActor = null)
		{
			target = t;
			aircraft = self.Trait<Aircraft>();
			this.requireSpace = requireSpace;
			this.ignoreActor = ignoreActor;
		}

		public override Activity Tick(Actor self)
		{
			if (ChildActivity != null)
			{
				ChildActivity = ActivityUtils.RunActivity(self, ChildActivity);
				if (ChildActivity != null)
					return this;
			}

			if (!target.IsValidFor(self))
				return NextActivity;

			if (IsCanceling)
			{
				aircraft.RemoveInfluence();
				return NextActivity;
			}

			if (requireSpace && !landingInitiated)
			{
				var landingCell = self.World.Map.CellContaining(target.CenterPosition);
				if (!aircraft.CanLand(landingCell, ignoreActor))
				{
					// Maintain holding pattern.
					QueueChild(self, new FlyCircle(self, 25), true);
					self.NotifyBlocker(landingCell);
					return this;
				}

				aircraft.AddInfluence(landingCell);
				aircraft.EnteringCell(self);
				landingInitiated = true;
			}

			if (!soundPlayed && aircraft.Info.LandingSounds.Length > 0 && !self.IsAtGroundLevel())
			{
				Game.Sound.Play(SoundType.World, aircraft.Info.LandingSounds, self.World, aircraft.CenterPosition);
				soundPlayed = true;
			}

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
	}
}
