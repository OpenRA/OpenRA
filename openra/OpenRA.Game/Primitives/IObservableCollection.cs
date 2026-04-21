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

namespace OpenRA.Primitives
{
	public interface IObservableCollection
	{
		event Action<IObservableCollection, object> OnAdd;
		event Action<IObservableCollection, object> OnRemove;
		event Action<IObservableCollection, int> OnRemoveAt;
		event Action<IObservableCollection, object, object> OnSet;
		event Action<IObservableCollection> OnRefresh;
		IEnumerable ObservedItems { get; }
	}
}
