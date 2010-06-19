#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
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
