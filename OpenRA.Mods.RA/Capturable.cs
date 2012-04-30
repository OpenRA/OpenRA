#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class CapturableInfo : ITraitInfo
	{
		public readonly string Type = "building";
		public readonly bool AllowAllies = false;
		public readonly bool AllowNeutral = true;
		public readonly bool AllowEnemies = true;
		public readonly int CaptureCompleteTime = 10; // seconds

		public object Create(ActorInitializer init) { return new Capturable(this); }
	}

	public class Capturable : ITick
	{
		[Sync] Actor captor = null;
		[Sync] public int CaptureProgressTime = 0;
		public bool CaptureInProgress { get { return captor != null; } }
		public CapturableInfo Info;

		public Capturable(CapturableInfo info)
		{
			this.Info = info;
		}

		public void BeginCapture(Actor self, Actor captor)
		{
			CaptureProgressTime = 0;

			this.captor = captor;

			if (self.Owner != self.World.WorldActor.Owner)
				self.ChangeOwner(self.World.WorldActor.Owner);
		}

		public void Tick(Actor self)
		{
			if (!CaptureInProgress) return;

			if (CaptureProgressTime < Info.CaptureCompleteTime * 25)
				CaptureProgressTime++;
			else
			{
				self.World.AddFrameEndTask(w =>
				{
					self.ChangeOwner(captor.Owner);

					foreach (var t in self.TraitsImplementing<INotifyCapture>())
						t.OnCapture(self, captor, self.Owner, captor.Owner);

					foreach (var t in captor.World.ActorsWithTrait<INotifyOtherCaptured>())
						t.Trait.OnActorCaptured(t.Actor, self, captor, self.Owner, captor.Owner);

					captor = null;
				});
			}
		}
	}
}
