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
using System.Linq;

namespace OpenRA.Primitives
{
	public class TypeDictionary : IEnumerable
	{
		static readonly Func<Type, List<object>> CreateList = type => new List<object>();
		readonly Dictionary<Type, List<object>> data = new Dictionary<Type, List<object>>();

		public void Add(object val)
		{
			var t = val.GetType();

			foreach (var i in t.GetInterfaces())
				InnerAdd(i, val);
			foreach (var tt in t.BaseTypes())
				InnerAdd(tt, val);
		}

		void InnerAdd(Type t, object val)
		{
			data.GetOrAdd(t, CreateList).Add(val);
		}

		public bool Contains<T>()
		{
			return data.ContainsKey(typeof(T));
		}

		public bool Contains(Type t)
		{
			return data.ContainsKey(t);
		}

		public T Get<T>()
		{
			return (T)Get(typeof(T), true);
		}

		public T GetOrDefault<T>()
		{
			var result = Get(typeof(T), false);
			if (result == null)
				return default;
			return (T)result;
		}

		object Get(Type t, bool throwsIfMissing)
		{
			if (!data.TryGetValue(t, out var ret))
			{
				if (throwsIfMissing)
					throw new InvalidOperationException($"TypeDictionary does not contain instance of type `{t}`");
				return null;
			}

			if (ret.Count > 1)
				throw new InvalidOperationException($"TypeDictionary contains multiple instances of type `{t}`");
			return ret[0];
		}

		public IEnumerable<T> WithInterface<T>()
		{
			if (data.TryGetValue(typeof(T), out var objs))
				return objs.Cast<T>();
			return Array.Empty<T>();
		}

		public void Remove<T>(T val)
		{
			var t = val.GetType();

			foreach (var i in t.GetInterfaces())
				InnerRemove(i, val);
			foreach (var tt in t.BaseTypes())
				InnerRemove(tt, val);
		}

		void InnerRemove(Type t, object val)
		{
			if (!data.TryGetValue(t, out var objs))
				return;
			objs.Remove(val);
			if (objs.Count == 0)
				data.Remove(t);
		}

		public void TrimExcess()
		{
			foreach (var objs in data.Values)
				objs.TrimExcess();
		}

		public IEnumerator GetEnumerator()
		{
			return WithInterface<object>().GetEnumerator();
		}
	}

	public static class TypeExts
	{
		public static IEnumerable<Type> BaseTypes(this Type t)
		{
			while (t != null)
			{
				yield return t;
				t = t.BaseType;
			}
		}
	}
}
