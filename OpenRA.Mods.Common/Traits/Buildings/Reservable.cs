#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Mods.Common.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Reserve landing places for aircraft.")]
	class ReservableInfo : TraitInfo<Reservable> { }

	public class Reservable : ITick, INotifyOwnerChanged, INotifySold, INotifyActorDisposing, INotifyCreated
	{
		Actor reservedFor;
		Aircraft reservedForAircraft;
		RallyPoint rallyPoint;

		void INotifyCreated.Created(Actor self)
		{
			rallyPoint = self.TraitOrDefault<RallyPoint>();
		}

		void ITick.Tick(Actor self)
		{
			// Nothing to do.
			if (reservedFor == null)
				return;

			if (!Target.FromActor(reservedFor).IsValidFor(self))
			{
				// Not likely to arrive now.
				reservedForAircraft.UnReserve();
				reservedFor = null;
				reservedForAircraft = null;
			}
		}

		public IDisposable Reserve(Actor self, Actor forActor, Aircraft forAircraft)
		{
			if (reservedForAircraft != null && reservedForAircraft.MayYieldReservation)
				UnReserve(self);

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

		public static bool IsAvailableFor(Actor reservable, Actor forActor)
		{
			var res = reservable.TraitOrDefault<Reservable>();
			return res == null || res.reservedForAircraft == null || res.reservedForAircraft.MayYieldReservation || res.reservedFor == forActor;
		}

		void UnReserve(Actor self)
		{
			if (reservedForAircraft != null)
			{
				if (reservedForAircraft.GetActorBelow() == self)
				{
					// HACK: Cache this in a local var, such that the inner activity of AttackMoveActivity can access the trait easily after reservedForAircraft was nulled
					var aircraft = reservedForAircraft;
					if (rallyPoint != null && rallyPoint.Path.Count > 0)
						foreach (var cell in rallyPoint.Path)
							reservedFor.QueueActivity(new AttackMoveActivity(reservedFor, () => aircraft.MoveTo(cell, 1, targetLineColor: Color.OrangeRed)));
					else
						reservedFor.QueueActivity(new TakeOff(reservedFor));
				}

				reservedForAircraft.UnReserve();
			}
		}

		void INotifyActorDisposing.Disposing(Actor self) { UnReserve(self); }

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner) { UnReserve(self); }

		void INotifySold.Selling(Actor self) { UnReserve(self); }
		void INotifySold.Sold(Actor self) { UnReserve(self); }
	}
}
