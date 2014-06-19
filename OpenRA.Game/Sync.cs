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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public sealed class SyncAttribute : Attribute { }
	public interface ISync { }	/* marker interface */

	public static class Sync
	{
		static Cache<Type, Func<object, int>> hashFuncCache = new Cache<Type, Func<object, int>>(t => GenerateHashFunc(t));

		public static int CalculateSyncHash(object obj)
		{
			return hashFuncCache[obj.GetType()](obj);
		}

		static Dictionary<Type, MethodInfo> hashFunctions = new Dictionary<Type, MethodInfo>()
		{
			{typeof(int2), ((Func<int2, int>)hash_int2).Method},
			{typeof(CPos), ((Func<CPos, int>)hash_CPos).Method},
			{typeof(CVec), ((Func<CVec, int>)hash_CVec).Method},
			{typeof(WRange), ((Func<WRange, int>)hash<WRange>).Method},
			{typeof(WPos), ((Func<WPos, int>)hash<WPos>).Method},
			{typeof(WVec), ((Func<WVec, int>)hash<WVec>).Method},
			{typeof(WAngle), ((Func<WAngle, int>)hash<WAngle>).Method},
			{typeof(WRot), ((Func<WRot, int>)hash<WRot>).Method},
			{typeof(TypeDictionary), ((Func<TypeDictionary, int>)hash_tdict).Method},
			{typeof(Actor), ((Func<Actor, int>)hash_actor).Method},
			{typeof(Player), ((Func<Player, int>)hash_player).Method},
			{typeof(Target), ((Func<Target, int>)hash_target).Method},
		};

		static void EmitSyncOpcodes(Type type, ILGenerator il)
		{
			if (hashFunctions.ContainsKey(type))
				il.EmitCall(OpCodes.Call, hashFunctions[type], null);
			else if (type == typeof(bool))
			{
				var l = il.DefineLabel();
				il.Emit(OpCodes.Ldc_I4, 0xaaa);
				il.Emit(OpCodes.Brtrue, l);
				il.Emit(OpCodes.Pop);
				il.Emit(OpCodes.Ldc_I4, 0x555);
				il.MarkLabel(l);
			}
			else if (type.HasAttribute<SyncAttribute>())
				il.EmitCall(OpCodes.Call, ((Func<object, int>)CalculateSyncHash).Method, null);
			else if (type != typeof(int))
				throw new NotImplementedException("SyncAttribute on member of unhashable type: {0}".F(type.FullName));

			il.Emit(OpCodes.Xor);
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

		public static int hash_int2(int2 i2)
		{
			return ((i2.X * 5) ^ (i2.Y * 3)) / 4;
		}

		public static int hash_CPos(CPos i2)
		{
			return ((i2.X * 5) ^ (i2.Y * 3)) / 4;
		}
		
		public static int hash_CVec(CVec i2)
		{
			return ((i2.X * 5) ^ (i2.Y * 3)) / 4;
		}

		public static int hash_tdict(TypeDictionary d)
		{
			var ret = 0;
			foreach (var o in d)
				ret += CalculateSyncHash(o);
			return ret;
		}

		public static int hash_actor(Actor a)
		{
			if (a != null)
				return (int)(a.ActorID << 16);
			return 0;
		}

		public static int hash_player(Player p)
		{
			if (p != null)
				return (int)(p.PlayerActor.ActorID << 16) * 0x567;
			return 0;
		}

		public static int hash_target(Target t)
		{
			switch (t.Type)
			{
				case TargetType.Actor:
					return (int)(t.Actor.ActorID << 16) * 0x567;

				case TargetType.FrozenActor:
					return (int)(t.FrozenActor.Actor.ActorID << 16) * 0x567;

				case TargetType.Terrain:
					return hash<WPos>(t.CenterPosition);

				default:
				case TargetType.Invalid:
					return 0;
			}
		}

		public static int hash<T>(T t)
		{
			return t.GetHashCode();
		}

		public static void CheckSyncUnchanged(World world, Action fn)
		{
			CheckSyncUnchanged(world, () => { fn(); return true; });
		}

		static bool inUnsyncedCode = false;

		public static T CheckSyncUnchanged<T>(World world, Func<T> fn)
		{
			if (world == null)
				return fn();

			var shouldCheckSync = Game.Settings.Debug.SanityCheckUnsyncedCode;
			var sync = shouldCheckSync ? world.SyncHash() : 0;
			var prevInUnsyncedCode = inUnsyncedCode;
			inUnsyncedCode = true;

			try
			{
				return fn();
			}
			finally
			{
				inUnsyncedCode = prevInUnsyncedCode;
				if (shouldCheckSync && sync != world.SyncHash())
					throw new InvalidOperationException("CheckSyncUnchanged: sync-changing code may not run here");
			}
		}

		public static void AssertUnsynced(string message)
		{
			if (!inUnsyncedCode)
				throw new InvalidOperationException(message);
		}
	}
}
