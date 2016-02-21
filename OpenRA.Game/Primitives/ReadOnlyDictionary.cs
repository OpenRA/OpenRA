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

namespace OpenRA
{
	/// <summary>
	/// A minimal read only dictionary interface for .NET 4
	/// </summary>
	/// <remarks>
	/// .NET 4.5 has an implementation built-in, this code is not meant to
	/// duplicate it but provide a compatible interface that can be replaced
	/// when we switch to .NET 4.5 or higher.
	/// </remarks>
	public interface IReadOnlyDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
	{
		int Count { get; }
		TValue this[TKey key] { get; }
		ICollection<TKey> Keys { get; }
		ICollection<TValue> Values { get; }

		bool ContainsKey(TKey key);
		bool TryGetValue(TKey key, out TValue value);
	}

	public static class ReadOnlyDictionary
	{
		public static IReadOnlyDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(this IDictionary<TKey, TValue> dict)
		{
			return dict as IReadOnlyDictionary<TKey, TValue> ?? new ReadOnlyDictionary<TKey, TValue>(dict);
		}
	}

	/// <summary>
	/// A minimal read only dictionary for .NET 4 implemented as a wrapper
	/// around an IDictionary.
	/// </summary>
	public class ReadOnlyDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
	{
		private readonly IDictionary<TKey, TValue> dict;

		public ReadOnlyDictionary()
			: this(new Dictionary<TKey, TValue>())
		{
		}

		public ReadOnlyDictionary(IDictionary<TKey, TValue> dict)
		{
			if (dict == null)
				throw new ArgumentNullException("dict");

			this.dict = dict;
		}

		#region IReadOnlyDictionary implementation
		public bool ContainsKey(TKey key)
		{
			return dict.ContainsKey(key);
		}

		public bool TryGetValue(TKey key, out TValue value)
		{
			return dict.TryGetValue(key, out value);
		}

		public int Count { get { return dict.Count; } }

		public TValue this[TKey key] { get { return dict[key]; } }

		public ICollection<TKey> Keys { get { return dict.Keys; } }

		public ICollection<TValue> Values { get { return dict.Values; } }
		#endregion

		#region IEnumerable implementation
		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			return dict.GetEnumerator();
		}
		#endregion

		#region IEnumerable implementation
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return dict.GetEnumerator();
		}
		#endregion
	}
}
