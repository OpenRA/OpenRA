#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World)]
	public class LegacyBridgeLayerInfo : TraitInfo
	{
		[ActorReference]
		public readonly string[] Bridges = { "bridge1", "bridge2" };

		public override object Create(ActorInitializer init) { return new LegacyBridgeLayer(init.Self, this); }
	}

	public class LegacyBridgeLayer : IWorldLoaded
	{
		readonly LegacyBridgeLayerInfo info;
		readonly Dictionary<ushort, (string Template, int Health)> bridgeTypes = new Dictionary<ushort, (string, int)>();
		readonly ITemplatedTerrainInfo terrainInfo;

		CellLayer<Bridge> bridges;

		public LegacyBridgeLayer(Actor self, LegacyBridgeLayerInfo info)
		{
			this.info = info;
			terrainInfo = self.World.Map.Rules.TerrainInfo as ITemplatedTerrainInfo;
			if (terrainInfo == null)
				throw new InvalidDataException("LegacyBridgeLayer requires a template-based tileset.");
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			bridges = new CellLayer<Bridge>(w.Map);

			// Build a list of templates that should be overlayed with bridges
			foreach (var bridge in info.Bridges)
			{
				var bi = w.Map.Rules.Actors[bridge].TraitInfo<BridgeInfo>();
				foreach (var template in bi.Templates)
					bridgeTypes.Add(template.Template, (bridge, template.Health));
			}

			// Take all templates to overlay from the map
			foreach (var cell in w.Map.AllCells.Where(cell => bridgeTypes.ContainsKey(w.Map.Tiles[cell].Type)))
				ConvertBridgeToActor(w, cell);

			// Link adjacent (long)-bridges so that artwork is updated correctly
			foreach (var p in w.ActorsWithTrait<Bridge>())
				p.Trait.LinkNeighbouringBridges(this);
		}

		void ConvertBridgeToActor(World w, CPos cell)
		{
			// This cell already has a bridge overlaying it from a previous iteration
			if (bridges[cell] != null)
				return;

			// Correlate the tile "image" aka subtile with its position to find the template origin
			var ti = w.Map.Tiles[cell];
			var tile = ti.Type;
			var index = ti.Index;
			var template = terrainInfo.Templates[tile];
			var ni = cell.X - index % template.Size.X;
			var nj = cell.Y - index / template.Size.X;

			// Create a new actor for this bridge and keep track of which subtiles this bridge includes
			var bridge = w.CreateActor(bridgeTypes[tile].Template, new TypeDictionary
			{
				new LocationInit(new CPos(ni, nj)),
				new OwnerInit(w.WorldActor.Owner),
				new HealthInit(bridgeTypes[tile].Health, true),
			}).Trait<Bridge>();

			var subTiles = new Dictionary<CPos, byte>();
			var mapTiles = w.Map.Tiles;

			// For each subtile in the template
			for (byte ind = 0; ind < template.Size.X * template.Size.Y; ind++)
			{
				// Where do we expect to find the subtile
				var subtile = new CPos(ni + ind % template.Size.X, nj + ind / template.Size.X);

				if (!mapTiles.Contains(subtile))
					continue;

				// This isn't the bridge you're looking for
				var subti = mapTiles[subtile];
				if (subti.Type != tile || subti.Index != ind)
					continue;

				subTiles.Add(subtile, ind);
				bridges[subtile] = bridge;
			}

			bridge.Create(tile, subTiles);
		}

		// Used to check for neighbouring bridges
		public Bridge GetBridge(CPos cell)
		{
			if (!bridges.Contains(cell))
				return null;

			return bridges[cell];
		}
	}
}
