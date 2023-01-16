#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Flags]
	public enum CellFlag : byte
	{
		HasFreeSpace = 0,
		HasMovingActor = 1,
		HasStationaryActor = 2,
		HasMovableActor = 4,
		HasCrushableActor = 8,
		HasTemporaryBlocker = 16,
		HasTransitOnlyActor = 32,
	}

	public static class LocomoterExts
	{
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

	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("Used by Mobile. Attach these to the world actor. You can have multiple variants by adding @suffixes.")]
	public class LocomotorInfo : TraitInfo, NotBefore<ICustomMovementLayerInfo>
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
		public readonly BitSet<CrushClass> Crushes = default;

		[Desc("Types of damage that are caused while crushing. Leave empty for no damage types.")]
		public readonly BitSet<DamageType> CrushDamageTypes = default;

		[FieldLoader.LoadUsing(nameof(LoadSpeeds), true)]
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

		public class TerrainInfo
		{
			public static readonly TerrainInfo Impassable = new TerrainInfo();

			public readonly short Cost;
			public readonly int Speed;

			public TerrainInfo()
			{
				Cost = PathGraph.MovementCostForUnreachableCell;
				Speed = 0;
			}

			public TerrainInfo(int speed, short cost)
			{
				Speed = speed;
				Cost = cost;
			}
		}

		public virtual bool DisableDomainPassabilityCheck => false;

		public override object Create(ActorInitializer init) { return new Locomotor(init.Self, this); }
	}

	public class Locomotor : IWorldLoaded
	{
		readonly struct CellCache
		{
			public readonly LongBitSet<PlayerBitMask> Immovable;
			public readonly LongBitSet<PlayerBitMask> Crushable;
			public readonly CellFlag CellFlag;

			public CellCache(LongBitSet<PlayerBitMask> immovable, CellFlag cellFlag, LongBitSet<PlayerBitMask> crushable = default)
			{
				Immovable = immovable;
				Crushable = crushable;
				CellFlag = cellFlag;
			}
		}

		public readonly LocomotorInfo Info;
		public readonly uint MovementClass;

		/// <summary>
		/// Raised when the movement cost for a cell changes, providing the old and new costs.
		/// </summary>
		public event Action<CPos, short, short> CellCostChanged;

		readonly LocomotorInfo.TerrainInfo[] terrainInfos;
		readonly World world;
		readonly HashSet<CPos> dirtyCells = new HashSet<CPos>();
		readonly bool sharesCell;

		CellLayer<short>[] cellsCost;
		CellLayer<CellCache>[] blockingCache;

		IActorMap actorMap;

		public Locomotor(Actor self, LocomotorInfo info)
		{
			Info = info;
			sharesCell = info.SharesCell;
			world = self.World;

			var terrainInfo = world.Map.Rules.TerrainInfo;
			terrainInfos = new LocomotorInfo.TerrainInfo[terrainInfo.TerrainTypes.Length];
			for (var i = 0; i < terrainInfos.Length; i++)
				if (!info.TerrainSpeeds.TryGetValue(terrainInfo.TerrainTypes[i].Type, out terrainInfos[i]))
					terrainInfos[i] = LocomotorInfo.TerrainInfo.Impassable;

			MovementClass = (uint)terrainInfos.Select(ti => ti.Cost != PathGraph.MovementCostForUnreachableCell).ToBits();
		}

		public short MovementCostForCell(CPos cell)
		{
			return MovementCostForCell(cell, null);
		}

		short MovementCostForCell(CPos cell, CPos? fromCell)
		{
			if (!world.Map.Contains(cell))
				return PathGraph.MovementCostForUnreachableCell;

			// Prevent units from jumping over height discontinuities.
			if (fromCell != null && cell.Layer == 0 && fromCell.Value.Layer == 0 && world.Map.Grid.MaximumTerrainHeight > 0)
			{
				var heightLayer = world.Map.Height;
				if (Math.Abs(heightLayer[cell] - heightLayer[fromCell.Value]) > 1)
					return PathGraph.MovementCostForUnreachableCell;
			}

			return cellsCost[cell.Layer][cell];
		}

		public int MovementSpeedForCell(CPos cell)
		{
			var index = cell.Layer == 0 ? world.Map.GetTerrainIndex(cell) :
				world.GetCustomMovementLayers()[cell.Layer].GetTerrainIndex(cell);

			return terrainInfos[index].Speed;
		}

		public short MovementCostToEnterCell(Actor actor, CPos destNode, BlockedByActor check, Actor ignoreActor, SubCell subCell = SubCell.FullCell)
		{
			var cellCost = MovementCostForCell(destNode);

			if (cellCost == PathGraph.MovementCostForUnreachableCell ||
				!CanMoveFreelyInto(actor, destNode, subCell, check, ignoreActor))
				return PathGraph.MovementCostForUnreachableCell;

			return cellCost;
		}

		public short MovementCostToEnterCell(Actor actor, CPos srcNode, CPos destNode, BlockedByActor check, Actor ignoreActor)
		{
			var cellCost = MovementCostForCell(destNode, srcNode);

			if (cellCost == PathGraph.MovementCostForUnreachableCell ||
				!CanMoveFreelyInto(actor, destNode, SubCell.FullCell, check, ignoreActor))
				return PathGraph.MovementCostForUnreachableCell;

			return cellCost;
		}

		// Determines whether the actor is blocked by other Actors
		bool CanMoveFreelyInto(Actor actor, CPos cell, SubCell subCell, BlockedByActor check, Actor ignoreActor)
		{
			// If the check allows: We are not blocked by other actors.
			if (check == BlockedByActor.None)
				return true;

			var cellCache = GetCache(cell);
			var cellFlag = cellCache.CellFlag;

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

			// If the check allows: We are not blocked by moving units.
			if (check <= BlockedByActor.Stationary && !cellFlag.HasCellFlag(CellFlag.HasStationaryActor))
				return true;

			// If the check allows: We are not blocked by units that we can force to move out of the way.
			if (check <= BlockedByActor.Immovable && !cellCache.Immovable.Overlaps(actor.Owner.PlayerMask))
				return true;

			// Cache doesn't account for ignored actors, subcells, temporary blockers or transit only actors.
			// These must use the slow path.
			if (ignoreActor == null && subCell == SubCell.FullCell &&
				!cellFlag.HasCellFlag(CellFlag.HasTemporaryBlocker) && !cellFlag.HasCellFlag(CellFlag.HasTransitOnlyActor))
			{
				// We already know there are uncrushable actors in the cell so we are always blocked.
				if (check == BlockedByActor.All)
					return false;

				// We already know there are either immovable or stationary actors which the check does not allow.
				if (!cellFlag.HasCellFlag(CellFlag.HasCrushableActor))
					return false;

				// All actors in the cell are immovable and some cannot be crushed.
				if (!cellFlag.HasCellFlag(CellFlag.HasMovableActor))
					return false;

				// All actors in the cell are stationary and some cannot be crushed.
				if (check == BlockedByActor.Stationary && !cellFlag.HasCellFlag(CellFlag.HasMovingActor))
					return false;
			}

			var otherActors = subCell == SubCell.FullCell ? world.ActorMap.GetActorsAt(cell) : world.ActorMap.GetActorsAt(cell, subCell);
			foreach (var otherActor in otherActors)
				if (IsBlockedBy(actor, otherActor, ignoreActor, cell, check, cellFlag))
					return false;

			return true;
		}

		public bool CanStayInCell(CPos cell)
		{
			if (!world.Map.Contains(cell))
				return false;

			return !GetCache(cell).CellFlag.HasCellFlag(CellFlag.HasTransitOnlyActor);
		}

		public SubCell GetAvailableSubCell(Actor self, CPos cell, BlockedByActor check, SubCell preferredSubCell = SubCell.Any, Actor ignoreActor = null)
		{
			if (MovementCostForCell(cell) == PathGraph.MovementCostForUnreachableCell)
				return SubCell.Invalid;

			if (check > BlockedByActor.None)
			{
				Func<Actor, bool> checkTransient = otherActor => IsBlockedBy(self, otherActor, ignoreActor, cell, check, GetCache(cell).CellFlag);

				if (!sharesCell)
					return world.ActorMap.AnyActorsAt(cell, SubCell.FullCell, checkTransient) ? SubCell.Invalid : SubCell.FullCell;

				return world.ActorMap.FreeSubCell(cell, preferredSubCell, checkTransient);
			}

			if (!sharesCell)
				return world.ActorMap.AnyActorsAt(cell, SubCell.FullCell) ? SubCell.Invalid : SubCell.FullCell;

			return world.ActorMap.FreeSubCell(cell, preferredSubCell);
		}

		/// <remarks>This logic is replicated in <see cref="HierarchicalPathFinder.ActorIsBlocking"/> and
		/// <see cref="HierarchicalPathFinder.ActorCellIsBlocking"/>. If this method is updated please update those as
		/// well.</remarks>
		bool IsBlockedBy(Actor actor, Actor otherActor, Actor ignoreActor, CPos cell, BlockedByActor check, CellFlag cellFlag)
		{
			if (otherActor == ignoreActor)
				return false;

			var otherMobile = otherActor.OccupiesSpace as Mobile;
			var otherIsMovable = otherMobile != null && !otherMobile.IsTraitDisabled && !otherMobile.IsTraitPaused && !otherMobile.IsImmovable;
			var otherIsMoving = otherIsMovable && otherMobile.CurrentMovementTypes.HasMovementType(MovementType.Horizontal);

			// If the check allows: We are not blocked by allied units that we can force to move out of the way.
			if (check <= BlockedByActor.Immovable && cellFlag.HasCellFlag(CellFlag.HasMovableActor) && otherIsMovable &&
				actor.Owner.RelationshipWith(otherActor.Owner) == PlayerRelationship.Ally)
				return false;

			// If the check allows: we are not blocked by moving units.
			if (check <= BlockedByActor.Stationary && cellFlag.HasCellFlag(CellFlag.HasMovingActor) && otherIsMoving)
				return false;

			if (cellFlag.HasCellFlag(CellFlag.HasTemporaryBlocker))
			{
				// If there is a temporary blocker in our path, but we can remove it, we are not blocked.
				var temporaryBlocker = otherActor.TraitOrDefault<ITemporaryBlocker>();
				if (temporaryBlocker != null && temporaryBlocker.CanRemoveBlockage(otherActor, actor))
					return false;
			}

			if (cellFlag.HasCellFlag(CellFlag.HasTransitOnlyActor))
			{
				// Transit only tiles should not block movement
				if (otherActor.OccupiesSpace is Building building && building.TransitOnlyCells().Contains(cell))
					return false;
			}

			// If we cannot crush the other actor in our way, we are blocked.
			if (!cellFlag.HasCellFlag(CellFlag.HasCrushableActor) || Info.Crushes.IsEmpty)
				return true;

			// If the other actor in our way cannot be crushed, we are blocked.
			// PERF: Avoid LINQ.
			var crushables = otherActor.TraitsImplementing<ICrushable>();
			foreach (var crushable in crushables)
				if (crushable.CrushableBy(otherActor, actor, Info.Crushes))
					return false;

			return true;
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			var map = w.Map;
			actorMap = w.ActorMap;
			map.CustomTerrain.CellEntryChanged += UpdateCellCost;
			map.Tiles.CellEntryChanged += UpdateCellCost;
			actorMap.CellUpdated += CellUpdated;

			cellsCost = new[] { new CellLayer<short>(map) };
			blockingCache = new[] { new CellLayer<CellCache>(map) };

			foreach (var cell in map.AllCells)
			{
				UpdateCellCost(cell);
				UpdateCellBlocking(cell);
			}

			// NotBefore<> ensures all custom movement layers have been initialized.
			var customMovementLayers = world.GetCustomMovementLayers();
			Array.Resize(ref cellsCost, customMovementLayers.Length);
			Array.Resize(ref blockingCache, customMovementLayers.Length);
			foreach (var cml in customMovementLayers)
			{
				if (cml == null)
					continue;

				var cellLayer = new CellLayer<short>(map);
				cellsCost[cml.Index] = cellLayer;
				blockingCache[cml.Index] = new CellLayer<CellCache>(map);

				foreach (var cell in map.AllCells)
				{
					var index = cml.GetTerrainIndex(cell);

					var cost = PathGraph.MovementCostForUnreachableCell;

					if (index != byte.MaxValue)
						cost = terrainInfos[index].Cost;

					cellLayer[cell] = cost;
				}
			}
		}

		CellCache GetCache(CPos cell)
		{
			if (dirtyCells.Remove(cell))
				UpdateCellBlocking(cell);

			var cache = blockingCache[cell.Layer];

			return cache[cell];
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

			var cost = PathGraph.MovementCostForUnreachableCell;

			if (index != byte.MaxValue)
				cost = terrainInfos[index].Cost;

			var cache = cellsCost[cell.Layer];
			if (CellCostChanged == null)
				cache[cell] = cost;
			else
			{
				var uv = cell.ToMPos(world.Map);
				var oldCost = cache[uv];
				cache[uv] = cost;
				CellCostChanged(cell, oldCost, cost);
			}
		}

		/// <remarks>This logic is replicated in <see cref="HierarchicalPathFinder.ActorIsBlocking"/> and
		/// <see cref="HierarchicalPathFinder.ActorCellIsBlocking"/>. If this method is updated please update those as
		/// well.</remarks>
		void UpdateCellBlocking(CPos cell)
		{
			using (new PerfSample("locomotor_cache"))
			{
				var cache = blockingCache[cell.Layer];

				var actors = actorMap.GetActorsAt(cell);
				var cellFlag = CellFlag.HasFreeSpace;

				if (!actors.Any())
				{
					cache[cell] = new CellCache(default, cellFlag);
					return;
				}

				if (sharesCell && actorMap.HasFreeSubCell(cell))
				{
					cache[cell] = new CellCache(default, cellFlag);
					return;
				}

				var cellImmovablePlayers = default(LongBitSet<PlayerBitMask>);
				var cellCrushablePlayers = world.AllPlayersMask;

				foreach (var actor in actors)
				{
					var actorImmovablePlayers = world.AllPlayersMask;
					var actorCrushablePlayers = world.NoPlayersMask;

					var crushables = actor.TraitsImplementing<ICrushable>();
					var mobile = actor.OccupiesSpace as Mobile;
					var isMovable = mobile != null && !mobile.IsTraitDisabled && !mobile.IsTraitPaused && !mobile.IsImmovable;
					var isMoving = isMovable && mobile.CurrentMovementTypes.HasMovementType(MovementType.Horizontal);

					var isTransitOnly = actor.OccupiesSpace is Building building && building.TransitOnlyCells().Contains(cell);

					if (isTransitOnly)
						cellFlag |= CellFlag.HasTransitOnlyActor;

					if (crushables.Any())
					{
						cellFlag |= CellFlag.HasCrushableActor;
						foreach (var crushable in crushables)
							actorCrushablePlayers = actorCrushablePlayers.Union(crushable.CrushableBy(actor, Info.Crushes));
					}

					if (isMoving)
						cellFlag |= CellFlag.HasMovingActor;
					else
						cellFlag |= CellFlag.HasStationaryActor;

					if (isMovable)
					{
						cellFlag |= CellFlag.HasMovableActor;
						actorImmovablePlayers = actorImmovablePlayers.Except(actor.Owner.AlliedPlayersMask);
					}

					// PERF: Only perform ITemporaryBlocker trait look-up if mod/map rules contain any actors that are temporary blockers
					if (world.RulesContainTemporaryBlocker)
					{
						// If there is a temporary blocker in this cell.
						if (actor.TraitOrDefault<ITemporaryBlocker>() != null)
							cellFlag |= CellFlag.HasTemporaryBlocker;
					}

					cellCrushablePlayers = cellCrushablePlayers.Intersect(actorCrushablePlayers);
					cellImmovablePlayers = cellImmovablePlayers.Union(actorImmovablePlayers);
				}

				cache[cell] = new CellCache(cellImmovablePlayers, cellFlag, cellCrushablePlayers);
			}
		}
	}
}
