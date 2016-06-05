using System;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	public class FacingInit : IActorInit<int>
	{
		[FieldFromYamlKey] readonly int value = 128;
		public FacingInit() { }
		public FacingInit(int init) { value = init; }
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

	// Allows maps / transformations to specify the faction variant of an actor.
	public class FactionInit : IActorInit<string>
	{
		[FieldFromYamlKey] public readonly string Faction;

		public FactionInit() { }
		public FactionInit(string faction) { Faction = faction; }
		public string Value(World world) { return Faction; }
	}
}
