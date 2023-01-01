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

namespace OpenRA
{
	public class ActorInfoDictionary : IReadOnlyDictionary<string, ActorInfo>
	{
		readonly Dictionary<string, ActorInfo> dict;

		public ActorInfoDictionary(IReadOnlyDictionary<string, ActorInfo> dict)
		{
			if (dict == null)
				throw new ArgumentNullException(nameof(dict));

			this.dict = new Dictionary<string, ActorInfo>(dict);
		}

		public bool ContainsKey(string key) => dict.ContainsKey(key);

		public bool TryGetValue(string key, out ActorInfo value) => dict.TryGetValue(key, out value);

		public int Count => dict.Count;

		public ActorInfo this[string key] => dict[key];
		public ActorInfo this[SystemActors key] => dict[key.ToString().ToLowerInvariant()];

		IEnumerable<string> IReadOnlyDictionary<string, ActorInfo>.Keys => dict.Keys;
		IEnumerable<ActorInfo> IReadOnlyDictionary<string, ActorInfo>.Values => dict.Values;

		public ICollection<string> Keys => dict.Keys;
		public ICollection<ActorInfo> Values => dict.Values;

		public IEnumerator<KeyValuePair<string, ActorInfo>> GetEnumerator() => dict.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => dict.GetEnumerator();
	}
}
