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

		public void AddTrait( Actor actor, object val )
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
			if( actor.Destroyed )
				throw new InvalidOperationException( "Attempted to get trait from destroyed object ({0})".F( actor.ToString() ) );
			return ( (TraitContainer<T>)InnerGet( typeof( T ) ) ).GetMultiple( actor.ActorID ).Count() != 0;
		}

		public T Get<T>( Actor actor )
		{
			if( actor.Destroyed )
				throw new InvalidOperationException( "Attempted to get trait from destroyed object ({0})".F( actor.ToString() ) );
			return ( (TraitContainer<T>)InnerGet( typeof( T ) ) ).Get( actor.ActorID );
		}

		public T GetOrDefault<T>( Actor actor )
		{
			if( actor.Destroyed )
				throw new InvalidOperationException( "Attempted to get trait from destroyed object ({0})".F( actor.ToString() ) );
			return ( (TraitContainer<T>)InnerGet( typeof( T ) ) ).GetOrDefault( actor.ActorID );
		}

		public IEnumerable<T> WithInterface<T>( Actor actor )
		{
			if( actor.Destroyed )
				throw new InvalidOperationException( "Attempted to get trait from destroyed object ({0})".F( actor.ToString() ) );
			return ( (TraitContainer<T>)InnerGet( typeof( T ) ) ).GetMultiple( actor.ActorID );
		}

		public IEnumerable<TraitPair<T>> ActorsWithTraitMultiple<T>( World world )
		{
			return ( (TraitContainer<T>)InnerGet( typeof( T ) ) ).All();//.Where( x => x.Actor.IsInWorld );
		}

		public void RemoveActor( Actor a )
		{
			foreach( var t in traits )
				t.Value.RemoveActor( a.ActorID );
		}

		interface ITraitContainer
		{
			void Add( Actor actor, object trait );
			void RemoveActor( uint actor );
		}

		class TraitContainer<T> : ITraitContainer
		{
			List<Actor> actors = new List<Actor>();
			List<T> traits = new List<T>();

			public void Add( Actor actor, object trait )
			{
				var insertIndex = actors.BinarySearchMany( actor.ActorID + 1 );
				actors.Insert( insertIndex, actor );
				traits.Insert( insertIndex, (T)trait );
			}

			public T Get( uint actor )
			{
				var index = actors.BinarySearchMany( actor );
				if( index >= actors.Count || actors[ index ].ActorID != actor )
					throw new InvalidOperationException( string.Format( "TraitDictionary does not contain instance of type `{0}`", typeof( T ) ) );
				else if( index + 1 < actors.Count && actors[ index + 1 ].ActorID == actor )
					throw new InvalidOperationException( string.Format( "TraitDictionary contains multiple instance of type `{0}`", typeof( T ) ) );
				else
					return traits[ index ];
			}

			public T GetOrDefault( uint actor )
			{
				var index = actors.BinarySearchMany( actor );
				if( index >= actors.Count || actors[ index ].ActorID != actor )
					return default( T );
				else if( index + 1 < actors.Count && actors[ index + 1 ].ActorID == actor )
					throw new InvalidOperationException( string.Format( "TraitDictionary contains multiple instance of type `{0}`", typeof( T ) ) );
				else return traits[ index ];
			}

			public IEnumerable<T> GetMultiple( uint actor )
			{
				var index = actors.BinarySearchMany( actor );
				while( index < actors.Count && actors[ index ].ActorID == actor )
				{
					yield return traits[ index ];
					++index;
				}
			}

			public IEnumerable<TraitPair<T>> All()
			{
				for( int i = 0 ; i < actors.Count ; i++ )
					yield return new TraitPair<T> { Actor = actors[ i ], Trait = traits[ i ] };
			}

			public void RemoveActor( uint actor )
			{
				for( int i = actors.Count - 1 ; i >= 0 ; i-- )
				{
					if( actors[ i ].ActorID == actor )
					{
						actors.RemoveAt( i );
						traits.RemoveAt( i );
					}
				}
			}
		}
	}

	static class ListExts
	{
		public static int BinarySearchMany( this List<Actor> list, uint searchFor )
		{
			int start = 0;
			int end = list.Count;
			int mid = 0;
			while( start != end )
			{
				mid = ( start + end ) / 2;
				var c = list[ mid ].ActorID.CompareTo( searchFor );
				if( c < 0 )
					start = mid + 1;
				else
					end = mid;
			}
			return start;
		}
	}
}
