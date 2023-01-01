#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA
{
	public interface IActorInitializer
	{
		World World { get; }
		T GetOrDefault<T>(TraitInfo info) where T : ActorInit;
		T Get<T>(TraitInfo info) where T : ActorInit;
		U GetValue<T, U>(TraitInfo info) where T : ValueActorInit<U>;
		U GetValue<T, U>(TraitInfo info, U fallback) where T : ValueActorInit<U>;
		bool Contains<T>(TraitInfo info) where T : ActorInit;

		T GetOrDefault<T>() where T : ActorInit, ISingleInstanceInit;
		T Get<T>() where T : ActorInit, ISingleInstanceInit;
		U GetValue<T, U>() where T : ValueActorInit<U>, ISingleInstanceInit;
		U GetValue<T, U>(U fallback) where T : ValueActorInit<U>, ISingleInstanceInit;
		bool Contains<T>() where T : ActorInit, ISingleInstanceInit;
	}

	public class ActorInitializer : IActorInitializer
	{
		public readonly Actor Self;
		public World World => Self.World;

		internal TypeDictionary Dict;

		public ActorInitializer(Actor actor, TypeDictionary dict)
		{
			Self = actor;
			Dict = dict;
		}

		public T GetOrDefault<T>(TraitInfo info) where T : ActorInit
		{
			var inits = Dict.WithInterface<T>();

			// Traits tagged with an instance name prefer inits with the same name.
			// If a more specific init is not available, fall back to an unnamed init.
			// If duplicate inits are defined, take the last to match standard yaml override expectations
			if (info != null && !string.IsNullOrEmpty(info.InstanceName))
				return inits.LastOrDefault(i => i.InstanceName == info.InstanceName) ??
					inits.LastOrDefault(i => string.IsNullOrEmpty(i.InstanceName));

			// Untagged traits will only use untagged inits
			return inits.LastOrDefault(i => string.IsNullOrEmpty(i.InstanceName));
		}

		public T Get<T>(TraitInfo info) where T : ActorInit
		{
			var init = GetOrDefault<T>(info);
			if (init == null)
				throw new InvalidOperationException($"TypeDictionary does not contain instance of type `{typeof(T)}`");

			return init;
		}

		public U GetValue<T, U>(TraitInfo info) where T : ValueActorInit<U>
		{
			return Get<T>(info).Value;
		}

		public U GetValue<T, U>(TraitInfo info, U fallback) where T : ValueActorInit<U>
		{
			var init = GetOrDefault<T>(info);
			return init != null ? init.Value : fallback;
		}

		public bool Contains<T>(TraitInfo info) where T : ActorInit { return GetOrDefault<T>(info) != null; }

		public T GetOrDefault<T>() where T : ActorInit, ISingleInstanceInit
		{
			return Dict.GetOrDefault<T>();
		}

		public T Get<T>() where T : ActorInit, ISingleInstanceInit
		{
			return Dict.Get<T>();
		}

		public U GetValue<T, U>() where T : ValueActorInit<U>, ISingleInstanceInit
		{
			return Get<T>().Value;
		}

		public U GetValue<T, U>(U fallback) where T : ValueActorInit<U>, ISingleInstanceInit
		{
			var init = GetOrDefault<T>();
			return init != null ? init.Value : fallback;
		}

		public bool Contains<T>() where T : ActorInit, ISingleInstanceInit { return GetOrDefault<T>() != null; }
	}

	/*
	 * Things to be aware of when writing ActorInits:
	 *
	 * - ActorReference and ActorGlobal can dynamically create objects without calling a constructor.
	 *   The object will be allocated directly then the best matching Initialize() method will be called to set valid state.
	 * - ActorReference will always attempt to call Initialize(MiniYaml). ActorGlobal will use whichever one it first
	 *   finds with an argument type that matches the given LuaValue.
	 * - Most ActorInits will want to inherit either ValueActorInit<T> or CompositeActorInit which hide the low-level plumbing.
	 * - Inits that reference actors should use ActorInitActorReference which allows actors to be referenced by name in map.yaml
	 * - Inits that should only have a single instance defined on an actor should implement ISingleInstanceInit to allow
	 *   direct queries and runtime enforcement.
	 * - Inits that aren't ISingleInstanceInit should expose a ctor that accepts a TraitInfo to allow per-trait targeting.
	 */
	public abstract class ActorInit
	{
		[FieldLoader.Ignore]
		public readonly string InstanceName;

		protected ActorInit(string instanceName)
		{
			InstanceName = instanceName;
		}

		protected ActorInit() { }

		public abstract MiniYaml Save();
	}

	public interface ISingleInstanceInit { }

	public abstract class ValueActorInit<T> : ActorInit
	{
		readonly T value;

		protected ValueActorInit(TraitInfo info, T value)
			: base(info.InstanceName) { this.value = value; }

		protected ValueActorInit(string instanceName, T value)
			: base(instanceName) { this.value = value; }

		protected ValueActorInit(T value) { this.value = value; }

		public virtual T Value => value;

		public virtual void Initialize(MiniYaml yaml)
		{
			Initialize((T)FieldLoader.GetValue(nameof(value), typeof(T), yaml.Value));
		}

		public virtual void Initialize(T value)
		{
			var field = typeof(ValueActorInit<T>).GetField(nameof(value), BindingFlags.NonPublic | BindingFlags.Instance);
			if (field != null)
				field.SetValue(this, value);
		}

		public override MiniYaml Save()
		{
			return new MiniYaml(FieldSaver.FormatValue(value));
		}
	}

	public abstract class CompositeActorInit : ActorInit
	{
		protected CompositeActorInit(TraitInfo info)
			: base(info.InstanceName) { }

		protected CompositeActorInit()
			: base() { }

		public virtual void Initialize(MiniYaml yaml)
		{
			FieldLoader.Load(this, yaml);
		}

		public virtual void Initialize(Dictionary<string, object> values)
		{
			foreach (var field in GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
			{
				var sa = field.GetCustomAttributes<FieldLoader.SerializeAttribute>(false).DefaultIfEmpty(FieldLoader.SerializeAttribute.Default).First();
				if (!sa.Serialize)
					continue;

				if (values.TryGetValue(field.Name, out var value))
					field.SetValue(this, value);
			}
		}

		public virtual Dictionary<string, Type> InitializeArgs()
		{
			var dict = new Dictionary<string, Type>();
			foreach (var field in GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
			{
				var sa = field.GetCustomAttributes<FieldLoader.SerializeAttribute>(false).DefaultIfEmpty(FieldLoader.SerializeAttribute.Default).First();
				if (!sa.Serialize)
					continue;

				dict[field.Name] = field.FieldType;
			}

			return dict;
		}

		public override MiniYaml Save()
		{
			return FieldSaver.Save(this);
		}
	}

	public class LocationInit : ValueActorInit<CPos>, ISingleInstanceInit
	{
		public LocationInit(CPos value)
			: base(value) { }
	}

	public class OwnerInit : ActorInit, ISingleInstanceInit
	{
		public readonly string InternalName;
		readonly Player value;

		public OwnerInit(Player value)
		{
			this.value = value;
			InternalName = value.InternalName;
		}

		public OwnerInit(string value)
		{
			InternalName = value;
		}

		public Player Value(World world)
		{
			return value ?? world.Players.First(x => x.InternalName == InternalName);
		}

		public void Initialize(MiniYaml yaml)
		{
			var field = typeof(OwnerInit).GetField(nameof(InternalName), BindingFlags.Public | BindingFlags.Instance);
			if (field != null)
				field.SetValue(this, yaml.Value);
		}

		public void Initialize(Player player)
		{
			var field = typeof(OwnerInit).GetField(nameof(value), BindingFlags.NonPublic | BindingFlags.Instance);
			if (field != null)
				field.SetValue(this, player);
		}

		public override MiniYaml Save()
		{
			return new MiniYaml(InternalName);
		}
	}

	public abstract class RuntimeFlagInit : ActorInit, ISuppressInitExport
	{
		public override MiniYaml Save()
		{
			throw new NotImplementedException("RuntimeFlagInit cannot be saved");
		}
	}
}
