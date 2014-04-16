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

namespace OpenRA.Primitives
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

		public bool Contains( T obj ) { return data.ContainsKey(obj); }

		public Set( params T[] ts )
		{
			foreach( var t in ts )
				Add(t);
		}
	}
}
