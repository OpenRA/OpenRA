#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;

namespace OpenRA.Collections
{
	public class Set<T> : IEnumerable<T>
	{
		Dictionary<T, bool> data = new Dictionary<T, bool>();

		public void Add( T obj )
		{
			data.Add( obj, false );
			if( OnAdd != null )
				OnAdd( obj );
		}

		public void Remove( T obj )
		{
			data.Remove( obj );
			if( OnRemove != null )
				OnRemove( obj );
		}

		public event Action<T> OnAdd;
		public event Action<T> OnRemove;

		public IEnumerator<T> GetEnumerator()
		{
			return data.Keys.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	public class CachedView<T,U> : Set<U>
	{
		public CachedView( Set<T> set, Func<T, bool> include, Func<T, U> store )
			: this( set, include, x => new[] { store( x ) } )
		{
		}

		public CachedView( Set<T> set, Func<T,bool> include, Func<T,IEnumerable<U>> store )
		{
			foreach( var t in set )
				if( include( t ) )
					store( t ).Do( x => Add( x ) );

			set.OnAdd += obj => { if( include( obj ) ) store( obj ).Do( x => Add( x ) ); };
			set.OnRemove += obj => { if( include( obj ) ) store( obj ).Do( x => Remove( x ) ); };
		}
	}
}
