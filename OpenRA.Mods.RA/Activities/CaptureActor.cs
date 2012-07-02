#region Copyright & License Information
/*
 * Copyright 2007-2012 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;
using OpenRA.Mods.RA.Move;

namespace OpenRA.Mods.RA.Activities
{
	class CaptureActor : Activity
	{
		Actor target;

		public CaptureActor(Actor target) { this.target = target; }

		public override Activity Tick(Actor self)
		{
			if (IsCanceled) return NextActivity;
			if (target == null || !target.IsInWorld || target.IsDead()) return NextActivity;
			if (target.Owner == self.Owner) return NextActivity;

			var capturable = target.TraitOrDefault<Capturable>();
			if (capturable != null && capturable.CaptureInProgress && capturable.Captor.Owner.Stances[self.Owner] == Stance.Ally)
				return NextActivity;

			var sellable = target.TraitOrDefault<Sellable>();
			if (sellable != null && sellable.Selling)
				return NextActivity;

			var captures = self.TraitOrDefault<Captures>();
			var capturesInfo = self.Info.Traits.Get<CapturesInfo>();
			if (captures != null && Combat.IsInRange(self.CenterLocation, capturesInfo.Range, target))
				target.Trait<Capturable>().BeginCapture(target, self);
			else
				return Util.SequenceActivities(self.Trait<Mobile>().MoveWithinRange(Target.FromActor(target), capturesInfo.Range), this);
			if (capturesInfo != null && capturesInfo.wastedAfterwards)
				self.World.AddFrameEndTask(w => self.Destroy());

			return this;
		}
	}
}
