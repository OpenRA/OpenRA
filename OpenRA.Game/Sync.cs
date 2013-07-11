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
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using OpenRA.FileFormats;

namespace OpenRA
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class SyncAttribute : Attribute { }
	public interface ISync { }	/* marker interface */

	public static class Sync
	{
		static Cache<Type, Func<object, int>> hashFuncCache = new Cache<Type, Func<object, int>>( t => GenerateHashFunc( t ) );

		public static int CalculateSyncHash( object obj )
		{
			return hashFuncCache[ obj.GetType() ]( obj );
		}

		static void EmitSyncOpcodes(Type type, ILGenerator il)
		{
			if (type == typeof(int))
			{
				il.Emit(OpCodes.Xor);
			}
			else if (type == typeof(bool))
			{
				var l = il.DefineLabel();
				il.Emit(OpCodes.Ldc_I4, 0xaaa);
				il.Emit(OpCodes.Brtrue, l);
				il.Emit(OpCodes.Pop);
				il.Emit(OpCodes.Ldc_I4, 0x555);
				il.MarkLabel(l);
				il.Emit(OpCodes.Xor);
			}
			else if (type == typeof(int2))
			{
				il.EmitCall(OpCodes.Call, ((Func<int2, int>)hash_int2).Method, null);
				il.Emit(OpCodes.Xor);
			}
			else if (type == typeof(CPos))
			{
				il.EmitCall(OpCodes.Call, ((Func<CPos, int>)hash_CPos).Method, null);
				il.Emit(OpCodes.Xor);
			}
			else if (type == typeof(CVec))
			{
				il.EmitCall(OpCodes.Call, ((Func<CVec, int>)hash_CVec).Method, null);
				il.Emit(OpCodes.Xor);
			}
			else if (type == typeof(PPos))
			{
				il.EmitCall(OpCodes.Call, ((Func<PPos, int>)hash_PPos).Method, null);
				il.Emit(OpCodes.Xor);
			}
			else if (type == typeof(PVecInt))
			{
				il.EmitCall(OpCodes.Call, ((Func<PVecInt, int>)hash_PVecInt).Method, null);
				il.Emit(OpCodes.Xor);
			}
			else if (type == typeof(PSubPos))
			{
				il.EmitCall(OpCodes.Call, ((Func<PSubPos, int>)hash_PSubPos).Method, null);
				il.Emit(OpCodes.Xor);
			}
			else if (type == typeof(PSubVec))
			{
				il.EmitCall(OpCodes.Call, ((Func<PSubVec, int>)hash_PSubVec).Method, null);
				il.Emit(OpCodes.Xor);
			}
			else if (type == typeof(WRange))
			{
				il.EmitCall(OpCodes.Call, ((Func<WRange, int>)hash<WRange>).Method, null);
				il.Emit(OpCodes.Xor);
			}
			else if (type == typeof(WPos))
			{
				il.EmitCall(OpCodes.Call, ((Func<WPos, int>)hash<WPos>).Method, null);
				il.Emit(OpCodes.Xor);
			}
			else if (type == typeof(WVec))
			{
				il.EmitCall(OpCodes.Call, ((Func<WVec, int>)hash<WVec>).Method, null);
				il.Emit(OpCodes.Xor);
			}
			else if (type == typeof(WAngle))
			{
				il.EmitCall(OpCodes.Call, ((Func<WAngle, int>)hash<WAngle>).Method, null);
				il.Emit(OpCodes.Xor);
			}
			else if (type == typeof(WRot))
			{
				il.EmitCall(OpCodes.Call, ((Func<WRot, int>)hash<WRot>).Method, null);
				il.Emit(OpCodes.Xor);
			}
			else if (type == typeof(TypeDictionary))
			{
				il.EmitCall(OpCodes.Call, ((Func<TypeDictionary, int>)hash_tdict).Method, null);
				il.Emit(OpCodes.Xor);
			}
			else if (type == typeof(Actor))
			{
				il.EmitCall(OpCodes.Call, ((Func<Actor, int>)hash_actor).Method, null);
				il.Emit(OpCodes.Xor);
			}
			else if (type == typeof(Player))
			{
				il.EmitCall(OpCodes.Call, ((Func<Player, int>)hash_player).Method, null);
				il.Emit(OpCodes.Xor);
			}
			else if (type.HasAttribute<SyncAttribute>())
			{
				il.EmitCall(OpCodes.Call, ((Func<object, int>)CalculateSyncHash).Method, null);
				il.Emit(OpCodes.Xor);
			}
			else
				throw new NotImplementedException("SyncAttribute on member of unhashable type: {0}".F(type.FullName));
		}

		public static Func<object, int> GenerateHashFunc(Type t)
		{
			var d = new DynamicMethod("hash_{0}".F(t.Name), typeof(int), new Type[] { typeof(object) }, t);
			var il = d.GetILGenerator();
			var this_ = il.DeclareLocal(t).LocalIndex;
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Castclass, t);
			il.Emit(OpCodes.Stloc, this_);
			il.Emit(OpCodes.Ldc_I4_0);

			const BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
			foreach (var field in t.GetFields(bf).Where(x => x.HasAttribute<SyncAttribute>()))
			{
				il.Emit(OpCodes.Ldloc, this_);
				il.Emit(OpCodes.Ldfld, field);

				EmitSyncOpcodes(field.FieldType, il);
			}

			foreach (var prop in t.GetProperties(bf).Where(x => x.HasAttribute<SyncAttribute>()))
			{
				il.Emit(OpCodes.Ldloc, this_);
				il.EmitCall(OpCodes.Call, prop.GetGetMethod(), null);

				EmitSyncOpcodes(prop.PropertyType, il);
			}

			il.Emit(OpCodes.Ret);
			return (Func<object, int>)d.CreateDelegate(typeof(Func<object, int>));
		}

		public static int hash_int2( int2 i2 )
		{
			return ( ( i2.X * 5 ) ^ ( i2.Y * 3 ) ) / 4;
		}

		public static int hash_CPos( CPos i2 )
		{
			return ( ( i2.X * 5) ^ ( i2.Y * 3 ) ) / 4;
		}
		
		public static int hash_CVec( CVec i2 )
		{
			return ( ( i2.X * 5) ^ ( i2.Y * 3 ) ) / 4;
		}
		
		public static int hash_PPos( PPos i2 )
		{
			return ( ( i2.X * 5) ^ ( i2.Y * 3 ) ) / 4;
		}
		
		public static int hash_PVecInt( PVecInt i2 )
		{
			return ( ( i2.X * 5) ^ ( i2.Y * 3 ) ) / 4;
		}

		public static int hash_PSubPos(PSubPos i2)
		{
			return ((i2.X * 5) ^ (i2.Y * 3)) / 4;
		}

		public static int hash_PSubVec(PSubVec i2)
		{
			return ((i2.X * 5) ^ (i2.Y * 3)) / 4;
		}

		public static int hash_tdict( TypeDictionary d )
		{
			int ret = 0;
			foreach( var o in d )
				ret += CalculateSyncHash( o );
			return ret;
		}

		public static int hash_actor( Actor a )
		{
			if( a != null )
				return (int)( a.ActorID << 16 );
			return 0;
		}

		public static int hash_player( Player p )
		{
			if( p != null )
				return (int)( p.PlayerActor.ActorID << 16 ) * 0x567;
			return 0;
		}

		public static int hash<T>(T t)
		{
			return t.GetHashCode();
		}

		public static void CheckSyncUnchanged( World world, Action fn )
		{
			CheckSyncUnchanged( world, () => { fn(); return true; } );
		}

		static bool inUnsyncedCode = false;

		public static T CheckSyncUnchanged<T>( World world, Func<T> fn )
		{
			if( world == null ) return fn();
			var shouldCheckSync = Game.Settings.Debug.SanityCheckUnsyncedCode;
			int sync = shouldCheckSync ? world.SyncHash() : 0;
			bool prevInUnsyncedCode = inUnsyncedCode;
			inUnsyncedCode = true;

			try
			{
				return fn();
			}
			finally
			{
				inUnsyncedCode = prevInUnsyncedCode;
				if( shouldCheckSync && sync != world.SyncHash() )
					throw new InvalidOperationException( "CheckSyncUnchanged: sync-changing code may not run here" );
			}
		}

		public static void AssertUnsynced( string message )
		{
			if( !inUnsyncedCode )
				throw new InvalidOperationException( message );
		}
	}
}
