using System;

namespace OpenRa.Traits
{
	class ReservableInfo : ITraitInfo
	{
		public object Create(Actor self) { return new Reservable(self); }
	}

	class Reservable : ITick
	{
		public Reservable(Actor self) { }
		Actor reservedFor;

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

		public static bool IsReserved(Actor a)
		{
			var res = a.traits.GetOrDefault<Reservable>();
			return res != null && res.reservedFor != null;
		}
	}
}
