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
using System.Collections;
using System.Collections.Generic;

namespace OpenRA.Primitives
{
	public class Cache<T, U> : IEnumerable<KeyValuePair<T, U>>
	{
		Dictionary<T, U> hax;
		Func<T,U> loader;

		public Cache(Func<T,U> loader, IEqualityComparer<T> c)
		{
			hax = new Dictionary<T, U>(c);
			if (loader == null)
				throw new ArgumentNullException("loader");

			this.loader = loader;
		}

		public Cache(Func<T, U> loader)
			: this(loader, EqualityComparer<T>.Default) { }

		public U this[T key]
		{
			get
			{
				U result;
				if (!hax.TryGetValue(key, out result))
					hax.Add(key, result = loader(key));

				return result;
			}
		}

		public IEnumerator<KeyValuePair<T, U>> GetEnumerator() { return hax.GetEnumerator(); }

		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

		public IEnumerable<T> Keys { get { return hax.Keys; } }
		public IEnumerable<U> Values { get { return hax.Values; } }
	}
}
