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
	public class ObservableList<T> : IList<T>, IObservableCollection
	{
		protected IList<T> innerList;

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

		public ObservableList()
		{
			innerList = new List<T>();
		}

		public virtual void Add(T item)
		{
			innerList.Add(item);
			OnAdd(this, item);
		}

		public bool Remove(T item)
		{
			var found = innerList.Remove(item);
			if (found)
				OnRemove(this, item);

			return found;
		}

		public void Clear()
		{
			innerList.Clear();
			OnRefresh(this);
		}

		public void Insert(int index, T item)
		{
			innerList.Insert(index, item);
			OnRefresh(this);
		}

		public int Count => innerList.Count;
		public int IndexOf(T item) { return innerList.IndexOf(item); }
		public bool Contains(T item) { return innerList.Contains(item); }

		public void RemoveAt(int index)
		{
			innerList.RemoveAt(index);
			OnRemoveAt(this, index);
		}

		public T this[int index]
		{
			get => innerList[index];

			set
			{
				var oldValue = innerList[index];
				innerList[index] = value;
				OnSet(this, oldValue, value);
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

		public IEnumerable ObservedItems => innerList;

		public bool IsReadOnly => innerList.IsReadOnly;
	}
}
