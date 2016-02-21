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
using System.Collections.ObjectModel;

namespace OpenRA.Primitives
{
	public class ObservableCollection<T> : Collection<T>, IObservableCollection
	{
		public event Action<object> OnAdd = k => { };

		// TODO Workaround for https://github.com/OpenRA/OpenRA/issues/6101
		#pragma warning disable 67
		public event Action<object> OnRemove = k => { };
		#pragma warning restore
		public event Action<int> OnRemoveAt = i => { };
		public event Action<object, object> OnSet = (o, n) => { };
		public event Action OnRefresh = () => { };

		public ObservableCollection() { }
		public ObservableCollection(IList<T> list) : base(list) { }

		protected override void SetItem(int index, T item)
		{
			var old = this[index];
			base.SetItem(index, item);
			OnSet(old, item);
		}

		protected override void InsertItem(int index, T item)
		{
			base.InsertItem(index, item);
			OnAdd(item);
		}

		protected override void ClearItems()
		{
			base.ClearItems();
			OnRefresh();
		}

		protected override void RemoveItem(int index)
		{
			base.RemoveItem(index);
			OnRemoveAt(index);
		}

		public IEnumerable ObservedItems
		{
			get { return Items; }
		}
	}
}
