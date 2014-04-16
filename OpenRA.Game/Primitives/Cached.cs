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

namespace OpenRA.Primitives
{
	public class Cached<T>
	{
		Func<T> p;
		T value;
		bool hasValue;

		public Cached(Func<T> p)
		{
			if (p == null)
				throw new ArgumentNullException();

			this.p = p;
		}

		public T Value
		{
			get
			{
				if (hasValue)
					return value;

				value = p();
				hasValue = true;
				return value;
			}
		}

		public T Force() { return Value; }

		public void Invalidate()
		{
			hasValue = false;
		}
	}

	public static class Cached
	{
		public static Cached<T> New<T>(Func<T> p) { return new Cached<T>(p); }
	}
}

