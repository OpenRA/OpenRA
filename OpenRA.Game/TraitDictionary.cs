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
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using System.Diagnostics;

namespace OpenRA
{
	class TraitDictionary
	{
		Dictionary<Type, ITraitContainer> traits = new Dictionary<Type, ITraitContainer>();

		ITraitContainer InnerGet( Type t )
		{
			return traits.GetOrAdd( t, CreateTraitContainer );
		}

		static ITraitContainer CreateTraitContainer( Type t )
		{
			return (ITraitContainer)typeof( TraitContainer<> ).MakeGenericType( t )
				.GetConstructor( new Type[ 0 ] ).Invoke( new object[ 0 ] );
		}

		public void Add( Actor actor, object val )
		{
			var t = val.GetType();

			foreach( var i in t.GetInterfaces() )
				InnerAdd( actor, i, val );
			foreach( var tt in t.BaseTypes() )
				InnerAdd( actor, tt, val );
		}

		void InnerAdd( Actor actor, Type t, object val )
		{
			InnerGet( t ).Add( actor, val );
		}

		public bool Contains<T>( Actor actor )
		{
			return ( (TraitContainer<T>)InnerGet( typeof( T ) ) ).GetMultiple( actor ).Count() != 0;
		}

		public T Get<T>( Actor actor )
		{
			return ( (TraitContainer<T>)InnerGet( typeof( T ) ) ).Get( actor );
		}

		public T GetOrDefault<T>( Actor actor )
		{
			return ( (TraitContainer<T>)InnerGet( typeof( T ) ) ).GetOrDefault( actor );
		}

		public IEnumerable<T> WithInterface<T>( Actor actor )
		{
			return ( (TraitContainer<T>)InnerGet( typeof( T ) ) ).GetMultiple( actor );
		}

		interface ITraitContainer
		{
			void Add( Actor actor, object trait );
		}

		class TraitContainer<T> : ITraitContainer
		{
			List<uint> actors = new List<uint>();
			List<T> traits = new List<T>();

			public void Add( Actor actor, object trait )
			{
				var insertIndex = actors.BinarySearchMany( actor.ActorID + 1 );
				actors.Insert( insertIndex, actor.ActorID );
				traits.Insert( insertIndex, (T)trait );
			}

			public T Get( Actor actor )
			{
				var index = actors.BinarySearchMany( actor.ActorID );
				if( index >= actors.Count || actors[ index ] != actor.ActorID )
					throw new InvalidOperationException( string.Format( "TraitDictionary does not contain instance of type `{0}`", typeof( T ) ) );
				else if( index + 1 < actors.Count && actors[ index + 1 ] == actor.ActorID )
					throw new InvalidOperationException( string.Format( "TraitDictionary contains multiple instance of type `{0}`", typeof( T ) ) );
				else
					return traits[ index ];
			}

			public T GetOrDefault( Actor actor )
			{
				var index = actors.BinarySearchMany( actor.ActorID );
				if( index >= actors.Count || actors[ index ] != actor.ActorID )
					return default( T );
				else if( index + 1 < actors.Count && actors[ index + 1 ] == actor.ActorID )
					throw new InvalidOperationException( string.Format( "TraitDictionary contains multiple instance of type `{0}`", typeof( T ) ) );
				else return traits[ index ];
			}

			public IEnumerable<T> GetMultiple( Actor actor )
			{
				var index = actors.BinarySearchMany( actor.ActorID );
				while( index < actors.Count && actors[ index ] == actor.ActorID )
				{
					yield return traits[ index ];
					++index;
				}
			}
		}
	}

	static class ListExts
	{
		public static int BinarySearchMany<T>( this List<T> list, T searchFor )
			where T : IComparable<T>
		{
			int start = 0;
			int end = list.Count;
			int mid = 0;
			while( start != end )
			{
				mid = ( start + end ) / 2;
				var c = list[ mid ].CompareTo( searchFor );
				if( c < 0 )
					start = mid + 1;
				else
					end = mid;
			}
			return start;
		}
	}
}
