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
using System.Collections;
using System.Collections.Generic;

namespace OpenRa.Collections
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
