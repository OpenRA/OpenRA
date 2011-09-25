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

namespace OpenRA.FileFormats
{
	public class Lazy<T>
	{
		Func<T> p;
		T value;

		public Lazy(Func<T> p)
		{
			if (p == null)
				throw new ArgumentNullException();

			this.p = p;
		}

		public T Value
		{
			get
			{
				if (p == null)
					return value;

				value = p();
				p = null;
				return value;
			}
		}

		public T Force() { return Value; }
	}

	public static class Lazy
	{
		public static Lazy<T> New<T>(Func<T> p) { return new Lazy<T>(p); }
	}
}
