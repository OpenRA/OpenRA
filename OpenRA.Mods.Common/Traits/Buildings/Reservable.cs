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

using System;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Reserve landing places for aircraft.")]
	class ReservableInfo : TraitInfo<Reservable> { }

	public class Reservable : ITick, INotifyOwnerChanged, INotifySold, INotifyActorDisposing
	{
		Actor reservedFor;
		Aircraft reservedForAircraft;

		public void Tick(Actor self)
		{
			if (reservedFor == null)
				return;		/* nothing to do */

			if (!Target.FromActor(reservedFor).IsValidFor(self))
			{
				/* Not likely to arrive now. */
				reservedForAircraft.UnReserve();
				reservedFor = null;
				reservedForAircraft = null;
			}
		}

		public IDisposable Reserve(Actor self, Actor forActor, Aircraft forAircraft)
		{
			if (reservedForAircraft != null && reservedForAircraft.MayYieldReservation)
				reservedForAircraft.UnReserve();

			reservedFor = forActor;
			reservedForAircraft = forAircraft;

			// NOTE: we really don't care about the GC eating DisposableActions that apply to a world *other* than
			// the one we're playing in.
			return new DisposableAction(
				() => { reservedFor = null; reservedForAircraft = null; },
				() => Game.RunAfterTick(() =>
				{
					if (Game.IsCurrentWorld(self.World))
						throw new InvalidOperationException(
							"Attempted to finalize an undisposed DisposableAction. {0} ({1}) reserved {2} ({3})".F(
							forActor.Info.Name, forActor.ActorID, self.Info.Name, self.ActorID));
				}));
		}

		public static bool IsReserved(Actor a)
		{
			var res = a.TraitOrDefault<Reservable>();
			return res != null && res.reservedForAircraft != null && !res.reservedForAircraft.MayYieldReservation;
		}

		public void Disposing(Actor self)
		{
			if (reservedForAircraft != null)
				reservedForAircraft.UnReserve();
		}

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (reservedForAircraft != null)
				reservedForAircraft.UnReserve();
		}

		public void Selling(Actor self) { Sold(self); }

		public void Sold(Actor self)
		{
			if (reservedForAircraft != null)
				reservedForAircraft.UnReserve();
		}
	}
}
