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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public interface IBridgeSegment
	{
		void Repair(Actor repairer);
		void Demolish(Actor saboteur, BitSet<DamageType> damageTypes);
		void SetNeighbours(IEnumerable<IBridgeSegment> neighbours);

		string Type { get; }
		DamageState DamageState { get; }
		IEnumerable<CPos> Footprint { get; }
		bool Valid { get; }
		CPos Location { get; }
	}

	[TraitLocation(SystemActors.World)]
	sealed class BridgeLayerInfo : TraitInfo
	{
		[ActorReference]
		[Desc("Actors to spawn on bridge terrain.")]
		public readonly string[] Bridges = Array.Empty<string>();

		public override object Create(ActorInitializer init) { return new BridgeLayer(init.Self, this); }
	}

	sealed class BridgeLayer : IWorldLoaded
	{
		readonly CellLayer<Actor> bridges;
		readonly BridgeLayerInfo info;

		readonly Dictionary<ushort, (string Template, int Health)> bridgeTypes = new();
		readonly ITemplatedTerrainInfo terrainInfo;

		public BridgeLayer(Actor self, BridgeLayerInfo info)
		{
			this.info = info;
			bridges = new CellLayer<Actor>(self.World.Map);

			if (info.Bridges.Length > 0)
			{
				terrainInfo = self.World.Map.Rules.TerrainInfo as ITemplatedTerrainInfo;
				if (terrainInfo == null)
					throw new InvalidDataException($"{nameof(BridgeLayer)} requires a template-based tileset.");
			}
		}

		public Actor this[CPos cell] => bridges[cell];

		public void Add(Actor b)
		{
			var buildingInfo = b.Info.TraitInfo<BuildingInfo>();
			foreach (var c in buildingInfo.PathableTiles(b.Location))
				bridges[c] = b;
		}

		public void Remove(Actor b)
		{
			var buildingInfo = b.Info.TraitInfo<BuildingInfo>();
			foreach (var c in buildingInfo.PathableTiles(b.Location))
				if (bridges[c] == b)
					bridges[c] = null;
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			if (info.Bridges.Length > 0)
			{
				// Build a list of templates that should be overlaid with bridges.
				foreach (var bridge in info.Bridges)
				{
					var bi = w.Map.Rules.Actors[bridge].TraitInfo<BridgeInfo>();
					foreach (var template in bi.Templates)
						bridgeTypes.Add(template.Template, (bridge, template.Health));
				}

				// Take all templates to overlay from the map.
				foreach (var cell in w.Map.AllCells.Where(cell => bridgeTypes.ContainsKey(w.Map.Tiles[cell].Type)))
					ConvertBridgeToActor(w, cell);
			}
		}

		void ConvertBridgeToActor(World w, CPos cell)
		{
			// This cell already has a bridge overlaying it from a previous iteration.
			if (bridges[cell] != null)
				return;

			// Correlate the tile "image" aka subtile with its position to find the template origin.
			var ti = w.Map.Tiles[cell];
			var tile = ti.Type;
			var index = ti.Index;
			var template = terrainInfo.Templates[tile];
			var location = new CPos(cell.X - index % template.Size.X, cell.Y - index / template.Size.X);

			// Create a new actor for this bridge and keep track of which subtiles this bridge includes.
			var bridge = w.CreateActor(bridgeTypes[tile].Template, new TypeDictionary
			{
				new LocationInit(location),
				new OwnerInit(w.WorldActor.Owner),
				new HealthInit(bridgeTypes[tile].Health, true),
			});

			var subTiles = new Dictionary<CPos, byte>();
			var mapTiles = w.Map.Tiles;

			// For each subtile in the template.
			for (byte i = 0; i < template.Size.X * template.Size.Y; i++)
			{
				// Where do we expect to find the subtile.
				var subtilePosition = new CPos(location.X + i % template.Size.X, location.Y + i / template.Size.X);

				if (!mapTiles.Contains(subtilePosition))
					continue;

				// This isn't the bridge you're looking for.
				var subtile = mapTiles[subtilePosition];
				if (subtile.Type != tile || subtile.Index != i)
					continue;

				subTiles.Add(subtilePosition, i);
				bridges[subtilePosition] = bridge;
			}

			bridge.Trait<Bridge>().Create(tile, subTiles);
		}
	}
}
