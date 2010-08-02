#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;
using OpenRA.FileFormats;

namespace OpenRA.Mods.RA
{
	class BridgeLayerInfo : ITraitInfo
	{
		[ActorReference]
		public readonly string[] Bridges = {"bridge1", "bridge2"};

		public object Create(ActorInitializer init) { return new BridgeLayer(init.self, this); }
	}

	class BridgeLayer : ILoadWorldHook
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
				{
					BridgeTypes.Add(template.First, Pair.New(bridge, template.Second));
					Log.Write("debug", "Adding template {0} for bridge {1}", template, bridge);
				}
			}
			
			// Loop through the map looking for templates to overlay
			var tl = w.Map.TopLeft;
			var br = w.Map.BottomRight;
			
			for (int i = tl.X; i < br.X; i++)
				for (int j = tl.Y; j < br.Y; j++)
					if (BridgeTypes.Keys.Contains(w.Map.MapTiles[i, j].type))
							ConvertBridgeToActor(w, i, j);
			
			// Link adjacent (long)-bridges so that artwork is updated correctly
			foreach (var b in w.Actors.SelectMany(a => a.traits.WithInterface<Bridge>()))
				b.LinkNeighbouringBridges(w,this);
		}
		
		void ConvertBridgeToActor(World w, int i, int j)
		{
			Log.Write("debug", "Converting bridge at {0} {1}", i, j);
			
			// This cell already has a bridge overlaying it from a previous iteration
			if (Bridges[i,j] != null)
				return;
			
			// Correlate the tile "image" aka subtile with its position to find the template origin
			var tile = w.Map.MapTiles[i, j].type;
			var image = w.Map.MapTiles[i, j].image;
			var template = w.TileSet.Templates[tile];
			var ni = i - image % template.Size.X;
			var nj = j - image / template.Size.X;
			
			// Create a new actor for this bridge and keep track of which subtiles this bridge includes
			var bridge = w.CreateActor(BridgeTypes[tile].First, new TypeDictionary
			{
				new LocationInit( new int2(ni, nj) ),
				new OwnerInit( w.WorldActor.Owner ),
				new HealthInit( BridgeTypes[tile].Second ),
			}).traits.Get<Bridge>();
			
			Dictionary<int2, byte> subTiles = new Dictionary<int2, byte>();
			
			// For each subtile in the template
			for (byte ind = 0; ind < template.Size.X*template.Size.Y; ind++)
			{
				// Is this tile actually included in the bridge template?
				if (!template.Tiles.Keys.Contains(ind))
					continue;
				
				// Where do we expect to find the subtile 
				var x = ni + ind % template.Size.X;
				var y = nj + ind / template.Size.X;
				
				// This isn't the bridge you're looking for
				if (!w.Map.IsInMap(x, y) || w.Map.MapTiles[x, y].image != ind)
					continue;
				
				Log.Write("debug", "Adding tile {0} {1} for type {2}", x,y,tile);
				
				subTiles.Add(new int2(x,y),ind);
				Bridges[x,y] = bridge;
			}
			bridge.Create(tile, subTiles);
		}
		
		// Used to check for neighbouring bridges
		public Bridge GetBridge(int2 cell)
		{
			if (!world.Map.IsInMap(cell.X, cell.Y))
				return null;
			
			return Bridges[ cell.X, cell.Y ];
		}
	}
}
