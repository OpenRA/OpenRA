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
		Target target;
		bool moveToRallyPoint;
		bool assignTargetOnFirstRun;

		public TakeOff(Actor self, Target target)
		{
			aircraft = self.Trait<Aircraft>();
			move = self.Trait<IMove>();
			this.target = target;
		}

		public TakeOff(Actor self)
			: this(self, Target.Invalid)
		{
			assignTargetOnFirstRun = true;
		}

		protected override void OnFirstRun(Actor self)
		{
			if (aircraft.ForceLanding)
				return;

			if (assignTargetOnFirstRun)
			{
				var host = aircraft.GetActorBelow();
				var rp = host != null ? host.TraitOrDefault<RallyPoint>() : null;

				var rallyPointDestination = rp != null ? rp.Location :
					(host != null ? self.World.Map.CellContaining(host.CenterPosition) : self.Location);

				target = Target.FromCell(self.World, rallyPointDestination);
				moveToRallyPoint = self.CurrentActivity.NextActivity == null && rallyPointDestination != self.Location;
			}

			// We are taking off, so remove reservation and influence in ground cells.
			aircraft.UnReserve();
			aircraft.RemoveInfluence();

			if (aircraft.Info.TakeoffSounds.Length > 0)
				Game.Sound.Play(SoundType.World, aircraft.Info.TakeoffSounds, self.World, aircraft.CenterPosition);
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

			var dat = self.World.Map.DistanceAboveTerrain(aircraft.CenterPosition);
			if (dat < aircraft.Info.CruiseAltitude)
			{
				// If we're a VTOL, rise before flying forward
				if (aircraft.Info.VTOL)
				{
					Fly.VerticalTakeOffOrLandTick(self, aircraft, aircraft.Facing, aircraft.Info.CruiseAltitude);
					return this;
				}
				else
				{
					Fly.FlyTick(self, aircraft, aircraft.Facing, aircraft.Info.CruiseAltitude);
					return this;
				}
			}

			// Checking for NextActivity == null again in case another activity was queued while taking off
			if (moveToRallyPoint && NextActivity == null)
			{
				QueueChild(self, new AttackMoveActivity(self, () => move.MoveToTarget(self, target)), true);
				moveToRallyPoint = false;
				return this;
			}

			return NextActivity;
		}
	}
}
