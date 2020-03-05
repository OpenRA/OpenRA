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
using System.Collections.Generic;
using OpenRA.Mods.Common.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Reserve landing places for aircraft.")]
	public class ReservableInfo : ITraitInfo
	{
		[Desc("Maximum Reserve Spaces.")]
		public readonly int MaxReserves = 1;
		public virtual object Create(ActorInitializer init) { return new Reservable(init, this); }
	}

	public class Reservable : ITick, INotifyOwnerChanged, INotifySold, INotifyActorDisposing, INotifyCreated
	{
		Actor[] reservedForActors;
		Aircraft[] reservedForAircrafts;
		RallyPoint rallyPoint;
		public readonly ReservableInfo Info;
		public Reservable(ActorInitializer init, ReservableInfo info)
		{
			Info = info;
			reservedForActors = new Actor[info.MaxReserves];
			reservedForAircrafts = new Aircraft[info.MaxReserves];
		}

		void INotifyCreated.Created(Actor self)
		{
			rallyPoint = self.TraitOrDefault<RallyPoint>();
		}

		void ITick.Tick(Actor self)
		{
			for (int i = 0; i < Info.MaxReserves; i++)
			{
				Actor cachedActor = reservedForActors[i];
				if (cachedActor == null)
					continue;

				if (!Target.FromActor(cachedActor).IsValidFor(self))
				{
					// Not likely to arrive now.
					reservedForAircrafts[i].UnReserve();
					reservedForActors[i] = null;
					reservedForAircrafts[i] = null;
				}
			}
		}

		public IDisposable Reserve(Actor self, Actor forActor, Aircraft forAircraft)
		{
			bool freeSpace = false;
			int i = 0;
			for (; i < Info.MaxReserves; i++)
			{
				if (reservedForActors[i] != null && reservedForAircrafts[i].MayYieldReservation)
					freeSpace = false;
				else
				{
					freeSpace = true;
					break;
				}
			}

			if (!freeSpace)
			{
				// prevent index out of range
				i--;
				UnReserve(self);
			}

			reservedForActors[i] = forActor;
			reservedForAircrafts[i] = forAircraft;
			return new DisposableAction(
				() => { reservedForActors[i] = null; reservedForAircrafts[i] = null; },
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
			if (res == null)
				return false;

			bool isReserved = true;
			for (int i = 0; i < res.Info.MaxReserves; i++)
			{
				isReserved = res.reservedForAircrafts[i] != null && !res.reservedForAircrafts[i].MayYieldReservation;
				if (!isReserved)
					break;
			}

			return isReserved;
		}

		public static int GetFreeReservation(Actor a, Actor self)
		{
			var res = a.TraitOrDefault<Reservable>();
			for (int i = 0; i < res.Info.MaxReserves; i++)
			{
				if (res.reservedForActors[i] == null || res.reservedForActors[i] == self)
					return i;
			}

			// cells exhausted, return -1 to signify none found
			return -1;
		}

		public static bool IsAvailableFor(Actor reservable, Actor forActor)
		{
			var res = reservable.TraitOrDefault<Reservable>();
			bool isAvailable = true;
			for (int i = 0; i < res.Info.MaxReserves; i++)
			{
				isAvailable = res == null || res.reservedForAircrafts[i] == null || res.reservedForAircrafts[i].MayYieldReservation || res.reservedForActors[i] == forActor;
				if (!isAvailable)
					break;
			}

			return isAvailable;
		}

		void UnReserve(Actor self)
		{
			for (int i = 0; i < Info.MaxReserves; i++)
			{
				Actor cachedActor = reservedForActors[i];
				Aircraft cachedAircraft = reservedForAircrafts[i];
				if (cachedAircraft != null)
				{
					if (cachedAircraft.GetActorBelow() == self)
					{
						if (rallyPoint != null)
							foreach (var cell in rallyPoint.Path)
								cachedActor.QueueActivity(
										cachedAircraft.MoveTo(
											cell,
											1,
											targetLineColor: Color.Green));
						else
							cachedActor.QueueActivity(
									new TakeOff(reservedForActors[i]));
					}

					reservedForAircrafts[i].UnReserve();
				}
			}
		}

		void INotifyActorDisposing.Disposing(Actor self) { UnReserve(self); }

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner) { UnReserve(self); }

		void INotifySold.Selling(Actor self) { UnReserve(self); }
		void INotifySold.Sold(Actor self) { UnReserve(self); }
	}
}
