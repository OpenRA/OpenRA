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
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;

namespace OpenRA.Traits
{
	class BibLayerInfo : ITraitInfo
	{
		public readonly string[] BibTypes = {"bib3", "bib2", "bib1"};
		public readonly int[] BibWidths = {2,3,4};
		public object Create(ActorInitializer init) { return new BibLayer(init.self, this); }
	}

	class BibLayer: IRenderOverlay, ILoadWorldHook
	{		
		World world;
		BibLayerInfo info;
		
		TileReference<byte,byte>[,] tiles;
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
			tiles = new TileReference<byte,byte>[w.Map.MapSize.X,w.Map.MapSize.Y];
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
				byte type = (byte)((isAdd) ? bib+1 : 0);
				byte index = (byte)i;
				
				tiles[p.X,p.Y] = new TileReference<byte,byte>(type,index);
			}
		}

		public void Render()
		{
			var cliprect = Game.viewport.ShroudBounds().HasValue
				? Rectangle.Intersect(Game.viewport.ShroudBounds().Value, world.Map.Bounds) : world.Map.Bounds;

			var minx = cliprect.Left;
			var maxx = cliprect.Right;

			var miny = cliprect.Top;
			var maxy = cliprect.Bottom;

			for (int x = minx; x < maxx; x++)
				for (int y = miny; y < maxy; y++)
				{
					var t = new int2(x, y);
					if (world.LocalPlayer != null && !world.LocalPlayer.Shroud.IsExplored(t) || tiles[x,y].type == 0) continue;

					Game.Renderer.SpriteRenderer.DrawSprite(bibSprites[tiles[x, y].type - 1][tiles[x, y].image],
						Game.CellSize * t, "terrain");
				}
		}
	}
	
	class BibInfo : TraitInfo<Bib> { }
	public class Bib { }
}
