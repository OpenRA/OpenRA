#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
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
	public class ObservableSortedDictionary<TKey, TValue> : ObservableDictionary<TKey, TValue>
	{
		public ObservableSortedDictionary(IComparer<TKey> comparer)
		{
			InnerDict = new SortedDictionary<TKey, TValue>(comparer);
		}

		public override void Add(TKey key, TValue value)
		{
			InnerDict.Add(key, value);
			FireOnRefresh();
		}
	}

	public class ObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IObservableCollection
	{
		protected IDictionary<TKey, TValue> InnerDict;

		public event Action<object> OnAdd = k => { };
		public event Action<object> OnRemove = k => { };
		public event Action<int> OnRemoveAt = i => { };
		public event Action<object, object> OnSet = (o, n) => { };
		public event Action OnRefresh = () => { };

		protected void FireOnRefresh()
		{
			OnRefresh();
		}

		protected ObservableDictionary() { }

		public ObservableDictionary(IEqualityComparer<TKey> comparer)
		{
			InnerDict = new Dictionary<TKey, TValue>(comparer);
		}

		public virtual void Add(TKey key, TValue value)
		{
			InnerDict.Add(key, value);
			OnAdd(key);
		}

		public bool Remove(TKey key)
		{
			var found = InnerDict.Remove(key);
			if (found)
				OnRemove(key);
			return found;
		}

		public bool ContainsKey(TKey key)
		{
			return InnerDict.ContainsKey(key);
		}

		public ICollection<TKey> Keys { get { return InnerDict.Keys; } }
		public ICollection<TValue> Values { get { return InnerDict.Values; } }

		public bool TryGetValue(TKey key, out TValue value)
		{
			return InnerDict.TryGetValue(key, out value);
		}

		public TValue this[TKey key]
		{
			get { return InnerDict[key]; }
			set { InnerDict[key] = value; }
		}

		public void Clear()
		{
			InnerDict.Clear();
			OnRefresh();
		}

		public int Count
		{
			get { return InnerDict.Count; }
		}

		public void Add(KeyValuePair<TKey, TValue> item)
		{
			Add(item.Key, item.Value);
		}

		public bool Contains(KeyValuePair<TKey, TValue> item)
		{
			return InnerDict.Contains(item);
		}

		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			InnerDict.CopyTo(array, arrayIndex);
		}

		public bool IsReadOnly
		{
			get { return InnerDict.IsReadOnly; }
		}

		public bool Remove(KeyValuePair<TKey, TValue> item)
		{
			return Remove(item.Key);
		}

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			return InnerDict.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return InnerDict.GetEnumerator();
		}

		public IEnumerable ObservedItems
		{
			get { return InnerDict.Keys; }
		}
	}
}
