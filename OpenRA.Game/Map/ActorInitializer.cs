#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
	}

	public class ActorInitializer : IActorInitializer
	{
		public readonly Actor Self;
		public World World { get { return Self.World; } }

		internal TypeDictionary Dict;

		public ActorInitializer(Actor actor, TypeDictionary dict)
		{
			Self = actor;
			Dict = dict;
		}

		public T GetOrDefault<T>(TraitInfo info) where T : ActorInit
		{
			return Dict.GetOrDefault<T>();
		}

		public T Get<T>(TraitInfo info) where T : ActorInit
		{
			var init = GetOrDefault<T>(info);
			if (init == null)
			    throw new InvalidOperationException("TypeDictionary does not contain instance of type `{0}`".F(typeof(T)));

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
	}

	/*
	 * Things to be aware of when writing ActorInits:
	 *
	 * - ActorReference and ActorGlobal can dynamically create objects without calling a constructor.
	 *   The object will be allocated directly then the best matching Initialize() method will be called to set valid state.
	 * - ActorReference will always attempt to call Initialize(MiniYaml). ActorGlobal will use whichever one it first
	 *   finds with an argument type that matches the given LuaValue.
	 * - Most ActorInits will want to inherit ValueActorInit<T> which hides the low-level plumbing.
	 * - Inits that reference actors should use ActorInitActorReference which allows actors to be referenced by name in map.yaml
	 */
	public abstract class ActorInit
	{
		public abstract MiniYaml Save();
	}

	public abstract class ValueActorInit<T> : ActorInit
	{
		protected readonly T value;

		protected ValueActorInit(TraitInfo info, T value) { this.value = value; }

		protected ValueActorInit(T value) { this.value = value; }

		public virtual T Value { get { return value; } }

		public virtual void Initialize(MiniYaml yaml)
		{
			var valueType = typeof(T).IsEnum ? Enum.GetUnderlyingType(typeof(T)) : typeof(T);
			Initialize((T)FieldLoader.GetValue("value", valueType, yaml.Value));
		}

		public virtual void Initialize(T value)
		{
			var field = GetType().GetField("value", BindingFlags.NonPublic | BindingFlags.Instance);
			if (field != null)
				field.SetValue(this, value);
		}

		public override MiniYaml Save()
		{
			return new MiniYaml(FieldSaver.FormatValue(value));
		}
	}

	public class LocationInit : ValueActorInit<CPos>
	{
		public LocationInit(TraitInfo info, CPos value)
			: base(info, value) { }

		public LocationInit(CPos value)
			: base(value) { }
	}

	public class OwnerInit : ActorInit
	{
		public readonly string InternalName;
		protected readonly Player value;

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
			var field = GetType().GetField("InternalName", BindingFlags.Public | BindingFlags.Instance);
			if (field != null)
				field.SetValue(this, yaml.Value);
		}

		public void Initialize(Player player)
		{
			var field = GetType().GetField("value", BindingFlags.NonPublic | BindingFlags.Instance);
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
