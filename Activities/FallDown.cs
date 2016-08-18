#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Activities
{
	public class FallDown : Activity
	{
		readonly IPositionable pos;
		readonly WVec fallVector;

		WPos dropPosition;
		WPos currentPosition;
		bool triggered = false;

		public FallDown(Actor self, WPos dropPosition, int fallRate, Actor ignoreActor = null)
		{
			pos = self.TraitOrDefault<IPositionable>();

			fallVector = new WVec(0, 0, fallRate);
			this.dropPosition = dropPosition;
		}

		Activity FirstTick(Actor self)
		{
			triggered = true;

			// Place the actor and retrieve its visual position (CenterPosition)
			pos.SetPosition(self, dropPosition);
			currentPosition = self.CenterPosition;

			return this;
		}

		Activity LastTick(Actor self)
		{
			var dat = self.World.Map.DistanceAboveTerrain(currentPosition);
			pos.SetPosition(self, currentPosition - new WVec(WDist.Zero, WDist.Zero, dat));

			return NextActivity;
		}

		public override Activity Tick(Actor self)
		{
			// If this is the first tick
			if (!triggered)
				return FirstTick(self);

			currentPosition -= fallVector;

			// If the unit has landed, this will be the last tick
			if (self.World.Map.DistanceAboveTerrain(currentPosition).Length <= 0)
				return LastTick(self);

			pos.SetVisualPosition(self, currentPosition);

			return this;
		}

		// Only the last queued activity (given order) is kept
		public override void Queue(Activity activity)
		{
			NextActivity = activity;
		}

		// Cannot be cancelled
		public override void Cancel(Actor self) { }
	}
}
