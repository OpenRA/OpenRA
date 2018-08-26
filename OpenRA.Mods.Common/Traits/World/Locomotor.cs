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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Flags]
	public enum CellConditions
	{
		None = 0,
		TransientActors,
		BlockedByMovers,
		All = TransientActors | BlockedByMovers
	}

	public static class CellConditionsExts
	{
		public static bool HasCellCondition(this CellConditions c, CellConditions cellCondition)
		{
			// PERF: Enum.HasFlag is slower and requires allocations.
			return (c & cellCondition) == cellCondition;
		}
	}

	public static class CustomMovementLayerType
	{
		public const byte Tunnel = 1;
		public const byte Subterranean = 2;
		public const byte Jumpjet = 3;
		public const byte ElevatedBridge = 4;
	}

	[Desc("Used by Mobile. Attach these to the world actor. You can have multiple variants by adding @suffixes.")]
	public class LocomotorInfo : ITraitInfo
	{
		[Desc("Locomotor ID.")]
		public readonly string Name = "default";

		public readonly int WaitAverage = 40;

		public readonly int WaitSpread = 10;

		[Desc("Allow multiple (infantry) units in one cell.")]
		public readonly bool SharesCell = false;

		[Desc("Can the actor be ordered to move in to shroud?")]
		public readonly bool MoveIntoShroud = true;

		[Desc("e.g. crate, wall, infantry")]
		public readonly BitSet<CrushClass> Crushes = default(BitSet<CrushClass>);

		[Desc("Types of damage that are caused while crushing. Leave empty for no damage types.")]
		public readonly BitSet<DamageType> CrushDamageTypes = default(BitSet<DamageType>);

		[FieldLoader.LoadUsing("LoadSpeeds", true)]
		[Desc("Lower the value on rough terrain. Leave out entries for impassable terrain.")]
		public readonly Dictionary<string, TerrainInfo> TerrainSpeeds;

		protected static object LoadSpeeds(MiniYaml y)
		{
			var ret = new Dictionary<string, TerrainInfo>();
			foreach (var t in y.ToDictionary()["TerrainSpeeds"].Nodes)
			{
				var speed = FieldLoader.GetValue<int>("speed", t.Value.Value);
				if (speed > 0)
				{
					var nodesDict = t.Value.ToDictionary();
					var cost = nodesDict.ContainsKey("PathingCost")
						? FieldLoader.GetValue<int>("cost", nodesDict["PathingCost"].Value)
						: 10000 / speed;
					ret.Add(t.Key, new TerrainInfo(speed, cost));
				}
			}

			return ret;
		}

		TerrainInfo[] LoadTilesetSpeeds(TileSet tileSet)
		{
			var info = new TerrainInfo[tileSet.TerrainInfo.Length];
			for (var i = 0; i < info.Length; i++)
				info[i] = TerrainInfo.Impassable;

			foreach (var kvp in TerrainSpeeds)
			{
				byte index;
				if (tileSet.TryGetTerrainIndex(kvp.Key, out index))
					info[index] = kvp.Value;
			}

			return info;
		}

		public class TerrainInfo
		{
			public static readonly TerrainInfo Impassable = new TerrainInfo();

			public readonly int Cost;
			public readonly int Speed;

			public TerrainInfo()
			{
				Cost = int.MaxValue;
				Speed = 0;
			}

			public TerrainInfo(int speed, int cost)
			{
				Speed = speed;
				Cost = cost;
			}
		}

		public struct WorldMovementInfo
		{
			internal readonly World World;
			internal readonly TerrainInfo[] TerrainInfos;
			internal WorldMovementInfo(World world, LocomotorInfo info)
			{
				// PERF: This struct allows us to cache the terrain info for the tileset used by the world.
				// This allows us to speed up some performance-sensitive pathfinding calculations.
				World = world;
				TerrainInfos = info.TilesetTerrainInfo[world.Map.Rules.TileSet];
			}
		}

		public readonly Cache<TileSet, TerrainInfo[]> TilesetTerrainInfo;
		public readonly Cache<TileSet, int> TilesetMovementClass;

		public LocomotorInfo()
		{
			TilesetTerrainInfo = new Cache<TileSet, TerrainInfo[]>(LoadTilesetSpeeds);
			TilesetMovementClass = new Cache<TileSet, int>(CalculateTilesetMovementClass);
		}

		public int MovementCostForCell(World world, CPos cell)
		{
			return MovementCostForCell(world, TilesetTerrainInfo[world.Map.Rules.TileSet], cell);
		}

		int MovementCostForCell(World world, TerrainInfo[] terrainInfos, CPos cell)
		{
			if (!world.Map.Contains(cell))
				return int.MaxValue;

			var index = cell.Layer == 0 ? world.Map.GetTerrainIndex(cell) :
				world.GetCustomMovementLayers()[cell.Layer].GetTerrainIndex(cell);

			if (index == byte.MaxValue)
				return int.MaxValue;

			return terrainInfos[index].Cost;
		}

		public int CalculateTilesetMovementClass(TileSet tileset)
		{
			// collect our ability to cross *all* terraintypes, in a bitvector
			return TilesetTerrainInfo[tileset].Select(ti => ti.Cost < int.MaxValue).ToBits();
		}

		public uint GetMovementClass(TileSet tileset)
		{
			return (uint)TilesetMovementClass[tileset];
		}

		public int TileSetMovementHash(TileSet tileSet)
		{
			var terrainInfos = TilesetTerrainInfo[tileSet];

			// Compute and return the hash using aggregate
			return terrainInfos.Aggregate(terrainInfos.Length,
				(current, terrainInfo) => unchecked(current * 31 + terrainInfo.Cost));
		}

		public WorldMovementInfo GetWorldMovementInfo(World world)
		{
			return new WorldMovementInfo(world, this);
		}

		public int MovementCostToEnterCell(WorldMovementInfo worldMovementInfo, Actor self, CPos cell, Actor ignoreActor = null, CellConditions check = CellConditions.All)
		{
			var cost = MovementCostForCell(worldMovementInfo.World, worldMovementInfo.TerrainInfos, cell);
			if (cost == int.MaxValue || !CanMoveFreelyInto(worldMovementInfo.World, self, cell, ignoreActor, check))
				return int.MaxValue;
			return cost;
		}

		public SubCell GetAvailableSubCell(
			World world, Actor self, CPos cell, SubCell preferredSubCell = SubCell.Any, Actor ignoreActor = null, CellConditions check = CellConditions.All)
		{
			if (MovementCostForCell(world, cell) == int.MaxValue)
				return SubCell.Invalid;

			if (check.HasCellCondition(CellConditions.TransientActors))
			{
				Func<Actor, bool> checkTransient = otherActor => IsBlockedBy(self, otherActor, ignoreActor, check);

				if (!SharesCell)
					return world.ActorMap.AnyActorsAt(cell, SubCell.FullCell, checkTransient) ? SubCell.Invalid : SubCell.FullCell;

				return world.ActorMap.FreeSubCell(cell, preferredSubCell, checkTransient);
			}

			if (!SharesCell)
				return world.ActorMap.AnyActorsAt(cell, SubCell.FullCell) ? SubCell.Invalid : SubCell.FullCell;

			return world.ActorMap.FreeSubCell(cell, preferredSubCell);
		}

		static bool IsMovingInMyDirection(Actor self, Actor other)
		{
			var otherMobile = other.TraitOrDefault<Mobile>();
			if (otherMobile == null || !otherMobile.CurrentMovementTypes.HasFlag(MovementType.Horizontal))
				return false;

			var selfMobile = self.TraitOrDefault<Mobile>();
			if (selfMobile == null)
				return false;

			// Moving in the same direction if the facing delta is between +/- 90 degrees
			var delta = Util.NormalizeFacing(otherMobile.Facing - selfMobile.Facing);
			return delta < 64 || delta > 192;
		}

		// Determines whether the actor is blocked by other Actors
		public bool CanMoveFreelyInto(World world, Actor self, CPos cell, Actor ignoreActor, CellConditions check)
		{
			if (!check.HasCellCondition(CellConditions.TransientActors))
				return true;

			if (SharesCell && world.ActorMap.HasFreeSubCell(cell))
				return true;

			// PERF: Avoid LINQ.
			foreach (var otherActor in world.ActorMap.GetActorsAt(cell))
				if (IsBlockedBy(self, otherActor, ignoreActor, check))
					return false;

			return true;
		}

		bool IsBlockedBy(Actor self, Actor otherActor, Actor ignoreActor, CellConditions check)
		{
			// We are not blocked by the actor we are ignoring.
			if (otherActor == ignoreActor)
				return false;

			// If self is null, we don't have a real actor - we're just checking what would happen theoretically.
			// In such a scenario - we'll just assume any other actor in the cell will block us by default.
			// If we have a real actor, we can then perform the extra checks that allow us to avoid being blocked.
			if (self == null)
				return true;

			// If the check allows: we are not blocked by allied units moving in our direction.
			if (!check.HasCellCondition(CellConditions.BlockedByMovers) &&
				self.Owner.Stances[otherActor.Owner] == Stance.Ally &&
				IsMovingInMyDirection(self, otherActor))
				return false;

			// PERF: Only perform ITemporaryBlocker trait look-up if mod/map rules contain any actors that are temporary blockers
			if (self.World.RulesContainTemporaryBlocker)
			{
				// If there is a temporary blocker in our path, but we can remove it, we are not blocked.
				var temporaryBlocker = otherActor.TraitOrDefault<ITemporaryBlocker>();
				if (temporaryBlocker != null && temporaryBlocker.CanRemoveBlockage(otherActor, self))
					return false;
			}

			// If we cannot crush the other actor in our way, we are blocked.
			if (Crushes.IsEmpty)
				return true;

			// If the other actor in our way cannot be crushed, we are blocked.
			// PERF: Avoid LINQ.
			var crushables = otherActor.TraitsImplementing<ICrushable>();
			foreach (var crushable in crushables)
				if (crushable.CrushableBy(otherActor, self, Crushes))
					return false;

			return true;
		}

		public virtual bool DisableDomainPassabilityCheck { get { return false; } }

		public virtual object Create(ActorInitializer init) { return new Locomotor(init.Self, this); }
	}

	public class Locomotor
	{
		public readonly LocomotorInfo Info;

		public Locomotor(Actor self, LocomotorInfo info)
		{
			Info = info;
		}
	}
}
