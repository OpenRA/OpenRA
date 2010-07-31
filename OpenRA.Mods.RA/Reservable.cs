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
	class ReservableInfo : TraitInfo<Reservable> { }

	public class Reservable : ITick
	{
		Actor reservedFor;

		public void Tick(Actor self)
		{
			if (reservedFor == null) 
				return;		/* nothing to do */

			if (reservedFor.IsDead() || !reservedFor.IsInWorld)
				reservedFor = null;		/* not likely to arrive now. */
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
