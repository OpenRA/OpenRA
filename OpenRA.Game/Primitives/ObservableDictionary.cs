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

		public event Action<IObservableCollection, object> OnAdd = (x, k) => { };
		public event Action<IObservableCollection, object> OnRemove = (x, k) => { };

		// TODO Workaround for https://github.com/OpenRA/OpenRA/issues/6101
		#pragma warning disable 67
		public event Action<IObservableCollection, int> OnRemoveAt = (x, i) => { };
		public event Action<IObservableCollection, object, object> OnSet = (x, o, n) => { };
		#pragma warning restore
		public event Action<IObservableCollection> OnRefresh = x => { };

		protected void FireOnRefresh()
		{
			OnRefresh(this);
		}

		protected ObservableDictionary() { }

		public ObservableDictionary(IEqualityComparer<TKey> comparer)
		{
			innerDict = new Dictionary<TKey, TValue>(comparer);
		}

		public virtual void Add(TKey key, TValue value)
		{
			innerDict.Add(key, value);
			OnAdd(this, key);
		}

		public bool Remove(TKey key)
		{
			var found = innerDict.Remove(key);
			if (found)
				OnRemove(this, key);
			return found;
		}

		public bool ContainsKey(TKey key)
		{
			return innerDict.ContainsKey(key);
		}

		public ICollection<TKey> Keys => innerDict.Keys;
		public ICollection<TValue> Values => innerDict.Values;

		public bool TryGetValue(TKey key, out TValue value)
		{
			return innerDict.TryGetValue(key, out value);
		}

		public TValue this[TKey key]
		{
			get => innerDict[key];
			set => innerDict[key] = value;
		}

		public void Clear()
		{
			innerDict.Clear();
			OnRefresh(this);
		}

		public int Count => innerDict.Count;

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

		public bool IsReadOnly => innerDict.IsReadOnly;

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

		public IEnumerable ObservedItems => innerDict.Keys;
	}
}
