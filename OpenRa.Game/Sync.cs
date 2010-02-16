using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using OpenRa.FileFormats;

namespace OpenRa
{
	public class SyncAttribute : Attribute { }

	static class Sync
	{
		static Cache<Type, Func<object, int>> hashFuncCache = new Cache<Type, Func<object, int>>( t => GenerateHashFunc( t ) );

		public static int CalculateSyncHash( object obj )
		{
			return hashFuncCache[ obj.GetType() ]( obj );
		}

		public static Func<object,int> GenerateHashFunc( Type t )
		{
			var d = new DynamicMethod( "hash_{0}".F( t.Name ), typeof( int ), new Type[] { typeof( object ) }, t );
			var il = d.GetILGenerator();
			var this_ = il.DeclareLocal( t ).LocalIndex;
			il.Emit( OpCodes.Ldarg_0 );
			il.Emit( OpCodes.Castclass, t );
			il.Emit( OpCodes.Stloc, this_ );
			il.Emit( OpCodes.Ldc_I4_0 );

			const BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
			foreach( var field in t.GetFields( bf ).Where( x => x.GetCustomAttributes( typeof( SyncAttribute ), true ).Length != 0 ) )
			{
				il.Emit( OpCodes.Ldloc, this_ );
				il.Emit( OpCodes.Ldfld, field );

				if( field.FieldType == typeof( int ) )
				{
					il.Emit( OpCodes.Xor );
				}
				else if( field.FieldType == typeof( bool ) )
				{
					var l = il.DefineLabel();
					il.Emit( OpCodes.Ldc_I4, 0xaaa );
					il.Emit( OpCodes.Brtrue, l );
					il.Emit( OpCodes.Pop );
					il.Emit( OpCodes.Ldc_I4, 0x555 );
					il.MarkLabel( l );
					il.Emit( OpCodes.Xor );
				}
				else if( field.FieldType == typeof( int2 ) )
				{
					il.EmitCall( OpCodes.Call, ( (Func<int2, int>)hash_int2 ).Method, null );
					il.Emit( OpCodes.Xor );
				}
				else if( field.FieldType == typeof( TypeDictionary ) )
				{
					il.EmitCall( OpCodes.Call, ( (Func<TypeDictionary, int>)hash_tdict ).Method, null );
					il.Emit( OpCodes.Xor );
				}
				else if( field.FieldType == typeof( Actor ) )
				{
					il.EmitCall( OpCodes.Call, ( (Func<Actor, int>)hash_actor ).Method, null );
					il.Emit( OpCodes.Xor );
				}
				else if( field.FieldType == typeof( Player ) )
				{
					il.EmitCall( OpCodes.Call, ( (Func<Player, int>)hash_player ).Method, null );
					il.Emit( OpCodes.Xor );
				}
				else
					throw new NotImplementedException( "SyncAttribute on unhashable field" );
			}

			il.Emit( OpCodes.Ret );
			return (Func<object,int>)d.CreateDelegate( typeof( Func<object,int> ) );
		}

		internal static int hash_int2( int2 i2 )
		{
			return ( ( i2.X * 5 ) ^ ( i2.Y * 3 ) ) / 4;
		}

		internal static int hash_tdict( TypeDictionary d )
		{
			int ret = 0;
			foreach( var o in d )
				ret += CalculateSyncHash( o );
			return ret;
		}

		internal static int hash_actor( Actor a )
		{
			if( a != null )
				return (int)( a.ActorID << 16 );
			return 0;
		}

		internal static int hash_player( Player p )
		{
			if( p != null )
				return p.Index * 0x567;
			return 0;
		}
	}
}
