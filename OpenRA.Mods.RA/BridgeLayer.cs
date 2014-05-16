#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class BridgeLayerInfo : ITraitInfo
	{
		[ActorReference]
		public readonly string[] Bridges = { "bridge1", "bridge2" };

		public object Create(ActorInitializer init) { return new BridgeLayer(init.self, this); }
	}

	class BridgeLayer : IWorldLoaded
	{
		readonly BridgeLayerInfo info;
		readonly World world;
		Dictionary<ushort, Pair<string, float>> bridgeTypes = new Dictionary<ushort, Pair<string, float>>();
		CellLayer<Bridge> bridges;

		public BridgeLayer(Actor self, BridgeLayerInfo info)
		{
			this.info = info;
			this.world = self.World;
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			bridges = new CellLayer<Bridge>(w.Map);

			// Build a list of templates that should be overlayed with bridges
			foreach (var bridge in info.Bridges)
			{
				var bi = w.Map.Rules.Actors[bridge].Traits.Get<BridgeInfo>();
				foreach (var template in bi.Templates)
					bridgeTypes.Add(template.First, Pair.New(bridge, template.Second));
			}

			// Loop through the map looking for templates to overlay
			for (var i = w.Map.Bounds.Left; i < w.Map.Bounds.Right; i++)
			{
				for (var j = w.Map.Bounds.Top; j < w.Map.Bounds.Bottom; j++)
				{
					var cell = new CPos(i, j);
					if (bridgeTypes.Keys.Contains(w.Map.MapTiles.Value[cell].Type))
						ConvertBridgeToActor(w, cell);
				}
			}

			// Link adjacent (long)-bridges so that artwork is updated correctly
			foreach (var b in w.Actors.SelectMany(a => a.TraitsImplementing<Bridge>()))
				b.LinkNeighbouringBridges(w, this);
		}

		void ConvertBridgeToActor(World w, CPos cell)
		{
			// This cell already has a bridge overlaying it from a previous iteration
			if (bridges[cell] != null)
				return;

			// Correlate the tile "image" aka subtile with its position to find the template origin
			var tile = w.Map.MapTiles.Value[cell].Type;
			var index = w.Map.MapTiles.Value[cell].Index;
			var template = w.TileSet.Templates[tile];
			var ni = cell.X - index % template.Size.X;
			var nj = cell.Y - index / template.Size.X;

			// Create a new actor for this bridge and keep track of which subtiles this bridge includes
			var bridge = w.CreateActor(bridgeTypes[tile].First, new TypeDictionary
			{
				new LocationInit(new CPos(ni, nj)),
				new OwnerInit(w.WorldActor.Owner),
				new HealthInit(bridgeTypes[tile].Second),
			}).Trait<Bridge>();

			var subTiles = new Dictionary<CPos, byte>();

			// For each subtile in the template
			for (byte ind = 0; ind < template.Size.X * template.Size.Y; ind++)
			{
				// Where do we expect to find the subtile
				var subtile = new CPos(ni + ind % template.Size.X, nj + ind / template.Size.X);

				// This isn't the bridge you're looking for
				if (!w.Map.IsInMap(subtile) || w.Map.MapTiles.Value[subtile].Type != tile ||
					w.Map.MapTiles.Value[subtile].Index != ind)
					continue;

				subTiles.Add(subtile, ind);
				bridges[subtile] = bridge;
			}

			bridge.Create(tile, subTiles);
		}

		// Used to check for neighbouring bridges
		public Bridge GetBridge(CPos cell)
		{
			if (!world.Map.IsInMap(cell))
				return null;

			return bridges[cell];
		}
	}
}
