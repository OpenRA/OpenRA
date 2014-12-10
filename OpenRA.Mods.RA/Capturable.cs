#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("This actor can be captured by a unit with Captures: trait.")]
	class CapturableInfo : ITraitInfo
	{
		[Desc("Type listed under Types in Captures: trait of actors that can capture this).")]
		public readonly string Type = "building";

		[Desc("Captor's stance with respect to target.")]
		public readonly Stance CaptorPlayers = Stance.Neutral | Stance.Enemy;
		[Desc("Health percentage the target must be at (or below) before it can be captured.")]
		public readonly float CaptureThreshold = 0.5f;
		public readonly bool CancelActivity = false;

		public object Create(ActorInitializer init) { return new Capturable(this); }

		public bool CanBeTargetedBy(Actor captor, Player owner)
		{
			var c = captor.TraitOrDefault<Captures>();
			if (c == null)
				return false;

			var playerRelationship = owner.Stances[captor.Owner];
			return playerRelationship.Intersects(CaptorPlayers) && c.Info.CaptureTypes.Contains(Type);
		}
	}

	class Capturable : INotifyCapture
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
