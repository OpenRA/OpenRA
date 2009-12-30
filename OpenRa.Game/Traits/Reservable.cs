using System;

namespace OpenRa.Game.Traits
{
	class Reservable : ITick
	{
		public Reservable(Actor self) { }
		Actor reservedFor;

		public bool IsReserved { get { return reservedFor != null; } }

		public void Tick(Actor self)
		{
			if (reservedFor == null) 
				return;		/* nothing to do */

			if (reservedFor.IsDead) reservedFor = null;		/* not likely to arrive now. */
		}

		public IDisposable Reserve(Actor forActor)
		{
			reservedFor = forActor;
			return new DisposableAction(() => reservedFor = null);
		}
	}
}
