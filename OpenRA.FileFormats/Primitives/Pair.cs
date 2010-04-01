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

using System.Collections.Generic;

namespace OpenRA.FileFormats
{
	public struct Pair<T, U>
	{
		public T First;
		public U Second;

		public Pair(T first, U second)
		{
			First = first;
			Second = second;
		}

		static IEqualityComparer<T> tc = EqualityComparer<T>.Default;
		static IEqualityComparer<U> uc = EqualityComparer<U>.Default;

		public static bool operator ==(Pair<T, U> a, Pair<T, U> b)
		{
			return tc.Equals(a.First, b.First) && uc.Equals(a.Second, b.Second);
		}

		public static bool operator !=(Pair<T, U> a, Pair<T, U> b)
		{
			return !(a == b);
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Pair<T, U>))
				return false;

			return (Pair<T, U>)obj == this;
		}

		public override int GetHashCode()
		{
			return First.GetHashCode() ^ Second.GetHashCode();
		}

		public Pair<T, U> WithFirst(T t) { return new Pair<T, U>(t, Second); }
		public Pair<T, U> WithSecond(U u) { return new Pair<T, U>(First, u); }

		public static T AsFirst(Pair<T, U> p) { return p.First; }
		public static U AsSecond(Pair<T, U> p) { return p.Second; }

        public Pair<U, T> Swap() { return Pair.New(Second, First); }
	}

    public static class Pair
    {
        public static Pair<T, U> New<T, U>(T t, U u) { return new Pair<T, U>(t, u); }
    }
}
