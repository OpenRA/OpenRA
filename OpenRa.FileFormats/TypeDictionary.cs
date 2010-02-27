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
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.FileFormats
{
	public class TypeDictionary
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
			return dataSingular.ContainsKey( typeof( T ) ) || dataMultiple.ContainsKey( typeof( T ) );
		}

		public T Get<T>()
		{
			if( dataMultiple.ContainsKey( typeof( T ) ) )
				throw new InvalidOperationException( string.Format( "TypeDictionary contains multiple instance of type `{0}`", typeof( T ) ) );

			object ret;
			if( !dataSingular.TryGetValue( typeof( T ), out ret ) )
				throw new InvalidOperationException(string.Format("TypeDictionary does not contain instance of type `{0}`", typeof(T)));
			return (T)ret;
		}

		public T GetOrDefault<T>()
		{
			if( dataMultiple.ContainsKey( typeof( T ) ) )
				throw new InvalidOperationException( string.Format( "TypeDictionary contains multiple instance of type `{0}`", typeof( T ) ) );

			object ret;
			if( !dataSingular.TryGetValue( typeof( T ), out ret ) )
				return default( T );
			return (T)ret;
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

		public IEnumerator<object> GetEnumerator()
		{
			return WithInterface<object>().GetEnumerator();
		}
	}

	static class TypeExts
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
