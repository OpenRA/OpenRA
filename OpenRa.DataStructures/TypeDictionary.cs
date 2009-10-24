using System;
using System.Collections.Generic;
using System.Text;

namespace OpenRa
{
	public class TypeDictionary
	{
		Dictionary<Type, object> inner = new Dictionary<Type, object>();

		public void Add( Type t, object val )
		{
			inner.Add( t, val );
		}

		public void Add<T>( T val )
		{
			Add( typeof( T ), val );
		}

		public void Remove<T>()
		{
			inner.Remove( typeof( T ) );
		}

		public bool Contains<T>()
		{
			return inner.ContainsKey( typeof( T ) );
		}

		public T Get<T>()
		{
			return (T)inner[ typeof( T ) ];
		}

		public T GetOrDefault<T>()
		{
			object o = null;
			inner.TryGetValue(typeof(T), out o);
			return (T)o;
		}

		public IEnumerable<T> WithInterface<T>()
		{
			foreach( var i in inner )
				if( i.Value is T )
					yield return (T)i.Value;
		}
	}
}
