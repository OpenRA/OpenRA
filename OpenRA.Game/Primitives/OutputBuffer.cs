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
using System.Collections.Generic;

namespace OpenRA.Primitives
{
	/// <summary>
	/// Destination where outputs should be written.
	/// </summary>
	public readonly struct OutputBuffer<T>(List<T> list)
	{
		public static implicit operator OutputBuffer<T>(List<T> list) => new(list);
		public void Add(T item) => list.Add(item);
		public void AddRange(IEnumerable<T> items) => list.AddRange(items);
		public void AddRange(params ReadOnlySpan<T> items) => list.AddRange(items);
		public void AddRange(T[] items) => list.AddRange(items.AsSpan());
	}
}
