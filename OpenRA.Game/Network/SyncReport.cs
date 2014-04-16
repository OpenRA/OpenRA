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

namespace OpenRA.Network
{
	class SyncReport
	{
		const int NumSyncReports = 5;

		static Cache<Type, Func<object, Dictionary<string, string>>> dumpFuncCache = new Cache<Type, Func<object, Dictionary<string, string>>>(t => GenerateDumpFunc(t));

		readonly OrderManager orderManager;

		Report[] syncReports = new Report[NumSyncReports];
		int curIndex = 0;

		public SyncReport(OrderManager orderManager)
		{
			this.orderManager = orderManager;
			for (var i = 0; i < NumSyncReports; i++)
				syncReports[i] = new Report();
		}

		internal void UpdateSyncReport()
		{
			GenerateSyncReport(syncReports[curIndex]);
			curIndex = ++curIndex % NumSyncReports;
		}

		public static Dictionary<string, string> DumpSyncTrait(object obj)
		{
			return dumpFuncCache[obj.GetType()](obj);
		}

		public static Func<object, Dictionary<string, string>> GenerateDumpFunc(Type t)
		{
			var dictType = typeof(Dictionary<string, string>);

			var d = new DynamicMethod("dump_{0}".F(t.Name), dictType, new Type[] { typeof(object) }, t);
			var il = d.GetILGenerator();

			var this_ = il.DeclareLocal(t).LocalIndex;
			var dict_ = il.DeclareLocal(dictType).LocalIndex;
			var obj_ = il.DeclareLocal(typeof(object)).LocalIndex;

			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Castclass, t);
			il.Emit(OpCodes.Stloc, this_);

			var dictAdd_ = dictType.GetMethod("Add", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(string), typeof(string) }, null);
			var dictCtor_ = dictType.GetConstructor(Type.EmptyTypes);
			var objToString_ = typeof(object).GetMethod("ToString", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);

			il.Emit(OpCodes.Newobj, dictCtor_);
			il.Emit(OpCodes.Stloc, dict_);

			const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
			foreach (var field in t.GetFields(Flags).Where(x => x.HasAttribute<SyncAttribute>()))
			{
				if (field.IsLiteral || field.IsStatic) continue;

				var lblNull = il.DefineLabel();

				il.Emit(OpCodes.Ldloc, this_);
				il.Emit(OpCodes.Ldfld, field);

				if (field.FieldType.IsValueType)
					il.Emit(OpCodes.Box, field.FieldType);

				il.Emit(OpCodes.Dup);
				il.Emit(OpCodes.Stloc, obj_);

				il.Emit(OpCodes.Brfalse, lblNull);

				il.Emit(OpCodes.Ldloc, dict_);
				il.Emit(OpCodes.Ldstr, field.Name);

				il.Emit(OpCodes.Ldloc, obj_);
				il.Emit(OpCodes.Callvirt, objToString_);
				il.Emit(OpCodes.Callvirt, dictAdd_);

				il.MarkLabel(lblNull);
			}

			foreach (var prop in t.GetProperties(Flags).Where(x => x.HasAttribute<SyncAttribute>()))
			{
				var lblNull = il.DefineLabel();

				il.Emit(OpCodes.Ldloc, this_);
				il.EmitCall(OpCodes.Call, prop.GetGetMethod(), null);

				if (prop.PropertyType.IsValueType)
					il.Emit(OpCodes.Box, prop.PropertyType);

				il.Emit(OpCodes.Dup);
				il.Emit(OpCodes.Stloc, obj_);

				il.Emit(OpCodes.Brfalse, lblNull);

				il.Emit(OpCodes.Ldloc, dict_);
				il.Emit(OpCodes.Ldstr, prop.Name);

				il.Emit(OpCodes.Ldloc, obj_);
				il.Emit(OpCodes.Callvirt, objToString_);
				il.Emit(OpCodes.Callvirt, dictAdd_);

				il.MarkLabel(lblNull);
			}

			il.Emit(OpCodes.Ldloc, dict_);
			il.Emit(OpCodes.Ret);
			return (Func<object, Dictionary<string, string>>)d.CreateDelegate(typeof(Func<object, Dictionary<string, string>>));
		}

		void GenerateSyncReport(Report report)
		{
			report.Frame = orderManager.NetFrameNumber;
			report.SyncedRandom = orderManager.world.SharedRandom.Last;
			report.TotalCount = orderManager.world.SharedRandom.TotalCount;
			report.Traits.Clear();
			foreach (var a in orderManager.world.ActorsWithTrait<ISync>())
			{
				var sync = Sync.CalculateSyncHash(a.Trait);
				if (sync != 0)
				{
					var tr = new TraitReport()
					{
						ActorID = a.Actor.ActorID,
						Type = a.Actor.Info.Name,
						Owner = (a.Actor.Owner == null) ? "null" : a.Actor.Owner.PlayerName,
						Trait = a.Trait.GetType().Name,
						Hash = sync
					};

					tr.Fields = DumpSyncTrait(a.Trait);
					report.Traits.Add(tr);
				}
			}

			foreach (var e in orderManager.world.Effects)
			{
				var sync = e as ISync;
				if (sync != null)
				{
					var hash = Sync.CalculateSyncHash(sync);
					if (hash != 0)
					{
						var er = new EffectReport()
						{
							Name = sync.ToString().Split('.').Last(),
							Hash = hash
						};

						er.Fields = DumpSyncTrait(sync);
						report.Effects.Add(er);
					}
				}
			}
		}

		internal void DumpSyncReport(int frame)
		{
			foreach (var r in syncReports)
			{
				if (r.Frame == frame)
				{
					var mod = Game.modData.Manifest.Mod;
					Log.Write("sync", "Player: {0} ({1} {2} {3})", Game.Settings.Player.Name, Platform.CurrentPlatform, Environment.OSVersion, Platform.RuntimeVersion);
					Log.Write("sync", "Game ID: {0} (Mod: {1} at Version {2})", orderManager.LobbyInfo.GlobalSettings.GameUid, mod.Title, mod.Version);
					Log.Write("sync", "Sync for net frame {0} -------------", r.Frame);
					Log.Write("sync", "SharedRandom: {0} (#{1})", r.SyncedRandom, r.TotalCount);
					Log.Write("sync", "Synced Traits:");
					foreach (var a in r.Traits)
					{
						Log.Write("sync", "\t {0} {1} {2} {3} ({4})".F(a.ActorID, a.Type, a.Owner, a.Trait, a.Hash));

						foreach (var f in a.Fields)
							Log.Write("sync", "\t\t {0}: {1}".F(f.Key, f.Value));
					}

					Log.Write("sync", "Synced Effects:");
					foreach (var e in r.Effects)
					{
						Log.Write("sync", "\t {0} ({1})", e.Name, e.Hash);
						foreach (var f in e.Fields)
							Log.Write("sync", "\t\t {0}: {1}".F(f.Key, f.Value));
					}

					return;
				}
			}

			Log.Write("sync", "No sync report available!");
		}

		class Report
		{
			public int Frame;
			public int SyncedRandom;
			public int TotalCount;
			public List<TraitReport> Traits = new List<TraitReport>();
			public List<EffectReport> Effects = new List<EffectReport>();
		}

		struct TraitReport
		{
			public uint ActorID;
			public string Type;
			public string Owner;
			public string Trait;
			public int Hash;
			public Dictionary<string, string> Fields;
		}

		struct EffectReport
		{
			public string Name;
			public int Hash;
			public Dictionary<string, string> Fields;
		}
	}
}
