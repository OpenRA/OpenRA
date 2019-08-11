#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	public class FacingInit : IActorInit<int>
	{
		[FieldFromYamlKey]
		readonly int value = 128;

		public FacingInit() { }
		public FacingInit(int init) { value = init; }
		public int Value(World world) { return value; }
	}

	public class MoveIntoWorldDelayInit : IActorInit<int>
	{
		[FieldFromYamlKey]
		readonly int value = 0;

		public MoveIntoWorldDelayInit() { }
		public MoveIntoWorldDelayInit(int init) { value = init; }
		public int Value(World world) { return value; }
	}

	public class DynamicFacingInit : IActorInit<Func<int>>
	{
		readonly Func<int> func;

		public DynamicFacingInit(Func<int> func) { this.func = func; }
		public Func<int> Value(World world) { return func; }
	}

	public class SubCellInit : IActorInit<SubCell>
	{
		[FieldFromYamlKey]
		readonly byte value = (byte)SubCell.FullCell;

		public SubCellInit() { }
		public SubCellInit(byte init) { value = init; }
		public SubCellInit(SubCell init) { value = (byte)init; }
		public SubCell Value(World world) { return (SubCell)value; }
	}

	public class CenterPositionInit : IActorInit<WPos>
	{
		[FieldFromYamlKey]
		readonly WPos value = WPos.Zero;

		public CenterPositionInit() { }
		public CenterPositionInit(WPos init) { value = init; }
		public WPos Value(World world) { return value; }
	}

	// Allows maps / transformations to specify the faction variant of an actor.
	public class FactionInit : IActorInit<string>
	{
		[FieldFromYamlKey]
		public readonly string Faction;

		public FactionInit() { }
		public FactionInit(string faction) { Faction = faction; }
		public string Value(World world) { return Faction; }
	}

	public class EffectiveOwnerInit : IActorInit<Player>
	{
		[FieldFromYamlKey]
		readonly Player value = null;

		public EffectiveOwnerInit() { }
		public EffectiveOwnerInit(Player owner) { value = owner; }
		Player IActorInit<Player>.Value(World world) { return value; }
	}
}
