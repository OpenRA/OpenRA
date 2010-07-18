#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
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
