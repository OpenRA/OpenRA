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
	public class ObservableList<T> : IList<T>, IObservableCollection
	{
		protected IList<T> innerList;

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

		public ObservableList()
		{
			innerList = new List<T>();
		}

		public virtual void Add(T item)
		{
			innerList.Add(item);
			OnAdd(item);
		}

		public bool Remove(T item)
		{
			var found = innerList.Remove(item);
			if (found)
				OnRemove(item);

			return found;
		}

		public void Clear()
		{
			innerList.Clear();
			OnRefresh();
		}

		public void Insert(int index, T item)
		{
			innerList.Insert(index, item);
			OnRefresh();
		}

		public int Count { get { return innerList.Count; } }
		public int IndexOf(T item) { return innerList.IndexOf(item); }
		public bool Contains(T item) { return innerList.Contains(item); }

		public void RemoveAt(int index)
		{
			innerList.RemoveAt(index);
			OnRemoveAt(index);
		}

		public T this[int index]
		{
			get
			{
				return innerList[index];
			}

			set
			{
				var oldValue = innerList[index];
				innerList[index] = value;
				OnSet(oldValue, value);
			}
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			innerList.CopyTo(array, arrayIndex);
		}

		public IEnumerator<T> GetEnumerator()
		{
			return innerList.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return innerList.GetEnumerator();
		}

		public IEnumerable ObservedItems
		{
			get { return innerList; }
		}

		public bool IsReadOnly
		{
			get { return innerList.IsReadOnly; }
		}
	}
}
