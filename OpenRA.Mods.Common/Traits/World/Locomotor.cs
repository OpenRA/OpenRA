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
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Support;
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

	[Flags]
	public enum CellFlag : byte
	{
		HasFreeSpace = 0,
		HasActor = 1,
		HasMovingActor = 2,
		HasCrushableActor = 4,
		HasTemporaryBlocker = 8
	}

	public static class LocomoterExts
	{
		public static bool HasCellCondition(this CellConditions c, CellConditions cellCondition)
		{
			// PERF: Enum.HasFlag is slower and requires allocations.
			return (c & cellCondition) == cellCondition;
		}

		public static bool HasCellFlag(this CellFlag c, CellFlag cellFlag)
		{
			// PERF: Enum.HasFlag is slower and requires allocations.
			return (c & cellFlag) == cellFlag;
		}

		public static bool HasMovementType(this MovementType m, MovementType movementType)
		{
			// PERF: Enum.HasFlag is slower and requires allocations.
			return (m & movementType) == movementType;
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
					var cost = (nodesDict.ContainsKey("PathingCost")
						? FieldLoader.GetValue<short>("cost", nodesDict["PathingCost"].Value)
						: 10000 / speed);
					ret.Add(t.Key, new TerrainInfo(speed, (short)cost));
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

			public readonly short Cost;
			public readonly int Speed;

			public TerrainInfo()
			{
				Cost = short.MaxValue;
				Speed = 0;
			}

			public TerrainInfo(int speed, short cost)
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

		public int CalculateTilesetMovementClass(TileSet tileset)
		{
			// collect our ability to cross *all* terraintypes, in a bitvector
			return TilesetTerrainInfo[tileset].Select(ti => ti.Cost < short.MaxValue).ToBits();
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

		public virtual bool DisableDomainPassabilityCheck { get { return false; } }

		public virtual object Create(ActorInitializer init) { return new Locomotor(init.Self, this); }
	}

	public class Locomotor : IWorldLoaded
	{
		struct CellCache
		{
			public readonly LongBitSet<PlayerBitMask> Blocking;
			public readonly LongBitSet<PlayerBitMask> Crushable;
			public readonly CellFlag CellFlag;

			public CellCache(LongBitSet<PlayerBitMask> blocking, CellFlag cellFlag, LongBitSet<PlayerBitMask> crushable = default(LongBitSet<PlayerBitMask>))
			{
				Blocking = blocking;
				Crushable = crushable;
				CellFlag = cellFlag;
			}
		}

		public readonly LocomotorInfo Info;
		CellLayer<short> cellsCost;
		CellLayer<CellCache> blockingCache;

		LocomotorInfo.TerrainInfo[] terrainInfos;
		World world;
		readonly HashSet<CPos> dirtyCells = new HashSet<CPos>();

		IActorMap actorMap;
		bool sharesCell;

		public Locomotor(Actor self, LocomotorInfo info)
		{
			Info = info;
			sharesCell = info.SharesCell;
		}

		public short MovementCostForCell(CPos cell)
		{
			if (!world.Map.Contains(cell))
				return short.MaxValue;

			return cellsCost[cell];
		}

		public short MovementCostToEnterCell(Actor actor, CPos destNode, Actor ignoreActor, CellConditions check)
		{
			if (!world.Map.Contains(destNode))
				return short.MaxValue;

			var cellCost = cellsCost[destNode];

			if (cellCost == short.MaxValue ||
				!CanMoveFreelyInto(actor, destNode, ignoreActor, check))
				return short.MaxValue;

			return cellCost;
		}

		// Determines whether the actor is blocked by other Actors
		public bool CanMoveFreelyInto(Actor actor, CPos cell, Actor ignoreActor, CellConditions check)
		{
			var cellCache = GetCache(cell);
			var cellFlag = cellCache.CellFlag;

			if (!check.HasCellCondition(CellConditions.TransientActors))
				return true;

			// No actor in the cell or free SubCell.
			if (cellFlag == CellFlag.HasFreeSpace)
				return true;

			// If actor is null we're just checking what would happen theoretically.
			// In such a scenario - we'll just assume any other actor in the cell will block us by default.
			// If we have a real actor, we can then perform the extra checks that allow us to avoid being blocked.
			if (actor == null)
				return false;

			// All actors that may be in the cell can be crushed.
			if (cellCache.Crushable.Overlaps(actor.Owner.PlayerMask))
				return true;

			// Cache doesn't account for ignored actors or temporary blockers - these must use the slow path.
			if (ignoreActor == null && !cellFlag.HasCellFlag(CellFlag.HasTemporaryBlocker))
			{
				// We are blocked by another actor in the cell.
				if (cellCache.Blocking.Overlaps(actor.Owner.PlayerMask))
					return false;

				if (check == CellConditions.BlockedByMovers && cellFlag < CellFlag.HasCrushableActor)
					return false;
			}

			foreach (var otherActor in world.ActorMap.GetActorsAt(cell))
				if (IsBlockedBy(actor, otherActor, ignoreActor, check, cellFlag))
					return false;

			return true;
		}

		public SubCell GetAvailableSubCell(Actor self, CPos cell, SubCell preferredSubCell = SubCell.Any, Actor ignoreActor = null, CellConditions check = CellConditions.All)
		{
			var cost = cellsCost[cell];
			if (cost == short.MaxValue)
				return SubCell.Invalid;

			if (check.HasCellCondition(CellConditions.TransientActors))
			{
				Func<Actor, bool> checkTransient = otherActor => IsBlockedBy(self, otherActor, ignoreActor, check, GetCache(cell).CellFlag);

				if (!sharesCell)
					return world.ActorMap.AnyActorsAt(cell, SubCell.FullCell, checkTransient) ? SubCell.Invalid : SubCell.FullCell;

				return world.ActorMap.FreeSubCell(cell, preferredSubCell, checkTransient);
			}

			if (!sharesCell)
				return world.ActorMap.AnyActorsAt(cell, SubCell.FullCell) ? SubCell.Invalid : SubCell.FullCell;

			return world.ActorMap.FreeSubCell(cell, preferredSubCell);
		}

		bool IsBlockedBy(Actor self, Actor otherActor, Actor ignoreActor, CellConditions check, CellFlag cellFlag)
		{
			if (otherActor == ignoreActor)
				return false;

			// If the check allows: we are not blocked by allied units moving in our direction.
			if (!check.HasCellCondition(CellConditions.BlockedByMovers) && cellFlag.HasCellFlag(CellFlag.HasMovingActor) &&
				self.Owner.Stances[otherActor.Owner] == Stance.Ally &&
				IsMovingInMyDirection(self, otherActor))
				return false;

			if (cellFlag.HasCellFlag(CellFlag.HasTemporaryBlocker))
			{
				// If there is a temporary blocker in our path, but we can remove it, we are not blocked.
				var temporaryBlocker = otherActor.TraitOrDefault<ITemporaryBlocker>();
				if (temporaryBlocker != null && temporaryBlocker.CanRemoveBlockage(otherActor, self))
					return false;
			}

			if (!cellFlag.HasCellFlag(CellFlag.HasCrushableActor))
				return true;

			// If we cannot crush the other actor in our way, we are blocked.
			if (Info.Crushes.IsEmpty)
				return true;

			// If the other actor in our way cannot be crushed, we are blocked.
			// PERF: Avoid LINQ.
			var crushables = otherActor.TraitsImplementing<ICrushable>();
			foreach (var crushable in crushables)
				if (crushable.CrushableBy(otherActor, self, Info.Crushes))
					return false;

			return true;
		}

		static bool IsMovingInMyDirection(Actor self, Actor other)
		{
			// PERF: Because we can be sure that OccupiesSpace is Mobile here we can save some performance by avoiding querying for the trait.
			var otherMobile = other.OccupiesSpace as Mobile;
			if (otherMobile == null || !otherMobile.CurrentMovementTypes.HasMovementType(MovementType.Horizontal))
				return false;

			// PERF: Same here.
			var selfMobile = self.OccupiesSpace as Mobile;
			if (selfMobile == null)
				return false;

			// Moving in the same direction if the facing delta is between +/- 90 degrees
			var delta = Util.NormalizeFacing(otherMobile.Facing - selfMobile.Facing);
			return delta < 64 || delta > 192;
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			world = w;
			var map = w.Map;
			actorMap = w.ActorMap;
			actorMap.CellUpdated += CellUpdated;
			blockingCache = new CellLayer<CellCache>(map);
			cellsCost = new CellLayer<short>(map);

			terrainInfos = Info.TilesetTerrainInfo[map.Rules.TileSet];

			foreach (var cell in map.AllCells)
				UpdateCellCost(cell);

			map.CustomTerrain.CellEntryChanged += UpdateCellCost;
			map.Tiles.CellEntryChanged += UpdateCellCost;
		}

		CellCache GetCache(CPos cell)
		{
			if (dirtyCells.Contains(cell))
			{
				UpdateCellBlocking(cell);
				dirtyCells.Remove(cell);
			}

			return blockingCache[cell];
		}

		void CellUpdated(CPos cell)
		{
			dirtyCells.Add(cell);
		}

		void UpdateCellCost(CPos cell)
		{
			var index = cell.Layer == 0
				? world.Map.GetTerrainIndex(cell)
				: world.GetCustomMovementLayers()[cell.Layer].GetTerrainIndex(cell);

			var cost = short.MaxValue;

			if (index != byte.MaxValue)
				cost = terrainInfos[index].Cost;

			cellsCost[cell] = cost;
		}

		void UpdateCellBlocking(CPos cell)
		{
			using (new PerfSample("locomotor_cache"))
			{
				var actors = actorMap.GetActorsAt(cell);

				if (!actors.Any())
				{
					blockingCache[cell] = new CellCache(default(LongBitSet<PlayerBitMask>), CellFlag.HasFreeSpace);
					return;
				}

				if (sharesCell && actorMap.HasFreeSubCell(cell))
				{
					blockingCache[cell] = new CellCache(default(LongBitSet<PlayerBitMask>), CellFlag.HasFreeSpace);
					return;
				}

				var cellFlag = CellFlag.HasActor;
				var cellBlockedPlayers = default(LongBitSet<PlayerBitMask>);
				var cellCrushablePlayers = world.AllPlayersMask;

				foreach (var actor in actors)
				{
					var actorBlocksPlayers = world.AllPlayersMask;
					var crushables = actor.TraitsImplementing<ICrushable>();
					var mobile = actor.OccupiesSpace as Mobile;
					var isMoving = mobile != null && mobile.CurrentMovementTypes.HasMovementType(MovementType.Horizontal);

					if (crushables.Any())
					{
						cellFlag |= CellFlag.HasCrushableActor;
						foreach (var crushable in crushables)
							cellCrushablePlayers = cellCrushablePlayers.Intersect(crushable.CrushableBy(actor, Info.Crushes));
					}
					else
						cellCrushablePlayers = world.NoPlayersMask;

					if (isMoving)
					{
						actorBlocksPlayers = actorBlocksPlayers.Except(actor.Owner.AlliedPlayersMask);
						cellFlag |= CellFlag.HasMovingActor;
					}

					// PERF: Only perform ITemporaryBlocker trait look-up if mod/map rules contain any actors that are temporary blockers
					if (world.RulesContainTemporaryBlocker)
					{
						// If there is a temporary blocker in this cell.
						if (actor.TraitOrDefault<ITemporaryBlocker>() != null)
							cellFlag |= CellFlag.HasTemporaryBlocker;
					}

					cellBlockedPlayers = cellBlockedPlayers.Union(actorBlocksPlayers);
				}

				blockingCache[cell] = new CellCache(cellBlockedPlayers, cellFlag, cellCrushablePlayers);
			}
		}
	}
}
