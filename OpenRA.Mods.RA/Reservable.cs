#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class ReservableInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new Reservable(init.self); }
	}

	public class Reservable : ITick
	{
		Actor reservedFor;
		Actor self;

		public Reservable(Actor self) { this.self = self; }

		public void Tick(Actor self)
		{
			if (reservedFor == null) 
				return;		/* nothing to do */

			if (!reservedFor.IsInWorld || reservedFor.IsDead())	// todo: replace with Target.IsValid?
				reservedFor = null;		/* not likely to arrive now. */
		}

		public IDisposable Reserve(Actor forActor)
		{
			//if (reservedFor != null)
			//    Game.Debug("BUG: #{0} {1} was already reserved (by #{2} {3})".F(
			//        self.ActorID, self.Info.Name, reservedFor.ActorID, reservedFor.Info.Name));

			reservedFor = forActor;
			//Game.Debug("#{0} {1} reserved by #{2} {3}".F(
			//    self.ActorID, self.Info.Name, forActor.ActorID, forActor.Info.Name));

			return new DisposableAction(() =>
				{
					//Game.Debug("#{0} {1} unreserved".F(
					//    self.ActorID, self.Info.Name));
					reservedFor = null;
				});
		}

		public static bool IsReserved(Actor a)
		{
			var res = a.TraitOrDefault<Reservable>();
			return res != null && res.reservedFor != null;
		}
	}
}
