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
using System.Collections.ObjectModel;

namespace OpenRA.Primitives
{
	public class ObservableCollection<T> : Collection<T>, IObservableCollection
	{
		public event Action<IObservableCollection, object> OnAdd = (x, k) => { };

		// TODO Workaround for https://github.com/OpenRA/OpenRA/issues/6101
		#pragma warning disable 67
		public event Action<IObservableCollection, object> OnRemove = (x, k) => { };
		#pragma warning restore
		public event Action<IObservableCollection, int> OnRemoveAt = (x, i) => { };
		public event Action<IObservableCollection, object, object> OnSet = (x, o, n) => { };
		public event Action<IObservableCollection> OnRefresh = x => { };

		public ObservableCollection() { }
		public ObservableCollection(IList<T> list)
			: base(list) { }

		protected override void SetItem(int index, T item)
		{
			var old = this[index];
			base.SetItem(index, item);
			OnSet(this, old, item);
		}

		protected override void InsertItem(int index, T item)
		{
			base.InsertItem(index, item);
			OnAdd(this, item);
		}

		protected override void ClearItems()
		{
			base.ClearItems();
			OnRefresh(this);
		}

		protected override void RemoveItem(int index)
		{
			base.RemoveItem(index);
			OnRemoveAt(this, index);
		}

		public IEnumerable ObservedItems => Items;
	}
}
