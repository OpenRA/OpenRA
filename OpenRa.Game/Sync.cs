using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace OpenRa.Game
{
	class SyncAttribute : Attribute { }

	static class Sync
	{
		public static int CalculateSyncHash( object obj )
		{
			int hash = 0; // TODO: start with a more interesting initial value.

			// TODO: cache the Syncable fields; maybe use DynamicMethod to make this fast?
			// FIXME: does GetFields even give fields in a well-defined order?
			const BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
			foreach( var field in obj.GetType().GetFields( bf ).Where( x => x.GetCustomAttributes( typeof( SyncAttribute ), true ).Length != 0 ) )
			{
				if( field.FieldType == typeof( int ) )
					hash ^= (int)field.GetValue( obj );

				else if( field.FieldType == typeof( Actor ) )
				{
					var a = (Actor)field.GetValue( obj );
					if( a != null )
						hash ^= (int)( a.ActorID << 16 );
				}
				else if( field.FieldType == typeof( TypeDictionary ) )
				{
					foreach( var o in (TypeDictionary)field.GetValue( obj ) )
						hash += CalculateSyncHash( o );
				}
				else if( field.FieldType == typeof( bool ) )
					hash ^= (bool)field.GetValue( obj ) ? 0xaaa : 0x555;

				else if( field.FieldType == typeof( int2 ) )
				{
					var i2 = (int2)field.GetValue( obj );
					hash ^= ( ( i2.X * 5 ) ^ ( i2.Y * 3 ) ) / 4;
				}
				else if( field.FieldType == typeof( Player ) )
				{
					var p = (Player)field.GetValue( obj );
					if( p != null )
					hash ^= p.Index * 0x567;
				}
				else
					throw new NotImplementedException( "SyncAttribute on unhashable field" );
			}

			return hash;
		}
	}
}
