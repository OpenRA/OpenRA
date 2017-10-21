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

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor can be captured by a unit with Captures: trait.")]
	public class CapturableInfo : ITraitInfo
	{
		[Desc("CaptureTypes (from the Captures trait) that are able to capture this.")]
		public readonly HashSet<string> Types = new HashSet<string>() { "building" };

		public readonly bool AllowAllies = false;
		public readonly bool AllowNeutral = true;
		public readonly bool AllowEnemies = true;

		[Desc("Health percentage the target must be at (or below) before it can be captured.")]
		public readonly int CaptureThreshold = 50;
		public readonly bool CancelActivity = false;

		public object Create(ActorInitializer init) { return new Capturable(this); }

		public bool CanBeTargetedBy(Actor captor, Player owner)
		{
			var c = captor.Info.TraitInfoOrDefault<CapturesInfo>();
			if (c == null)
				return false;

			var playerRelationship = owner.Stances[captor.Owner];
			if (playerRelationship == Stance.Ally && !AllowAllies)
				return false;

			if (playerRelationship == Stance.Enemy && !AllowEnemies)
				return false;

			if (playerRelationship == Stance.Neutral && !AllowNeutral)
				return false;

			if (!c.CaptureTypes.Overlaps(Types))
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
