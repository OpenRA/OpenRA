#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;
using OpenRA.Effects;
using OpenRA.Mods.RA.Move;
using OpenRA.Mods.RA.Buildings;

namespace OpenRA.Mods.RA.Activities
{
	class CaptureActor : Activity
	{
		Target target;

		public CaptureActor(Target target) { this.target = target; }

		public override Activity Tick(Actor self)
		{
			if (target.Type != TargetType.Actor)
				return NextActivity;

			var capturable = target.Actor.Trait<Capturable>();

			if (IsCanceled || !self.IsInWorld || self.IsDead())
			{
				if (capturable.CaptureInProgress)
					capturable.EndCapture();

				return NextActivity;
			}

			var mobile = self.Trait<Mobile>();
			var nearest = target.Actor.OccupiesSpace.NearestCellTo(mobile.toCell);

			if ((nearest - mobile.toCell).LengthSquared > 2)
				return Util.SequenceActivities(new MoveAdjacentTo(target), this);

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
					var capturesInfo = self.Info.Traits.Get<CapturesInfo>();

					self.World.AddFrameEndTask(w =>
					{
						var oldOwner = target.Actor.Owner;

						target.Actor.ChangeOwner(self.Owner);

						foreach (var t in target.Actor.TraitsImplementing<INotifyCapture>())
							t.OnCapture(target.Actor, self, oldOwner, self.Owner);

						capturable.EndCapture();

						if (capturesInfo != null && capturesInfo.ConsumeActor)
							self.Destroy();
					});
				}
			}
			return this;
		}
	}
}
