#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
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
		Dictionary<Type, object> dataSingular = new Dictionary<Type, object>();
		Dictionary<Type, List<object>> dataMultiple = new Dictionary<Type, List<object>>();

		public void Add( object val )
		{
			var t = val.GetType();

			foreach( var i in t.GetInterfaces() )
				InnerAdd( i, val );
			foreach( var tt in t.BaseTypes() )
				InnerAdd( tt, val );
		}

		void InnerAdd( Type t, object val )
		{
			List<object> objs;
			object obj;

			if( dataMultiple.TryGetValue( t, out objs ) )
				objs.Add( val );
			else if( dataSingular.TryGetValue( t, out obj ) )
			{
				dataSingular.Remove( t );
				dataMultiple.Add( t, new List<object> { obj, val } );
			}
			else
				dataSingular.Add( t, val );
		}

		public bool Contains<T>()
		{
			return dataSingular.ContainsKey(typeof(T)) || dataMultiple.ContainsKey(typeof(T));
		}

		public T Get<T>()
		{
			if (dataMultiple.ContainsKey(typeof(T)))
				throw new InvalidOperationException("TypeDictionary contains multiple instance of type `{0}`".F(typeof(T)));

			object ret;
			if (!dataSingular.TryGetValue(typeof(T), out ret))
				throw new InvalidOperationException("TypeDictionary does not contain instance of type `{0}`".F(typeof(T)));
			return (T)ret;
		}

		public T GetOrDefault<T>()
		{
			return (T)GetOrDefault(typeof(T));
		}

		public object GetOrDefault(Type t)
		{
			if (dataMultiple.ContainsKey(t))
				throw new InvalidOperationException("TypeDictionary contains multiple instances of type `{0}`".F(t));

			object ret;
			if (!dataSingular.TryGetValue(t, out ret))
				return null;
			return ret;
		}

		public IEnumerable<T> WithInterface<T>()
		{
			List<object> objs;
			object obj;

			if( dataMultiple.TryGetValue( typeof( T ), out objs ) )
				return objs.Cast<T>();
			else if( dataSingular.TryGetValue( typeof( T ), out obj ) )
				return new T[] { (T)obj };
			else
				return new T[ 0 ];
		}

		public IEnumerator GetEnumerator()
		{
			return WithInterface<object>().GetEnumerator();
		}
	}

	public static class TypeExts
	{
		public static IEnumerable<Type> BaseTypes( this Type t )
		{
			while( t != null )
			{
				yield return t;
				t = t.BaseType;
			}
		}
	}
}
