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
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class TakeOff : Activity
	{
		readonly Aircraft aircraft;
		readonly IMove move;
		readonly Target target;
		bool moveToRallyPoint;
		bool soundPlayed;

		public TakeOff(Actor self, Target target)
		{
			aircraft = self.Trait<Aircraft>();
			move = self.Trait<IMove>();
			this.target = target;
		}

		public TakeOff(Actor self)
			: this(self, Target.FromCell(self.World, self.Location))
		{
			var host = aircraft.GetActorBelow();
			var hasHost = host != null;
			var rp = hasHost ? host.TraitOrDefault<RallyPoint>() : null;

			var rallyPointDestination = rp != null ? rp.Location :
				(hasHost ? self.World.Map.CellContaining(host.CenterPosition) : self.Location);

			this.target = Target.FromCell(self.World, rallyPointDestination);
			moveToRallyPoint = NextActivity == null && rallyPointDestination != self.Location;
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
			if (aircraft.ForceLanding)
			{
				Cancel(self);
				return NextActivity;
			}

			// We are taking off, so remove reservation and influence in ground cells.
			aircraft.UnReserve();
			aircraft.RemoveInfluence();

			if (!soundPlayed && aircraft.Info.TakeoffSounds.Length > 0)
			{
				Game.Sound.Play(SoundType.World, aircraft.Info.TakeoffSounds, self.World, aircraft.CenterPosition);
				soundPlayed = true;
			}

			var dat = self.World.Map.DistanceAboveTerrain(aircraft.CenterPosition);
			if (dat < aircraft.Info.CruiseAltitude)
			{
				var delta = target.CenterPosition - self.CenterPosition;
				var desiredFacing = delta.HorizontalLengthSquared != 0 ? delta.Yaw.Facing : aircraft.Facing;

				// If we're a VTOL, rise before flying forward
				if (aircraft.Info.VTOL)
				{
					Fly.FlyTick(self, aircraft, desiredFacing, aircraft.Info.CruiseAltitude, -1, MovementType.Vertical | MovementType.Turn);
					return this;
				}
				else
				{
					// Don't turn until we've reached the cruise altitude
					if (!aircraft.Info.CanHover)
						desiredFacing = aircraft.Facing;

					Fly.FlyTick(self, aircraft, desiredFacing, aircraft.Info.CruiseAltitude);
					return this;
				}
			}

			if (moveToRallyPoint)
			{
				QueueChild(self, new AttackMoveActivity(self, () => move.MoveToTarget(self, target)), true);
				moveToRallyPoint = false;
				return this;
			}

			return NextActivity;
		}
	}
}
