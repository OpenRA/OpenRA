#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
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
	public class TraitTypeIndexMap
	{
		private static Dictionary<Type, int> map = new Dictionary<Type, int>();

		public static int RegisterType(Type t) {
			int index;
			if (map.TryGetValue(t, out index)) {
				return index;
			}

			index = map.Count + 1;
			map.Add(t, index);
			return index;
		}

		public static int FindType(Type t) {
			int index;
			if (map.TryGetValue(t, out index)) {
				return index;
			}

			return 0;
		}

		public static int GetCount() {
			return map.Count;
		}

		public static Type GetType(int index) {
			if (index > 0 && index < map.Count) {
				foreach (var t in map) {
					if (t.Value == index)
						return t.Key;
				}
			}

			return null;
		}
	}

	// Index of type in array
	public class TraitTypeIndex<T>
	{
		private static int typeIndex = 0;
		public static int GetTypeIndex() {
			if (typeIndex == 0) {
				typeIndex = TraitTypeIndexMap.RegisterType(typeof(T));
			}

			return typeIndex;
		}

		public static bool IsRegistered() {
			return typeIndex != 0;
		}
	}
}
