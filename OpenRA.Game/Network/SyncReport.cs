#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using OpenRA.Primitives;

namespace OpenRA.Network
{
	using NamesValuesPair = Pair<string[], object[]>;

	class SyncReport
	{
		const int NumSyncReports = 5;
		static Cache<Type, TypeInfo> typeInfoCache = new Cache<Type, TypeInfo>(t => new TypeInfo(t));

		readonly OrderManager orderManager;

		readonly Report[] syncReports = new Report[NumSyncReports];
		int curIndex = 0;

		static NamesValuesPair DumpSyncTrait(ISync sync)
		{
			var type = sync.GetType();
			TypeInfo typeInfo;
			lock (typeInfoCache)
				typeInfo = typeInfoCache[type];
			var values = new object[typeInfo.Names.Length];
			var index = 0;

			foreach (var func in typeInfo.SerializableCopyOfMemberFunctions)
				values[index++] = func(sync);

			return Pair.New(typeInfo.Names, values);
		}

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

		void GenerateSyncReport(Report report)
		{
			report.Frame = orderManager.NetFrameNumber;
			report.SyncedRandom = orderManager.World.SharedRandom.Last;
			report.TotalCount = orderManager.World.SharedRandom.TotalCount;
			report.Traits.Clear();
			foreach (var a in orderManager.World.ActorsWithTrait<ISync>())
			{
				var sync = Sync.CalculateSyncHash(a.Trait);
				if (sync != 0)
					report.Traits.Add(new TraitReport()
					{
						ActorID = a.Actor.ActorID,
						Type = a.Actor.Info.Name,
						Owner = (a.Actor.Owner == null) ? "null" : a.Actor.Owner.PlayerName,
						Trait = a.Trait.GetType().Name,
						Hash = sync,
						NamesValues = DumpSyncTrait(a.Trait)
					});
			}

			foreach (var e in orderManager.World.Effects)
			{
				var sync = e as ISync;
				if (sync != null)
				{
					var hash = Sync.CalculateSyncHash(sync);
					if (hash != 0)
						report.Effects.Add(new EffectReport()
						{
							Name = sync.ToString().Split('.').Last(),
							Hash = hash,
							NamesValues = DumpSyncTrait(sync)
						});
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

						var nvp = a.NamesValues;
						for (int i = 0; i < nvp.First.Length; i++)
							if (nvp.Second[i] != null)
								Log.Write("sync", "\t\t {0}: {1}".F(nvp.First[i], nvp.Second[i]));
					}

					Log.Write("sync", "Synced Effects:");
					foreach (var e in r.Effects)
					{
						Log.Write("sync", "\t {0} ({1})", e.Name, e.Hash);

						var nvp = e.NamesValues;
						for (int i = 0; i < nvp.First.Length; i++)
							if (nvp.Second[i] != null)
								Log.Write("sync", "\t\t {0}: {1}".F(nvp.First[i], nvp.Second[i]));
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
			public NamesValuesPair NamesValues;
		}

		struct EffectReport
		{
			public string Name;
			public int Hash;
			public NamesValuesPair NamesValues;
		}

		struct TypeInfo
		{
			static ParameterExpression syncParam = Expression.Parameter(typeof(ISync), "sync");
			static ConstantExpression nullString = Expression.Constant(null, typeof(string));

			public readonly Func<ISync, object>[] SerializableCopyOfMemberFunctions;
			public readonly string[] Names;

			public TypeInfo(Type type)
			{
				const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
				var fields = type.GetFields(Flags).Where(fi => !fi.IsLiteral && !fi.IsStatic && fi.HasAttribute<SyncAttribute>());
				var properties = type.GetProperties(Flags).Where(pi => pi.HasAttribute<SyncAttribute>());

				foreach (var prop in properties)
					if (!prop.CanRead || prop.GetIndexParameters().Any())
						throw new InvalidOperationException(
							"Properties using the Sync attribute must be readable and must not use index parameters.\n" +
							"Invalid Property: " + prop.DeclaringType.FullName + "." + prop.Name);

				var sync = Expression.Convert(syncParam, type);
				SerializableCopyOfMemberFunctions = fields
					.Select(fi => SerializableCopyOfMember(Expression.Field(sync, fi), fi.FieldType, fi.Name))
					.Concat(properties.Select(pi => SerializableCopyOfMember(Expression.Property(sync, pi), pi.PropertyType, pi.Name)))
					.ToArray();

				Names = fields.Select(fi => fi.Name).Concat(properties.Select(pi => pi.Name)).ToArray();
			}

			static Func<ISync, object> SerializableCopyOfMember(MemberExpression getMember, Type memberType, string name)
			{
				if (memberType.IsValueType)
				{
					// We can get a copy just by accessing the member since it is a value type.
					var boxedCopy = Expression.Convert(getMember, typeof(object));
					return Expression.Lambda<Func<ISync, object>>(boxedCopy, name, new[] { syncParam }).Compile();
				}

				return MemberToString(getMember, memberType, name);
			}

			static Func<ISync, string> MemberToString(MemberExpression getMember, Type memberType, string name)
			{
				// The lambda generated is shown below.
				// TSync is actual type of the ISync object. Foo is a field or property with the Sync attribute applied.
				var toString = memberType.GetMethod("ToString", Type.EmptyTypes);
				Expression getString;
				if (memberType.IsValueType)
				{
					// (ISync sync) => { return ((TSync)sync).Foo.ToString(); }
					getString = Expression.Call(getMember, toString);
				}
				else
				{
					// (ISync sync) => { var foo = ((TSync)sync).Foo; return foo == null ? null : foo.ToString(); }
					var memberVariable = Expression.Variable(memberType, getMember.Member.Name);
					var assignMemberVariable = Expression.Assign(memberVariable, getMember);
					var member = Expression.Block(new[] { memberVariable }, assignMemberVariable);
					getString = Expression.Call(member, toString);
					var nullMember = Expression.Constant(null, memberType);
					getString = Expression.Condition(Expression.Equal(member, nullMember), nullString, getString);
				}

				return Expression.Lambda<Func<ISync, string>>(getString, name, new[] { syncParam }).Compile();
			}
		}
	}
}
