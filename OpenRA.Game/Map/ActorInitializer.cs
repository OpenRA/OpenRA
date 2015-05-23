#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA
{
	public class ActorInitializer
	{
		public readonly Actor self;
		public World world { get { return self.World; } }

		internal TypeDictionary Dict;

		public ActorInitializer(Actor actor, TypeDictionary dict)
		{
			self = actor;
			Dict = dict;
		}

		public T Get<T>() where T : IActorInit { return Dict.Get<T>(); }
		public U Get<T, U>() where T : IActorInit<U> { return Dict.Get<T>().Value(world); }
		public bool Contains<T>() where T : IActorInit { return Dict.Contains<T>(); }
	}

	public interface IActorInit { }

	public interface IActorInit<T> : IActorInit
	{
		T Value(World world);
	}

	public class FacingInit : IActorInit<int>
	{
		[FieldFromYamlKey] readonly int value = 128;
		public FacingInit() { }
		public FacingInit(int init) { value = init; }
		public int Value(World world) { return value; }
	}

	public class TurretFacingInit : IActorInit<int>
	{
		[FieldFromYamlKey] readonly int value = 128;
		public TurretFacingInit() { }
		public TurretFacingInit(int init) { value = init; }
		public int Value(World world) { return value; }
	}

	public class LocationInit : IActorInit<CPos>
	{
		[FieldFromYamlKey] readonly CPos value = CPos.Zero;
		public LocationInit() { }
		public LocationInit(CPos init) { value = init; }
		public CPos Value(World world) { return value; }
	}

	public class SubCellInit : IActorInit<SubCell>
	{
		[FieldFromYamlKey] readonly int value = (int)SubCell.FullCell;
		public SubCellInit() { }
		public SubCellInit(int init) { value = init; }
		public SubCellInit(SubCell init) { value = (int)init; }
		public SubCell Value(World world) { return (SubCell)value; }
	}

	public class CenterPositionInit : IActorInit<WPos>
	{
		[FieldFromYamlKey] readonly WPos value = WPos.Zero;
		public CenterPositionInit() { }
		public CenterPositionInit(WPos init) { value = init; }
		public WPos Value(World world) { return value; }
	}

	public class OwnerInit : IActorInit<Player>
	{
		[FieldFromYamlKey] public readonly string PlayerName = "Neutral";
		Player player;

		public OwnerInit() { }
		public OwnerInit(string playerName) { this.PlayerName = playerName; }

		public OwnerInit(Player player)
		{
			this.player = player;
			this.PlayerName = player.InternalName;
		}

		public Player Value(World world)
		{
			if (player != null)
				return player;

			return world.Players.First(x => x.InternalName == PlayerName);
		}
	}
}
