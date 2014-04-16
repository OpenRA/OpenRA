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
using System.Collections.ObjectModel;

namespace OpenRA.Primitives
{
	public class ObservableCollection<T> : Collection<T>, IObservableCollection
	{
		public event Action<object> OnAdd = k => { };
		public event Action<object> OnRemove = k => { };
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
