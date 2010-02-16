#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;

namespace OpenRa.FileFormats
{
	public class Cache<T, U> : IEnumerable<KeyValuePair<T, U>>
	{
		Dictionary<T, U> hax = new Dictionary<T, U>();
		Func<T,U> loader;

		public Cache(Func<T,U> loader)
		{
			if (loader == null)
				throw new ArgumentNullException();

			this.loader = loader;
		}

		public U this[T key]
		{
			get
			{
				U result;
				if (!hax.TryGetValue(key, out result))
					hax.Add(key, result = loader(key));

				return result;
			}
		}

		public IEnumerator<KeyValuePair<T, U>> GetEnumerator() { return hax.GetEnumerator(); }

		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

		public IEnumerable<T> Keys { get { return hax.Keys; } }
		public IEnumerable<U> Values { get { return hax.Values; } }
	}
}
