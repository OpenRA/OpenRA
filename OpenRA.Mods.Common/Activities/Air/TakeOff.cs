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

using System.Collections.Generic;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class TakeOff : Activity
	{
		readonly Aircraft aircraft;
		readonly IMove move;
		Target fallbackTarget;
		bool movedToTarget = false;

		public TakeOff(Actor self, Target fallbackTarget)
		{
			aircraft = self.Trait<Aircraft>();
			move = self.Trait<IMove>();
			this.fallbackTarget = fallbackTarget;
		}

		public TakeOff(Actor self)
			: this(self, Target.Invalid) { }

		protected override void OnFirstRun(Actor self)
		{
			if (aircraft.ForceLanding)
				return;

			if (self.World.Map.DistanceAboveTerrain(aircraft.CenterPosition).Length >= aircraft.Info.MinAirborneAltitude)
				return;

			// We are taking off, so remove reservation and influence in ground cells.
			aircraft.UnReserve();
			aircraft.RemoveInfluence();

			if (aircraft.Info.TakeoffSounds.Length > 0)
				Game.Sound.Play(SoundType.World, aircraft.Info.TakeoffSounds, self.World, aircraft.CenterPosition);
		}

		public override bool Tick(Actor self)
		{
			// Refuse to take off if it would land immediately again.
			if (aircraft.ForceLanding)
			{
				Cancel(self);
				return true;
			}

			var dat = self.World.Map.DistanceAboveTerrain(aircraft.CenterPosition);
			if (dat < aircraft.Info.CruiseAltitude)
			{
				// If we're a VTOL, rise before flying forward
				if (aircraft.Info.VTOL)
				{
					Fly.VerticalTakeOffOrLandTick(self, aircraft, aircraft.Facing, aircraft.Info.CruiseAltitude);
					return false;
				}

				Fly.FlyTick(self, aircraft, aircraft.Facing, aircraft.Info.CruiseAltitude);
				return false;
			}

			// Only move to the fallback target if we don't have anything better to do
			if (NextActivity == null && fallbackTarget.IsValidFor(self) && !movedToTarget)
			{
				QueueChild(new AttackMoveActivity(self, () => move.MoveToTarget(self, fallbackTarget, targetLineColor: Color.OrangeRed)));
				movedToTarget = true;
				return false;
			}

			return true;
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			if (ChildActivity != null)
				foreach (var n in ChildActivity.TargetLineNodes(self))
					yield return n;
			else
				yield return new TargetLineNode(fallbackTarget, Color.OrangeRed);
		}
	}
}
