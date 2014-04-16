#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;

namespace OpenRA.Primitives
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

		internal static IEqualityComparer<T> tc = EqualityComparer<T>.Default;
		internal static IEqualityComparer<U> uc = EqualityComparer<U>.Default;

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
			var o = obj as Pair<T, U>?;
			return o != null && o == this;
		}

		public override int GetHashCode()
		{
			return First.GetHashCode() ^ Second.GetHashCode();
		}

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
			Pair<char,Color>.uc = new ColorEqualityComparer();
		}

		// avoid the default crappy one
		class ColorEqualityComparer : IEqualityComparer<Color>
		{
			public bool Equals(Color x, Color y) { return x.ToArgb() == y.ToArgb(); }
			public int GetHashCode(Color obj) { return obj.GetHashCode(); }
		}
	}
}
