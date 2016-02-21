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

		public T Get<T>()
		{
			return (T)Get(typeof(T), true);
		}

		public T GetOrDefault<T>()
		{
			var result = Get(typeof(T), false);
			if (result == null)
				return default(T);
			return (T)result;
		}

		object Get(Type t, bool throwsIfMissing)
		{
			List<object> ret;
			if (!data.TryGetValue(t, out ret))
			{
				if (throwsIfMissing)
					throw new InvalidOperationException("TypeDictionary does not contain instance of type `{0}`".F(t));
				return null;
			}

			if (ret.Count > 1)
				throw new InvalidOperationException("TypeDictionary contains multiple instances of type `{0}`".F(t));
			return ret[0];
		}

		public IEnumerable<T> WithInterface<T>()
		{
			List<object> objs;
			if (data.TryGetValue(typeof(T), out objs))
				return objs.Cast<T>();
			return new T[0];
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
			List<object> objs;
			if (!data.TryGetValue(t, out objs))
				return;
			objs.Remove(val);
			if (objs.Count == 0)
				data.Remove(t);
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
