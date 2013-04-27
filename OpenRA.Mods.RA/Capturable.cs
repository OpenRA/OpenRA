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
using OpenRA.FileFormats;
using OpenRA.Traits;
using OpenRA.Mods.RA.Buildings;

namespace OpenRA.Mods.RA
{
	[Desc("This actor can be captured by a unit with Captures: trait.")]
	public class CapturableInfo : ITraitInfo
	{
		public readonly string Type = "building";
		public readonly bool AllowAllies = false;
		public readonly bool AllowNeutral = true;
		public readonly bool AllowEnemies = true;
		[Desc("Seconds it takes to change the owner.", "It stays neutral during this period. You might want to add a CapturableBar: trait, too.")]
		public readonly int CaptureCompleteTime = 10;

		public object Create(ActorInitializer init) { return new Capturable(this); }
	}

	public class Capturable : ITick
	{
		[Sync] public Actor Captor = null;
		[Sync] public int CaptureProgressTime = 0;
		public bool CaptureInProgress { get { return Captor != null; } }
		public CapturableInfo Info;

		public Capturable(CapturableInfo info)
		{
			this.Info = info;
		}

		public bool BeginCapture(Actor self, Actor captor)
		{
			if (!CaptureInProgress && !self.Trait<Building>().Lock())
				return false;

			if (CaptureInProgress && Captor.Owner.Stances[captor.Owner] == Stance.Ally)
				return false;

			CaptureProgressTime = 0;

			this.Captor = captor;

			if (self.Owner != self.World.WorldActor.Owner)
				self.ChangeOwner(self.World.WorldActor.Owner);

			return true;
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
					self.ChangeOwner(Captor.Owner);
					ChangeCargoOwner(self, Captor.Owner);

					foreach (var t in self.TraitsImplementing<INotifyCapture>())
						t.OnCapture(self, Captor, self.Owner, Captor.Owner);

					foreach (var t in Captor.World.ActorsWithTrait<INotifyOtherCaptured>())
						t.Trait.OnActorCaptured(t.Actor, self, Captor, self.Owner, Captor.Owner);

					Captor = null;
					self.Trait<Building>().Unlock();
				});
			}
		}

		public static void ChangeCargoOwner(Actor self, Player captor)
		{
			var cargo = self.TraitOrDefault<Cargo>();
			if (cargo == null)
				return;

			foreach (var c in cargo.Passengers)
				c.Owner = captor;
		}
	}
}
