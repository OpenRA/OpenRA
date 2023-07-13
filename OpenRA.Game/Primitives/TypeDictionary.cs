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

namespace OpenRA.Primitives
{
	public class TypeDictionary : IEnumerable<object>
	{
		static readonly Func<Type, ITypeContainer> CreateTypeContainer = t =>
			(ITypeContainer)typeof(TypeContainer<>).MakeGenericType(t).GetConstructor(Type.EmptyTypes).Invoke(null);

		readonly Dictionary<Type, ITypeContainer> data = new();

		ITypeContainer InnerGet(Type t)
		{
			return data.GetOrAdd(t, CreateTypeContainer);
		}

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
			InnerGet(t).Add(val);
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
			return Get<T>(true);
		}

		public T GetOrDefault<T>()
		{
			return Get<T>(false);
		}

		T Get<T>(bool throwsIfMissing)
		{
			if (!data.TryGetValue(typeof(T), out var container))
			{
				if (throwsIfMissing)
					throw new InvalidOperationException($"TypeDictionary does not contain instance of type `{typeof(T)}`");
				return default;
			}

			var list = ((TypeContainer<T>)container).Objects;
			if (list.Count > 1)
				throw new InvalidOperationException($"TypeDictionary contains multiple instances of type `{typeof(T)}`");
			return list[0];
		}

		public IReadOnlyCollection<T> WithInterface<T>()
		{
			if (data.TryGetValue(typeof(T), out var container))
				return ((TypeContainer<T>)container).Objects;
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
			if (!data.TryGetValue(t, out var container))
				return;

			container.Remove(val);
			if (container.Count == 0)
				data.Remove(t);
		}

		public void TrimExcess()
		{
			data.TrimExcess();
			foreach (var t in data.Keys)
				InnerGet(t).TrimExcess();
		}

		public IEnumerator<object> GetEnumerator()
		{
			return WithInterface<object>().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		interface ITypeContainer
		{
			int Count { get; }
			void Add(object value);
			void Remove(object value);
			void TrimExcess();
		}

		sealed class TypeContainer<T> : ITypeContainer
		{
			public List<T> Objects { get; } = new List<T>();

			public int Count => Objects.Count;

			public void Add(object value)
			{
				Objects.Add((T)value);
			}

			public void Remove(object value)
			{
				Objects.Remove((T)value);
			}

			public void TrimExcess()
			{
				Objects.TrimExcess();
			}
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
