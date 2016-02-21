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
using System.Collections.Generic;

namespace OpenRA
{
	/// <summary>
	/// A minimal read only list interface for .NET 4
	/// </summary>
	/// <remarks>
	/// .NET 4.5 has an implementation built-in, this code is not meant to
	/// duplicate it but provide a compatible interface that can be replaced
	/// when we switch to .NET 4.5 or higher.
	/// </remarks>
	public interface IReadOnlyList<out T> : IEnumerable<T>
	{
		int Count { get; }
		T this[int index] { get; }
	}

	public static class ReadOnlyList
	{
		public static IReadOnlyList<T> AsReadOnly<T>(this IList<T> list)
		{
			return list as IReadOnlyList<T> ?? new ReadOnlyList<T>(list);
		}
	}

	/// <summary>
	/// A minimal read only list for .NET 4 implemented as a wrapper
	/// around an IList.
	/// </summary>
	public class ReadOnlyList<T> : IReadOnlyList<T>
	{
		private readonly IList<T> list;

		public ReadOnlyList()
			: this(new List<T>())
		{
		}

		public ReadOnlyList(IList<T> list)
		{
			if (list == null)
				throw new ArgumentNullException("list");

			this.list = list;
		}

		#region IEnumerable implementation
		public IEnumerator<T> GetEnumerator()
		{
			return list.GetEnumerator();
		}
		#endregion

		#region IEnumerable implementation
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return list.GetEnumerator();
		}
		#endregion

		#region IReadOnlyList implementation
		public int Count { get { return list.Count; } }

		public T this[int index] { get { return list[index]; } }
		#endregion
	}
}
