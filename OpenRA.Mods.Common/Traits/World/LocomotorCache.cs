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
	public class LocomotorCacheInfo : TraitInfo<LocomotorCache> { }

	public class LocomotorCache : IWorldLoaded
	{
		public CellLayer<CellFlag> CellsFlags { get; private set; }
		public CellLayer<LongBitSet<PlayerBitMask>> Immovable { get; private set; }

		public Dictionary<byte, CellLayer<LongBitSet<PlayerBitMask>>> CustomLayerImmovableCache { get; private set; }
		public Dictionary<byte, CellLayer<CellFlag>> CustomLayerCellsFlags { get; private set; }

		public event Action<CPos, List<Pair<Actor, IEnumerable<ICrushable>>>> CrushableUpdated;

		World world;
		IActorMap actorMap;

		readonly List<Pair<Actor, IEnumerable<ICrushable>>> crushableActors = new List<Pair<Actor, IEnumerable<ICrushable>>>();

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			world = w;
			var map = w.Map;
			actorMap = w.ActorMap;
			actorMap.CellUpdated += UpdateCellBlocking;
			Immovable = new CellLayer<LongBitSet<PlayerBitMask>>(map);
			CustomLayerImmovableCache = new Dictionary<byte, CellLayer<LongBitSet<PlayerBitMask>>>();

			CellsFlags = new CellLayer<CellFlag>(map);
			CustomLayerCellsFlags = new Dictionary<byte, CellLayer<CellFlag>>();

			// This section needs to run after WorldLoaded() because we need to be sure that all types of ICustomMovementLayer have been initialized.
			w.AddFrameEndTask(_ =>
			{
				var customMovementLayers = w.WorldActor.TraitsImplementing<ICustomMovementLayer>();
				foreach (var cml in customMovementLayers)
				{
					CustomLayerCellsFlags[cml.Index] = new CellLayer<CellFlag>(map);
					CustomLayerImmovableCache[cml.Index] = new CellLayer<LongBitSet<PlayerBitMask>>(map);
				}

				foreach (var cell in map.AllCells)
					UpdateCellBlocking(cell);
			});
		}

		void UpdateCellBlocking(CPos cell)
		{
			using (new PerfSample("locomotor_cache"))
			{
				var immovable = cell.Layer == 0 ? Immovable : CustomLayerImmovableCache[cell.Layer];
				var flags = cell.Layer == 0 ? CellsFlags : CustomLayerCellsFlags[cell.Layer];

				var actors = actorMap.GetActorsAt(cell);
				var cellFlag = CellFlag.HasFreeSpace;

				if (!actors.Any())
				{
					flags[cell] = CellFlag.HasFreeSpace;
					return;
				}

				if (actorMap.HasFreeSubCell(cell))
					cellFlag |= CellFlag.HasFreeSubCell;

				var cellImmovablePlayers = default(LongBitSet<PlayerBitMask>);

				foreach (var actor in actors)
				{
					var actorImmovablePlayers = world.AllPlayersMask;

					var crushables = actor.TraitsImplementing<ICrushable>();
					var mobile = actor.OccupiesSpace as Mobile;
					var isMovable = mobile != null;
					var isMoving = isMovable && mobile.CurrentMovementTypes.HasMovementType(MovementType.Horizontal);

					if (crushables.Any())
					{
						cellFlag |= CellFlag.HasCrushableActor;
						crushableActors.Add(Pair.New(actor, crushables));
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

					cellImmovablePlayers = cellImmovablePlayers.Union(actorImmovablePlayers);
				}

				immovable[cell] = cellImmovablePlayers;
				flags[cell] = cellFlag;
			}

			UpdateCrushables(cell, crushableActors);
			crushableActors.Clear();
		}

		void UpdateCrushables(CPos cell, List<Pair<Actor, IEnumerable<ICrushable>>> crushables)
		{
			if (CrushableUpdated != null)
				CrushableUpdated(cell, crushables);
		}
	}
}
