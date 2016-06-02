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

using OpenRA.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	class ExternalCaptureActor : Activity
	{
		readonly ExternalCapturable capturable;
		readonly ExternalCapturesInfo capturesInfo;
		readonly Mobile mobile;
		readonly Target target;

		public ExternalCaptureActor(Actor self, Target target)
		{
			this.target = target;
			capturable = target.Actor.Trait<ExternalCapturable>();
			capturesInfo = self.Info.TraitInfo<ExternalCapturesInfo>();
			mobile = self.Trait<Mobile>();
		}

		public override Activity Tick(Actor self)
		{
			if (target.Type != TargetType.Actor)
				return NextActivity;

			if (IsCanceled || !self.IsInWorld || self.IsDead || !target.IsValidFor(self))
			{
				if (capturable.CaptureInProgress)
					capturable.EndCapture();

				return NextActivity;
			}

			var nearest = target.Actor.OccupiesSpace.NearestCellTo(mobile.ToCell);

			if ((nearest - mobile.ToCell).LengthSquared > 2)
				return ActivityUtils.SequenceActivities(new MoveAdjacentTo(self, target), this);

			if (!capturable.CaptureInProgress)
				capturable.BeginCapture(self);
			else
			{
				if (capturable.Captor != self) return NextActivity;

				if (capturable.CaptureProgressTime % 25 == 0)
				{
					self.World.Add(new FlashTarget(target.Actor, self.Owner));
					self.World.Add(new FlashTarget(self));
				}

				if (capturable.CaptureProgressTime == capturable.Info.CaptureCompleteTime * 25)
				{
					self.World.AddFrameEndTask(w =>
					{
						if (target.Actor.IsDead)
							return;

						var oldOwner = target.Actor.Owner;

						target.Actor.ChangeOwner(self.Owner);

						foreach (var t in target.Actor.TraitsImplementing<INotifyCapture>())
							t.OnCapture(target.Actor, self, oldOwner, self.Owner);

						capturable.EndCapture();

						if (capturesInfo != null && capturesInfo.ConsumeActor)
							self.Dispose();
					});
				}
			}

			return this;
		}
	}
}
