using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IjwFramework.Collections;

namespace OpenRa
{
	public class TypeDictionary
	{
		Cache<Type, List<object>> innerInherit = new Cache<Type, List<object>>( _ => new List<object>() );

		public void Add( object val )
		{
			var t = val.GetType();

			foreach( var i in t.GetInterfaces() )
				innerInherit[ i ].Add( val );
			foreach( var tt in t.BaseTypes() )
				innerInherit[ tt ].Add( val );
		}

		public bool Contains<T>()
		{
			return innerInherit.Keys.Contains( typeof( T ) );
		}

		public T Get<T>()
		{
			var l = innerInherit[ typeof( T ) ];
			if( l.Count == 1 )
				return (T)l[ 0 ];
			else if( l.Count == 0 )
				throw new InvalidOperationException( string.Format( "TypeDictionary does not contain instance of type `{0}`", typeof( T ) ) );
			else
				throw new InvalidOperationException( string.Format( "TypeDictionary contains multiple instance of type `{0}`", typeof( T ) ) );
		}

		public T GetOrDefault<T>()
		{
			var l = innerInherit[ typeof( T ) ];
			if( l.Count == 1 )
				return (T)l[ 0 ];
			else if( l.Count == 0 )
				return default( T );
			else
				throw new InvalidOperationException( string.Format( "TypeDictionary contains multiple instance of type `{0}`", typeof( T ) ) );
		}

		public IEnumerable<T> WithInterface<T>()
		{
			foreach( var i in innerInherit[ typeof( T ) ] )
				yield return (T)i;
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
