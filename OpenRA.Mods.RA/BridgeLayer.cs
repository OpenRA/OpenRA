#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class BridgeLayerInfo : ITraitInfo
	{
		[ActorReference]
		public readonly string[] Bridges = {"bridge1", "bridge2"};

		public object Create(ActorInitializer init) { return new BridgeLayer(init.self, this); }
	}

	class BridgeLayer : IWorldLoaded
	{
		readonly BridgeLayerInfo Info;
		readonly World world;
		Dictionary<ushort, Pair<string, float>> BridgeTypes = new Dictionary<ushort, Pair<string,float>>();
		Bridge[,] Bridges;

		public BridgeLayer(Actor self, BridgeLayerInfo Info)
		{
			this.Info = Info;
			this.world = self.World;
		}

		public void WorldLoaded(World w)
		{
			Bridges = new Bridge[w.Map.MapSize.X, w.Map.MapSize.Y];

			// Build a list of templates that should be overlayed with bridges
			foreach(var bridge in Info.Bridges)
			{
				var bi = Rules.Info[bridge].Traits.Get<BridgeInfo>();
				foreach (var template in bi.Templates)
					BridgeTypes.Add(template.First, Pair.New(bridge, template.Second));
			}

			// Loop through the map looking for templates to overlay
			for (int i = w.Map.Bounds.Left; i < w.Map.Bounds.Right; i++)
				for (int j = w.Map.Bounds.Top; j < w.Map.Bounds.Bottom; j++)
					if (BridgeTypes.Keys.Contains(w.Map.MapTiles.Value[i, j].type))
							ConvertBridgeToActor(w, i, j);

			// Link adjacent (long)-bridges so that artwork is updated correctly
			foreach (var b in w.Actors.SelectMany(a => a.TraitsImplementing<Bridge>()))
				b.LinkNeighbouringBridges(w,this);
		}

		void ConvertBridgeToActor(World w, int i, int j)
		{
			// This cell already has a bridge overlaying it from a previous iteration
			if (Bridges[i,j] != null)
				return;

			// Correlate the tile "image" aka subtile with its position to find the template origin
			var tile = w.Map.MapTiles.Value[i, j].type;
			var index = w.Map.MapTiles.Value[i, j].index;
			var template = w.TileSet.Templates[tile];
			var ni = i - index % template.Size.X;
			var nj = j - index / template.Size.X;

			// Create a new actor for this bridge and keep track of which subtiles this bridge includes
			var bridge = w.CreateActor(BridgeTypes[tile].First, new TypeDictionary
			{
				new LocationInit( new CPos(ni, nj) ),
				new OwnerInit( w.WorldActor.Owner ),
				new HealthInit( BridgeTypes[tile].Second ),
			}).Trait<Bridge>();

			var subTiles = new Dictionary<CPos, byte>();

			// For each subtile in the template
			for (byte ind = 0; ind < template.Size.X*template.Size.Y; ind++)
			{
				// Where do we expect to find the subtile
				var x = ni + ind % template.Size.X;
				var y = nj + ind / template.Size.X;

				// This isn't the bridge you're looking for
				if (!w.Map.IsInMap(x, y) || w.Map.MapTiles.Value[x, y].type != tile ||
				    w.Map.MapTiles.Value[x, y].index != ind)
					continue;

				subTiles.Add(new CPos(x, y), ind);
				Bridges[x,y] = bridge;
			}
			bridge.Create(tile, subTiles);
		}

		// Used to check for neighbouring bridges
		public Bridge GetBridge(CPos cell)
		{
			if (!world.Map.IsInMap(cell))
				return null;

			return Bridges[ cell.X, cell.Y ];
		}
	}
}
