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
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA
{
	public interface IActorInitializer
	{
		World World { get; }
		T GetOrDefault<T>(TraitInfo info) where T : IActorInit;
		T Get<T>(TraitInfo info) where T : IActorInit;
		U GetValue<T, U>(TraitInfo info) where T : IActorInit<U>;
		U GetValue<T, U>(TraitInfo info, U fallback) where T : IActorInit<U>;
		bool Contains<T>(TraitInfo info) where T : IActorInit;
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

		public T GetOrDefault<T>(TraitInfo info) where T : IActorInit
		{
			return Dict.GetOrDefault<T>();
		}

		public T Get<T>(TraitInfo info) where T : IActorInit
		{
			var init = GetOrDefault<T>(info);
			if (init == null)
			    throw new InvalidOperationException("TypeDictionary does not contain instance of type `{0}`".F(typeof(T)));

			return init;
		}

		public U GetValue<T, U>(TraitInfo info) where T : IActorInit<U>
		{
			return Get<T>(info).Value;
		}

		public U GetValue<T, U>(TraitInfo info, U fallback) where T : IActorInit<U>
		{
			var init = GetOrDefault<T>(info);
			return init != null ? init.Value : fallback;
		}

		public bool Contains<T>(TraitInfo info) where T : IActorInit { return GetOrDefault<T>(info) != null; }
	}

	public interface IActorInit { }

	public interface IActorInit<T> : IActorInit
	{
		T Value { get; }
	}

	public class LocationInit : IActorInit<CPos>
	{
		[FieldFromYamlKey]
		readonly CPos value = CPos.Zero;

		public LocationInit() { }
		public LocationInit(CPos init) { value = init; }
		public CPos Value { get { return value; } }
	}

	public class OwnerInit : IActorInit
	{
		[FieldFromYamlKey]
		public readonly string InternalName = "Neutral";

		Player player;

		public OwnerInit() { }
		public OwnerInit(string playerName) { InternalName = playerName; }

		public OwnerInit(Player player)
		{
			this.player = player;
			InternalName = player.InternalName;
		}

		public Player Value(World world)
		{
			if (player != null)
				return player;

			return world.Players.First(x => x.InternalName == InternalName);
		}
	}
}
