using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class StrategicPointInfo : ITraitInfo
	{
		public readonly bool Critical = false;

		public object Create(ActorInitializer init) { return new StrategicPoint(init.self, this); }
	}

	public class StrategicPoint : INotifyCapture, ITick
	{
		[Sync] public Actor Self;
		[Sync] public bool Critical;
		[Sync] public Player OriginalOwner;
		[Sync] public int TicksOwned = 0;

		public StrategicPointInfo Info;

		public StrategicPoint(Actor self, StrategicPointInfo info)
		{
			Self = self;
			Info = info;
			OriginalOwner = self.Owner;

			Critical = info.Critical;
		}

		public void OnCapture(Actor self, Actor captor, Player oldOwner, Player newOwner)
		{
			TicksOwned = 0;
		}

		public void Tick(Actor self) 
		{
			if (OriginalOwner == self.Owner || self.Owner.WinState != WinState.Undefined) return;

			TicksOwned++;
		}
	}
}
