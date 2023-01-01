#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace OpenRA.Primitives
{
	public class ConcurrentCache<T, U> : IReadOnlyDictionary<T, U>
	{
		readonly ConcurrentDictionary<T, U> cache;
		readonly Func<T, U> loader;

		public ConcurrentCache(Func<T, U> loader, IEqualityComparer<T> c)
		{
			if (loader == null)
				throw new ArgumentNullException(nameof(loader));

			this.loader = loader;
			cache = new ConcurrentDictionary<T, U>(c);
		}

		public ConcurrentCache(Func<T, U> loader)
			: this(loader, EqualityComparer<T>.Default) { }

		public U this[T key] => cache.GetOrAdd(key, loader);
		public IEnumerable<T> Keys => cache.Keys;

		public IEnumerable<U> Values => cache.Values;

		public bool ContainsKey(T key) { return cache.ContainsKey(key); }
		public bool TryGetValue(T key, out U value) { return cache.TryGetValue(key, out value); }
		public int Count => cache.Count;
		public IEnumerator<KeyValuePair<T, U>> GetEnumerator() { return cache.GetEnumerator(); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}
}
