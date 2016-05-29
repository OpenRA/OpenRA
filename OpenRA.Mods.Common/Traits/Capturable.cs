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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor can be captured by a unit with Captures: trait.")]
	public class CapturableInfo : ITraitInfo
	{
		[Desc("Type listed under Types in Captures: trait of actors that can capture this).")]
		public readonly string Type = "building";

		public readonly Stance TargetStances = Stance.Enemy | Stance.Neutral;
		public readonly Stance ForceTargetStances = Stance.Enemy | Stance.Neutral;

		[Desc("Health percentage the target must be at (or below) before it can be captured.")]
		public readonly int CaptureThreshold = 50;
		public readonly bool CancelActivity = false;

		public object Create(ActorInitializer init) { return new Capturable(this); }

		public bool CanBeTargetedBy(Actor captor, Player owner)
		{
			var c = captor.Info.TraitInfoOrDefault<CapturesInfo>();
			if (c == null)
				return false;

			if (!c.CaptureTypes.Contains(Type))
				return false;

			var stances = ForceTargetStances | TargetStances;
			if (!stances.HasStance(captor.Owner.Stances[owner]))
				return false;

			return true;
		}
	}

	public class Capturable : INotifyCapture
	{
		public readonly CapturableInfo Info;
		public bool BeingCaptured { get; private set; }
		public Capturable(CapturableInfo info) { Info = info; }

		public void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			BeingCaptured = true;
			self.World.AddFrameEndTask(w => BeingCaptured = false);

			if (Info.CancelActivity)
			{
				var stop = new Order("Stop", self, false);
				foreach (var t in self.TraitsImplementing<IResolveOrder>())
					t.ResolveOrder(self, stop);
			}
		}
	}
}
