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
using System.Collections;
using System.Collections.Generic;

namespace OpenRA.Primitives
{
	public class ObservableSortedDictionary<TKey, TValue> : ObservableDictionary<TKey, TValue>
	{
		public ObservableSortedDictionary(IComparer<TKey> comparer)
		{
			innerDict = new SortedDictionary<TKey, TValue>(comparer);
		}

		public override void Add(TKey key, TValue value)
		{
			innerDict.Add(key, value);
			FireOnRefresh();
		}
	}

	public class ObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IObservableCollection
	{
		protected IDictionary<TKey, TValue> innerDict;

		public event Action<object> OnAdd = k => { };
		public event Action<object> OnRemove = k => { };

		// TODO Workaround for https://github.com/OpenRA/OpenRA/issues/6101
		#pragma warning disable 67
		public event Action<int> OnRemoveAt = i => { };
		public event Action<object, object> OnSet = (o, n) => { };
		#pragma warning restore
		public event Action OnRefresh = () => { };

		protected void FireOnRefresh()
		{
			OnRefresh();
		}

		protected ObservableDictionary() { }

		public ObservableDictionary(IEqualityComparer<TKey> comparer)
		{
			innerDict = new Dictionary<TKey, TValue>(comparer);
		}

		public virtual void Add(TKey key, TValue value)
		{
			innerDict.Add(key, value);
			OnAdd(key);
		}

		public bool Remove(TKey key)
		{
			var found = innerDict.Remove(key);
			if (found)
				OnRemove(key);
			return found;
		}

		public bool ContainsKey(TKey key)
		{
			return innerDict.ContainsKey(key);
		}

		public ICollection<TKey> Keys { get { return innerDict.Keys; } }
		public ICollection<TValue> Values { get { return innerDict.Values; } }

		public bool TryGetValue(TKey key, out TValue value)
		{
			return innerDict.TryGetValue(key, out value);
		}

		public TValue this[TKey key]
		{
			get { return innerDict[key]; }
			set { innerDict[key] = value; }
		}

		public void Clear()
		{
			innerDict.Clear();
			OnRefresh();
		}

		public int Count
		{
			get { return innerDict.Count; }
		}

		public void Add(KeyValuePair<TKey, TValue> item)
		{
			Add(item.Key, item.Value);
		}

		public bool Contains(KeyValuePair<TKey, TValue> item)
		{
			return innerDict.Contains(item);
		}

		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			innerDict.CopyTo(array, arrayIndex);
		}

		public bool IsReadOnly
		{
			get { return innerDict.IsReadOnly; }
		}

		public bool Remove(KeyValuePair<TKey, TValue> item)
		{
			return Remove(item.Key);
		}

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			return innerDict.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return innerDict.GetEnumerator();
		}

		public IEnumerable ObservedItems
		{
			get { return innerDict.Keys; }
		}
	}
}
