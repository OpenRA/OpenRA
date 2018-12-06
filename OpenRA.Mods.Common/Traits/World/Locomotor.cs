#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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
		[Desc("Locomotor ID.")] public readonly string Name = "default";

		public readonly int WaitAverage = 40;

		public readonly int WaitSpread = 10;

		[Desc("Allow multiple (infantry) units in one cell.")]
		public readonly bool SharesCell = false;

		[Desc("Can the actor be ordered to move in to shroud?")]
		public readonly bool MoveIntoShroud = true;

		[Desc("e.g. crate, wall, infantry")] public readonly BitSet<CrushClass> Crushes = default(BitSet<CrushClass>);

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

		public virtual bool DisableDomainPassabilityCheck
		{
			get { return false; }
		}

		public virtual object Create(ActorInitializer init)
		{
			return new Locomotor(init.Self, this);
		}
	}

	public class Locomotor : ITick, IWorldLoaded
	{
		struct CellCache : IEquatable<CellCache>
		{
			public readonly int Cost;
			public readonly Blocking Blocking;

			public CellCache(int cost, Blocking blocking)
			{
				Cost = cost;
				Blocking = blocking;
			}

			public bool Equals(CellCache other)
			{
				return Cost == other.Cost && Blocking == other.Blocking;
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				return obj is CellCache && Equals((CellCache)obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return (Cost * 397) ^ Blocking.GetHashCode();
				}
			}
		}

		readonly HashSet<CPos> updatedCells = new HashSet<CPos>();

		public readonly LocomotorInfo Info;
		CellLayer<CellCache> pathabilityCache;
		HashSet<CPos> dirtyCells = new HashSet<CPos>();

		IActorMap actorMap;

		World world;
		LocomotorInfo.TerrainInfo[] terrainInfos;

		public Locomotor(Actor self, LocomotorInfo info)
		{
			Info = info;
		}

		void OnCellsUpdated(CPos cell)
		{
			updatedCells.Add(cell);
		}

		void MapCellEntryChanged(CPos cell)
		{
			dirtyCells.Add(cell);
		}

		void ITick.Tick(Actor self)
		{
			self.World.AddFrameEndTask(s =>
			{
				UpdateCells();

				foreach (var cell in dirtyCells)
				{
					var index = cell.Layer == 0
						? world.Map.GetTerrainIndex(cell)
						: world.GetCustomMovementLayers()[cell.Layer].GetTerrainIndex(cell);

					var cost = int.MaxValue;

					if (index != byte.MaxValue)
						cost = terrainInfos[index].Cost;

					var cellCache = pathabilityCache[cell];
					pathabilityCache[cell] = new CellCache(cost, cellCache.Blocking);
				}

				updatedCells.Clear();
				dirtyCells.Clear();
			});
		}

		void UpdateCells()
		{
			var shareCells = Info.SharesCell;

			foreach (var cell in updatedCells)
			{
				var cellBits = Blocking.Empty;

				var hasFreeSubCell = shareCells && actorMap.HasFreeSubCell(cell);

				if (!hasFreeSubCell)
				{
					var actors = actorMap.GetActorsAt(cell);

					foreach (var actor in actors)
					{
						var actorBits = Blocking.Empty;

						var isMobile = actor.TraitOrDefault<Mobile>();

						var isNotMoving = !(isMobile != null && isMobile.IsMoving);

						if (isNotMoving)
							actorBits |= Blocking.NotMobile;

						var isCrushable = IsCrushable(actor, ref actorBits);

						if (!isCrushable)
							actorBits |= new Blocking(actor.Owner.EnemyMask) | Blocking.IsBlocking;

						cellBits |= actorBits;
					}
				}

				var cellCache = pathabilityCache[cell];
				pathabilityCache[cell] = new CellCache(cellCache.Cost, cellBits);
			}
		}

		bool IsCrushable(Actor actor, ref Blocking actorBits)
		{
			if (Info.Crushes.IsEmpty)
				return false;

			var crushable = actor.TraitOrDefault<Crushable>();

			if (crushable == null)
				return false;

			var crushes = crushable.CrushableInner(Info.Crushes);

			if (!crushes)
				return false;

			actorBits |= Blocking.IsCrushable;

			if (!crushable.Info.CrushedByFriendlies)
				actorBits |= new Blocking(actor.Owner.AllyMask);

			return true;
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			world = w;
			var map = w.Map;
			actorMap = w.ActorMap;
			actorMap.CellsUpdated += OnCellsUpdated;

			pathabilityCache = new CellLayer<CellCache>(map);

			terrainInfos = Info.TilesetTerrainInfo[map.Rules.TileSet];

			foreach (var cell in map.AllCells)
			{
				var index = cell.Layer == 0
					? map.GetTerrainIndex(cell)
					: w.GetCustomMovementLayers()[cell.Layer].GetTerrainIndex(cell);

				var cost = int.MaxValue;

				if (index != byte.MaxValue)
					cost = terrainInfos[index].Cost;

				pathabilityCache[cell] = new CellCache(cost, Blocking.Empty);
			}

			map.CustomTerrain.CellEntryChanged += MapCellEntryChanged;
			map.Tiles.CellEntryChanged += MapCellEntryChanged;
		}

		public int MovementCostForCell(CPos cell)
		{
			if (!world.Map.Contains(cell))
				return int.MaxValue;

			var cellCache = pathabilityCache[cell];

			return cellCache.Cost;
		}

		public int MovementCostToEnterCell(Actor actor, CPos destNode, Actor ignoreActor, CellConditions check)
		{
			if (!world.Map.Contains(destNode))
				return int.MaxValue;

			var cellCache = pathabilityCache[destNode];

			if (cellCache.Cost == int.MaxValue ||
			    !CanMoveFreelyInto(actor, ignoreActor, destNode, check, cellCache.Blocking))
				return int.MaxValue;

			return cellCache.Cost;
		}

		public bool CanEnterCell(Actor self, CPos cell, Actor ignoreActor = null, bool checkTransientActors = true)
		{
			var check = checkTransientActors ? CellConditions.All : CellConditions.BlockedByMovers;
			return MovementCostToEnterCell(self, cell, ignoreActor, check) != int.MaxValue;
		}

		// Determines whether the actor is blocked by other Actors
		public bool CanMoveFreelyInto(Actor actor, Actor ignoreActor, CPos cell, CellConditions check,
			Blocking blocking)
		{
			if (!check.HasCellCondition(CellConditions.TransientActors))
				return true;

			// Empty cell
			if (blocking.IsEmpty)
				return true;

			// If self is null, we don't have a real actor - we're just checking what would happen theoretically.
			// In such a scenario - we'll just assume any other actor in the cell will block us by default.
			// If we have a real actor, we can then perform the extra checks that allow us to avoid being blocked.
			if (actor == null)
				return true;

			var playerMask = new Blocking(actor.Owner.PlayerMask);
			if (Blocking.IsCrushable.DontKnowWhatToCall(blocking) && !playerMask.DontKnowWhatToCall(blocking) &&
			    !Blocking.IsBlocking.DontKnowWhatToCall(blocking))
				return true;

			// static blocking actor, eg buildings or not moving units
			if ((Blocking.NotMobile | Blocking.IsBlocking).DontKnowWhatToCall(blocking))
				return false;

			// enemy actor
			if (playerMask.DontKnowWhatToCall(blocking))
				return false;

			foreach (var otherActor in world.ActorMap.GetActorsAt(cell))
			{
				if (IsBlockedBy(actor, otherActor, ignoreActor, check))
					return false;
			}

			return true;
		}

		bool IsBlockedBy(Actor self, Actor otherActor, Actor ignoreActor, CellConditions check)
		{
			if (otherActor == ignoreActor)
				return false;

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

			return true;
		}

		static bool IsMovingInMyDirection(Actor self, Actor other)
		{
			var otherMobile = other.TraitOrDefault<Mobile>();
			if (otherMobile == null || !otherMobile.IsMoving)
				return false;

			var selfMobile = self.TraitOrDefault<Mobile>();
			if (selfMobile == null)
				return false;

			// Moving in the same direction if the facing delta is between +/- 90 degrees
			var delta = Util.NormalizeFacing(otherMobile.Facing - selfMobile.Facing);
			return delta < 64 || delta > 192;
		}

		public SubCell GetAvailableSubCell(World world, Actor self, CPos cell, SubCell preferredSubCell = SubCell.Any,
			Actor ignoreActor = null, CellConditions check = CellConditions.All)
		{
			if (MovementCostForCell(cell) == int.MaxValue)
				return SubCell.Invalid;

			if (check.HasCellCondition(CellConditions.TransientActors))
			{
				Func<Actor, bool> checkTransient = otherActor => IsBlockedBy(self, otherActor, ignoreActor, check);

				if (!Info.SharesCell)
					return world.ActorMap.AnyActorsAt(cell, SubCell.FullCell, checkTransient)
						? SubCell.Invalid
						: SubCell.FullCell;

				return world.ActorMap.FreeSubCell(cell, preferredSubCell, checkTransient);
			}

			if (!Info.SharesCell)
				return world.ActorMap.AnyActorsAt(cell, SubCell.FullCell) ? SubCell.Invalid : SubCell.FullCell;

			return world.ActorMap.FreeSubCell(cell, preferredSubCell);
		}

		public bool CanMoveFreelyInto(Actor self, CPos cell, Actor ignoreActor, CellConditions check)
		{
			if (!world.Map.Contains(cell))
				return false;

			var cellCache = pathabilityCache[cell];

			return CanMoveFreelyInto(self, ignoreActor, cell, check, cellCache.Blocking);
		}
	}

	public struct Blocking : IEquatable<Blocking>
	{
		readonly int bits;

		public Blocking(int bits)
		{
			this.bits = bits;
		}

		public static Blocking Empty
		{
			get { return new Blocking(0); }
		}

		public static Blocking IsBlocking
		{
			get { return new Blocking(1); }
		}

		public static Blocking NotMobile
		{
			get { return new Blocking(2); }
		}

		public static Blocking IsCrushable
		{
			get { return new Blocking(4); }
		}

		public bool IsEmpty
		{
			get { return bits == 0; }
		}

		public static bool operator ==(Blocking me, Blocking other)
		{
			return me.bits == other.bits;
		}

		public static bool operator !=(Blocking me, Blocking other)
		{
			return !(me == other);
		}

		public static Blocking operator |(Blocking me, Blocking othBlocking)
		{
			return new Blocking(me.bits | othBlocking.bits);
		}

		public bool Equals(Blocking other)
		{
			return other == this;
		}

		public override bool Equals(object obj)
		{
			return obj is Blocking && Equals((Blocking)obj);
		}

		public override int GetHashCode()
		{
			return bits.GetHashCode();
		}

		public bool DontKnowWhatToCall(Blocking other)
		{
			return (bits & other.bits) == bits;
		}
	}
}