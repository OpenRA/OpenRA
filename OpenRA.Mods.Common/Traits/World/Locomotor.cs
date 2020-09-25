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
using OpenRA.Graphics;
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

	[Desc("Used by Mobile. Attach these to the world actor. You can have multiple variants by adding @suffixes.")]
	public class LocomotorInfo : TraitInfo
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

		public virtual bool DisableDomainPassabilityCheck { get { return false; } }

		public override object Create(ActorInitializer init) { return new Locomotor(init.Self, this); }
	}

	public class Locomotor : IWorldLoaded
	{
		struct CellCache
		{
			public readonly LongBitSet<PlayerBitMask> Immovable;
			public readonly LongBitSet<PlayerBitMask> Crushable;
			public readonly CellFlag CellFlag;

			public CellCache(LongBitSet<PlayerBitMask> immovable, CellFlag cellFlag, LongBitSet<PlayerBitMask> crushable = default(LongBitSet<PlayerBitMask>))
			{
				Immovable = immovable;
				Crushable = crushable;
				CellFlag = cellFlag;
			}
		}

		public readonly LocomotorInfo Info;
		public readonly uint MovementClass;
		CellLayer<short> cellsCost;
		CellLayer<CellCache> blockingCache;

		readonly Dictionary<byte, CellLayer<short>> customLayerCellsCost = new Dictionary<byte, CellLayer<short>>();
		readonly Dictionary<byte, CellLayer<CellCache>> customLayerBlockingCache = new Dictionary<byte, CellLayer<CellCache>>();

		readonly LocomotorInfo.TerrainInfo[] terrainInfos;
		readonly World world;
		readonly HashSet<CPos> dirtyCells = new HashSet<CPos>();

		IActorMap actorMap;
		bool sharesCell;

		public Locomotor(Actor self, LocomotorInfo info)
		{
			Info = info;
			sharesCell = info.SharesCell;
			world = self.World;

			var tileSet = world.Map.Rules.TileSet;
			terrainInfos = new LocomotorInfo.TerrainInfo[tileSet.TerrainInfo.Length];
			for (var i = 0; i < terrainInfos.Length; i++)
				if (!info.TerrainSpeeds.TryGetValue(tileSet.TerrainInfo[i].Type, out terrainInfos[i]))
					terrainInfos[i] = LocomotorInfo.TerrainInfo.Impassable;

			MovementClass = (uint)terrainInfos.Select(ti => ti.Cost < short.MaxValue).ToBits();
		}

		public short MovementCostForCell(CPos cell)
		{
			if (!world.Map.Contains(cell))
				return short.MaxValue;

			return cell.Layer == 0 ? cellsCost[cell] : customLayerCellsCost[cell.Layer][cell];
		}

		public int MovementSpeedForCell(CPos cell)
		{
			var index = cell.Layer == 0 ? world.Map.GetTerrainIndex(cell) :
				world.GetCustomMovementLayers()[cell.Layer].GetTerrainIndex(cell);

			return terrainInfos[index].Speed;
		}

		public short MovementCostToEnterCell(Actor actor, CPos destNode, BlockedByActor check, Actor ignoreActor)
		{
			if (!world.Map.Contains(destNode))
				return short.MaxValue;

			var cellCost = destNode.Layer == 0 ? cellsCost[destNode] : customLayerCellsCost[destNode.Layer][destNode];

			if (cellCost == short.MaxValue ||
				!CanMoveFreelyInto(actor, destNode, check, ignoreActor))
				return short.MaxValue;

			return cellCost;
		}

		// Determines whether the actor is blocked by other Actors
		public bool CanMoveFreelyInto(Actor actor, CPos cell, BlockedByActor check, Actor ignoreActor)
		{
			return CanMoveFreelyInto(actor, cell, SubCell.FullCell, check, ignoreActor);
		}

		public bool CanMoveFreelyInto(Actor actor, CPos cell, SubCell subCell, BlockedByActor check, Actor ignoreActor)
		{
			var cellCache = GetCache(cell);
			var cellFlag = cellCache.CellFlag;

			// If the check allows: We are not blocked by transient actors.
			if (check == BlockedByActor.None)
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

			// If the check allows: We are not blocked by moving units.
			if (check <= BlockedByActor.Stationary && !cellFlag.HasCellFlag(CellFlag.HasStationaryActor))
				return true;

			// If the check allows: We are not blocked by units that we can force to move out of the way.
			if (check <= BlockedByActor.Immovable && !cellCache.Immovable.Overlaps(actor.Owner.PlayerMask))
				return true;

			// Cache doesn't account for ignored actors, temporary blockers, or subcells - these must use the slow path.
			if (ignoreActor == null && !cellFlag.HasCellFlag(CellFlag.HasTemporaryBlocker) && subCell == SubCell.FullCell)
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
			if (MovementCostForCell(cell) == short.MaxValue)
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

		bool IsBlockedBy(Actor actor, Actor otherActor, Actor ignoreActor, CPos cell, BlockedByActor check, CellFlag cellFlag)
		{
			if (otherActor == ignoreActor)
				return false;

			// If the check allows: We are not blocked by units that we can force to move out of the way.
			if (check <= BlockedByActor.Immovable && cellFlag.HasCellFlag(CellFlag.HasMovableActor) &&
				actor.Owner.RelationshipWith(otherActor.Owner) == PlayerRelationship.Ally)
			{
				var mobile = otherActor.OccupiesSpace as Mobile;
				if (mobile != null && !mobile.IsTraitDisabled && !mobile.IsTraitPaused && !mobile.IsImmovable)
					return false;
			}

			// If the check allows: we are not blocked by moving units.
			if (check <= BlockedByActor.Stationary && cellFlag.HasCellFlag(CellFlag.HasMovingActor) &&
				IsMoving(actor, otherActor))
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
				var building = otherActor.OccupiesSpace as Building;
				if (building != null && building.TransitOnlyCells().Contains(cell))
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

		static bool IsMoving(Actor self, Actor other)
		{
			// PERF: Because we can be sure that OccupiesSpace is Mobile here we can save some performance by avoiding querying for the trait.
			var otherMobile = other.OccupiesSpace as Mobile;
			if (otherMobile == null || !otherMobile.CurrentMovementTypes.HasMovementType(MovementType.Horizontal))
				return false;

			// PERF: Same here.
			var selfMobile = self.OccupiesSpace as Mobile;
			if (selfMobile == null)
				return false;

			return true;
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			var map = w.Map;
			actorMap = w.ActorMap;
			actorMap.CellUpdated += CellUpdated;

			blockingCache = new CellLayer<CellCache>(map);
			cellsCost = new CellLayer<short>(map);

			foreach (var cell in map.AllCells)
				UpdateCellCost(cell);

			map.CustomTerrain.CellEntryChanged += UpdateCellCost;
			map.Tiles.CellEntryChanged += UpdateCellCost;

			// This section needs to run after WorldLoaded() because we need to be sure that all types of ICustomMovementLayer have been initialized.
			w.AddFrameEndTask(_ =>
			{
				var customMovementLayers = w.WorldActor.TraitsImplementing<ICustomMovementLayer>();
				foreach (var cml in customMovementLayers)
				{
					var cellLayer = new CellLayer<short>(map);
					customLayerCellsCost[cml.Index] = cellLayer;
					customLayerBlockingCache[cml.Index] = new CellLayer<CellCache>(map);

					foreach (var cell in map.AllCells)
					{
						var index = cml.GetTerrainIndex(cell);

						var cost = short.MaxValue;

						if (index != byte.MaxValue)
							cost = terrainInfos[index].Cost;

						cellLayer[cell] = cost;
					}
				}
			});
		}

		CellCache GetCache(CPos cell)
		{
			if (dirtyCells.Contains(cell))
			{
				UpdateCellBlocking(cell);
				dirtyCells.Remove(cell);
			}

			var cache = cell.Layer == 0 ? blockingCache : customLayerBlockingCache[cell.Layer];

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

			var cost = short.MaxValue;

			if (index != byte.MaxValue)
				cost = terrainInfos[index].Cost;

			var cache = cell.Layer == 0 ? cellsCost : customLayerCellsCost[cell.Layer];

			cache[cell] = cost;
		}

		void UpdateCellBlocking(CPos cell)
		{
			using (new PerfSample("locomotor_cache"))
			{
				var cache = cell.Layer == 0 ? blockingCache : customLayerBlockingCache[cell.Layer];

				var actors = actorMap.GetActorsAt(cell);
				var cellFlag = CellFlag.HasFreeSpace;

				if (!actors.Any())
				{
					cache[cell] = new CellCache(default(LongBitSet<PlayerBitMask>), cellFlag);
					return;
				}

				if (sharesCell && actorMap.HasFreeSubCell(cell))
				{
					cache[cell] = new CellCache(default(LongBitSet<PlayerBitMask>), cellFlag);
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

					var building = actor.OccupiesSpace as Building;
					var isTransitOnly = building != null && building.TransitOnlyCells().Contains(cell);

					if (isTransitOnly)
					{
						cellFlag |= CellFlag.HasTransitOnlyActor;
						continue;
					}

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
