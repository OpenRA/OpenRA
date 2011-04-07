#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA.Traits;
using OpenRA.Mods.RA.Air;

namespace OpenRA.Mods.RA
{
	class ReservableInfo : TraitInfo<Reservable> {}

	public class Reservable : ITick, INotifyDamage, INotifyCapture, INotifySold
	{
		Actor reservedFor;
		Aircraft herp;

		public void Tick(Actor self)
		{
			if (reservedFor == null) 
				return;		/* nothing to do */

			if (!Target.FromActor( reservedFor ).IsValid)
				reservedFor = null;		/* not likely to arrive now. */
		}

		public IDisposable Reserve(Actor self, Actor forActor, Aircraft derp)
		{
			reservedFor = forActor;
			herp = derp;

			// NOTE: we really dont care about the GC eating DisposableActions that apply to a world *other* than
			// the one we're playing in.

			return new DisposableAction(
				() => {reservedFor = null; herp = null;},
				() => Game.RunAfterTick(
					() => { if (Game.IsCurrentWorld( self.World )) throw new InvalidOperationException(
						"Attempted to finalize an undisposed DisposableAction. {0} ({1}) reserved {2} ({3})"
						.F(forActor.Info.Name, forActor.ActorID, self.Info.Name, self.ActorID)); }));
		}

		public static bool IsReserved(Actor a)
		{
			var res = a.TraitOrDefault<Reservable>();
			return res != null && res.reservedFor != null;
		}
		
		public void Damaged(Actor self, AttackInfo e)
		{
			if (herp != null && e.DamageStateChanged && e.DamageState == DamageState.Dead)
				herp.UnReserve();
		}
		
		public void OnCapture (Actor self, Actor captor, Player oldOwner, Player newOwner)
		{		
			if (herp != null)
				herp.UnReserve();
		}

		public void Selling (Actor self) { Sold(self); }
		public void Sold (Actor self)
		{
            if (herp != null)
				herp.UnReserve();
		}
	}
}
