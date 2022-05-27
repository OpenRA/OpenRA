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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[TraitLocation(SystemActors.World)]
	class TSResourceLayerInfo : ResourceLayerInfo
	{
		public readonly string VeinType = "Veins";

		[ActorReference]
		[Desc("Actor types that should be treated as veins for adjacency.")]
		public readonly HashSet<string> VeinholeActors = new HashSet<string> { };

		public override object Create(ActorInitializer init) { return new TSResourceLayer(init.Self, this); }
	}

	class TSResourceLayer : ResourceLayer, INotifyActorDisposing
	{
		readonly TSResourceLayerInfo info;
		readonly HashSet<CPos> veinholeCells = new HashSet<CPos>();

		public TSResourceLayer(Actor self, TSResourceLayerInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		protected override void WorldLoaded(World w, WorldRenderer wr)
		{
			// Cache locations of veinhole actors
			w.ActorAdded += ActorAddedToWorld;
			w.ActorRemoved += ActorRemovedFromWorld;
			foreach (var a in w.Actors)
				ActorAddedToWorld(a);

			base.WorldLoaded(w, wr);
		}

		void ActorAddedToWorld(Actor a)
		{
			if (info.VeinholeActors.Contains(a.Info.Name))
				foreach (var cell in a.OccupiesSpace.OccupiedCells())
					veinholeCells.Add(cell.Cell);
		}

		void ActorRemovedFromWorld(Actor a)
		{
			if (info.VeinholeActors.Contains(a.Info.Name))
				foreach (var cell in a.OccupiesSpace.OccupiedCells())
					veinholeCells.Remove(cell.Cell);
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			self.World.ActorAdded -= ActorAddedToWorld;
			self.World.ActorRemoved -= ActorRemovedFromWorld;
		}

		bool IsValidResourceNeighbour(CPos cell, CPos neighbour)
		{
			if (!Map.Contains(neighbour))
				return false;

			// Non-vein resources are not allowed in the cardinal neighbours to
			// an already existing vein cell
			return Content[neighbour].Type != info.VeinType;
		}

		bool IsValidVeinNeighbour(CPos cell, CPos neighbour)
		{
			if (!Map.Contains(neighbour))
				return false;

			// Cell is automatically valid if it contains a veinhole actor
			if (veinholeCells.Contains(neighbour))
				return true;

			// Neighbour must be flat or a cardinal slope, unless the resource cell itself is a slope
			if (Map.Ramp[cell] == 0 && Map.Ramp[neighbour] > 4)
				return false;

			// Neighbour must be have a compatible terrain type (which also implies no other resources)
			var neighbourTerrain = Map.GetTerrainInfo(neighbour).Type;
			var veinInfo = info.ResourceTypes[info.VeinType];
			return neighbourTerrain == veinInfo.TerrainType || veinInfo.AllowedTerrainTypes.Contains(neighbourTerrain);
		}

		protected override bool AllowResourceAt(string resourceType, CPos cell)
		{
			if (!Map.Contains(cell))
				return false;

			// Resources are allowed on flat terrain and cardinal slopes
			if (Map.Ramp[cell] > 4)
				return false;

			if (!info.ResourceTypes.TryGetValue(resourceType, out var resourceInfo))
				return false;

			if (!resourceInfo.AllowedTerrainTypes.Contains(Map.GetTerrainInfo(cell).Type))
				return false;

			// Ensure there is space for the vein border tiles (not needed on ramps)
			var check = resourceType == info.VeinType ? (Func<CPos, CPos, bool>)IsValidVeinNeighbour : IsValidResourceNeighbour;
			var blockedByNeighbours = Map.Ramp[cell] == 0 && !Common.Util.ExpandFootprint(cell, false).All(c => check(cell, c));

			return !blockedByNeighbours && (resourceType == info.VeinType || !BuildingInfluence.AnyBuildingAt(cell));
		}
	}
}
