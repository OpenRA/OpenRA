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
using System.Collections.Generic;
using System.Drawing;

namespace OpenRA.Primitives
{
	public struct Pair<T, U> : IEquatable<Pair<T, U>>
	{
		public T First;
		public U Second;

		public Pair(T first, U second)
		{
			First = first;
			Second = second;
		}

		internal static IEqualityComparer<T> Tcomparer = EqualityComparer<T>.Default;
		internal static IEqualityComparer<U> Ucomparer = EqualityComparer<U>.Default;

		public static bool operator ==(Pair<T, U> a, Pair<T, U> b)
		{
			return Tcomparer.Equals(a.First, b.First) && Ucomparer.Equals(a.Second, b.Second);
		}

		public static bool operator !=(Pair<T, U> a, Pair<T, U> b)
		{
			return !(a == b);
		}

		public override int GetHashCode() { return First.GetHashCode() ^ Second.GetHashCode(); }

		public bool Equals(Pair<T, U> other) { return this == other; }
		public override bool Equals(object obj) { return obj is Pair<T, U> && Equals((Pair<T, U>)obj); }

		public Pair<T, U> WithFirst(T t) { return new Pair<T, U>(t, Second); }
		public Pair<T, U> WithSecond(U u) { return new Pair<T, U>(First, u); }

		public static T AsFirst(Pair<T, U> p) { return p.First; }
		public static U AsSecond(Pair<T, U> p) { return p.Second; }

		public override string ToString()
		{
			return "({0},{1})".F(First, Second);
		}

		class PairEqualityComparer : IEqualityComparer<Pair<T, U>>
		{
			public bool Equals(Pair<T, U> x, Pair<T, U> y) { return x == y; }
			public int GetHashCode(Pair<T, U> obj) { return obj.GetHashCode(); }
		}

		public static IEqualityComparer<Pair<T, U>> EqualityComparer { get { return new PairEqualityComparer(); } }
	}

	public static class Pair
	{
		public static Pair<T, U> New<T, U>(T t, U u) { return new Pair<T, U>(t, u); }

		static Pair()
		{
			Pair<char, Color>.Ucomparer = new ColorEqualityComparer();
		}

		// avoid the default crappy one
		class ColorEqualityComparer : IEqualityComparer<Color>
		{
			public bool Equals(Color x, Color y) { return x.ToArgb() == y.ToArgb(); }
			public int GetHashCode(Color obj) { return obj.GetHashCode(); }
		}
	}
}
