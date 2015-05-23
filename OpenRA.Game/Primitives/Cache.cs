#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
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
	public class Cache<T, U> : IReadOnlyDictionary<T, U>
	{
		readonly Dictionary<T, U> cache;
		readonly Func<T, U> loader;

		public Cache(Func<T, U> loader, IEqualityComparer<T> c)
		{
			if (loader == null)
				throw new ArgumentNullException("loader");
			this.loader = loader;
			cache = new Dictionary<T, U>(c);
		}

		public Cache(Func<T, U> loader)
			: this(loader, EqualityComparer<T>.Default) { }

		public U this[T key]
		{
			get
			{
				U result;
				if (!cache.TryGetValue(key, out result))
					cache.Add(key, result = loader(key));
				return result;
			}
		}

		public bool ContainsKey(T key) { return cache.ContainsKey(key); }
		public bool TryGetValue(T key, out U value) { return cache.TryGetValue(key, out value); }
		public int Count { get { return cache.Count; } }
		public ICollection<T> Keys { get { return cache.Keys; } }
		public ICollection<U> Values { get { return cache.Values; } }
		public IEnumerator<KeyValuePair<T, U>> GetEnumerator() { return cache.GetEnumerator(); }
		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}
}
