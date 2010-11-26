#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Buildings
{
	class BibLayerInfo : ITraitInfo
	{
		public readonly string[] BibTypes = {"bib3", "bib2", "bib1"};
		public readonly int[] BibWidths = {2,3,4};
		public object Create(ActorInitializer init) { return new BibLayer(init.self, this); }
	}

	class BibLayer: IRenderOverlay, IWorldLoaded
	{		
		World world;
		BibLayerInfo info;
		Dictionary<int2, TileReference<byte, byte>> tiles;
		Sprite[][] bibSprites;
		
		public BibLayer(Actor self, BibLayerInfo info)
		{
			this.info = info;
			bibSprites = info.BibTypes.Select(x => SpriteSheetBuilder.LoadAllSprites(x)).ToArray();
			
			self.World.ActorAdded +=
				a => { if (a.HasTrait<Bib>()) DoBib(a,true); };
			self.World.ActorRemoved +=
				a => { if (a.HasTrait<Bib>()) DoBib(a,false); };
		}
		
		public void WorldLoaded(World w)
		{
			world = w;
			tiles = new Dictionary<int2, TileReference<byte, byte>>();
		}
		
		public void DoBib(Actor b, bool isAdd)
		{
			var buildingInfo = b.Info.Traits.Get<BuildingInfo>();			
			var size = buildingInfo.Dimensions.X;
			var bibOffset = buildingInfo.Dimensions.Y - 1;
			
			int bib = Array.IndexOf(info.BibWidths,size);
			if (bib < 0)
			{
				Log.Write("debug", "Cannot bib {0}-wide building {1}", size, b.Info.Name);
				return;
			}
			
			for (int i = 0; i < 2 * size; i++)
			{
				var p = b.Location + new int2(i % size, i / size + bibOffset);
				if (isAdd)
					tiles[p] = new TileReference<byte, byte>((byte)((isAdd) ? bib + 1 : 0), (byte)i);
				else
					tiles.Remove(p);
			}
		}

		public void Render( WorldRenderer wr )
		{
			var cliprect = Game.viewport.WorldBounds(world);
			foreach (var kv in tiles)
			{
				if (!cliprect.Contains(kv.Key.X, kv.Key.Y))
					continue;
				if (!world.LocalShroud.IsExplored(kv.Key))
					continue;

				bibSprites[kv.Value.type - 1][kv.Value.image].DrawAt( wr,
					Game.CellSize * kv.Key, "terrain");
			}
		}
	}
	
	public class BibInfo : TraitInfo<Bib> { }
	public class Bib { }
}
