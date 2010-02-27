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
using OpenRA.FileFormats;

namespace OpenRA.GameRules
{
	public class InfoLoader<T> : IEnumerable<KeyValuePair<string, T>>
	{
		readonly Dictionary<string, T> infos = new Dictionary<string, T>();

		public InfoLoader(params Pair<string, Func<string,T>>[] srcs)
		{
			foreach (var src in srcs)
				foreach (var name in Rules.Categories[src.First])
				{
					var t = src.Second(name);
					FieldLoader.Load(t, Rules.AllRules.GetSection(name));
					infos[name] = t;
				}
		}

		public T this[string name]
		{
			get { return infos[name.ToLowerInvariant()]; }
		}

		public IEnumerator<KeyValuePair<string, T>> GetEnumerator() { return infos.GetEnumerator(); }
		IEnumerator IEnumerable.GetEnumerator() { return infos.GetEnumerator(); }
	}
}
