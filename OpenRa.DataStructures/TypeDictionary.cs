using System;
using System.Collections.Generic;
using System.Text;

namespace OpenRa
{
	public class TypeDictionary
	{
		Dictionary<Type, object> inner = new Dictionary<Type, object>();

		public void Add<T>( T val )
		{
			inner.Add( typeof( T ), val );
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

		public IEnumerable<T> WithInterface<T>()
		{
			foreach( var i in inner )
				if( i.Value is T )
					yield return (T)i.Value;
		}
	}
}
