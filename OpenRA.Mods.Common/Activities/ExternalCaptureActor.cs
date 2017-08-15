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

using OpenRA.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	class ExternalCaptureActor : Activity
	{
		readonly ExternalCapturable capturable;
		readonly Actor underlyingActor;
		readonly ExternalCapturesInfo capturesInfo;
		readonly Mobile mobile;
		readonly Target target;

		public ExternalCaptureActor(Actor self, Target target)
		{
			this.target = target;
			if (target.Type == TargetType.Actor)
				underlyingActor = target.Actor;
			else if (target.Type == TargetType.FrozenActor)
				underlyingActor = target.FrozenActor.Actor;
			capturable = underlyingActor.Trait<ExternalCapturable>();
			capturesInfo = self.Info.TraitInfo<ExternalCapturesInfo>();
			mobile = self.Trait<Mobile>();
		}

		public override Activity Tick(Actor self)
		{
			bool isFrozenActor = target.Type == TargetType.FrozenActor;

			if (target.Type != TargetType.Actor && target.Type != TargetType.FrozenActor)
				return NextActivity;

			if (IsCanceled || !self.IsInWorld || self.IsDead || !target.IsValidFor(self))
			{
				if (capturable.CaptureInProgress)
					capturable.EndCapture();

				return NextActivity;
			}

			if (!target.IsOwnerTargetable(self))
			{
				var target2 = target.TryUpdateFrozenActorTarget(self);
				if (target2.Type == TargetType.Invalid)
					return NextActivity;
				return new ExternalCaptureActor(self, target2);	
			}

			var occ = isFrozenActor ? target.FrozenActor : target.Actor.OccupiesSpace;
			var nearest = occ.NearestCellTo(mobile.ToCell);

			if ((nearest - mobile.ToCell).LengthSquared > 2)
				return ActivityUtils.SequenceActivities(new MoveAdjacentTo(self, target), this);

			if (!capturable.CaptureInProgress)
				capturable.BeginCapture(self);
			else
			{
				if (capturable.Captor != self) return NextActivity;

				if (capturable.CaptureProgressTime % 25 == 0)
				{
					self.World.Add(new FlashTarget(underlyingActor, self.Owner));
					self.World.Add(new FlashTarget(self));
				}

				if (capturable.CaptureProgressTime == capturable.Info.CaptureCompleteTime * 25)
				{
					self.World.AddFrameEndTask(w =>
					{
						if (underlyingActor.IsDead)
							return;

						var oldOwner = underlyingActor.Owner;

						underlyingActor.ChangeOwner(self.Owner);

						foreach (var t in underlyingActor.TraitsImplementing<INotifyCapture>())
							t.OnCapture(underlyingActor, self, oldOwner, self.Owner);

						capturable.EndCapture();

						if (self.Owner.Stances[oldOwner].HasStance(capturesInfo.PlayerExperienceStances))
						{
							var exp = self.Owner.PlayerActor.TraitOrDefault<PlayerExperience>();
							if (exp != null)
								exp.GiveExperience(capturesInfo.PlayerExperience);
						}

						if (capturesInfo != null && capturesInfo.ConsumeActor)
							self.Dispose();
					});
				}
			}

			return this;
		}
	}
}
