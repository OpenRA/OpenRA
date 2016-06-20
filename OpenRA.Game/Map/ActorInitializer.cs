#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Primitives;

namespace OpenRA
{
	public interface IActorInitializer
	{
		World World { get; }
		T Get<T>() where T : IActorInit;
		U Get<T, U>() where T : IActorInit<U>;
		bool Contains<T>() where T : IActorInit;
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

		public T Get<T>() where T : IActorInit { return Dict.Get<T>(); }
		public U Get<T, U>() where T : IActorInit<U> { return Dict.Get<T>().Value(World); }
		public bool Contains<T>() where T : IActorInit { return Dict.Contains<T>(); }
	}

	public interface IActorInit { }

	public interface IActorInit<T> : IActorInit
	{
		T Value(World world);
	}

	public class LocationInit : IActorInit<CPos>
	{
		[FieldFromYamlKey] readonly CPos value = CPos.Zero;
		public LocationInit() { }
		public LocationInit(CPos init) { value = init; }
		public CPos Value(World world) { return value; }
	}

	public class OwnerInit : IActorInit<Player>
	{
		[FieldFromYamlKey] public readonly string PlayerName = "Neutral";
		Player player;

		public OwnerInit() { }
		public OwnerInit(string playerName) { PlayerName = playerName; }

		public OwnerInit(Player player)
		{
			this.player = player;
			PlayerName = player.InternalName;
		}

		public Player Value(World world)
		{
			if (player != null)
				return player;

			return world.Players.First(x => x.InternalName == PlayerName);
		}
	}
}
