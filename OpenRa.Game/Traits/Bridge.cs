using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Graphics;
using OpenRa.FileFormats;
using IjwFramework.Collections;
using System.Drawing;

namespace OpenRa.Traits
{
	class BridgeInfo : ITraitInfo
	{
		public object Create(Actor self) { return new Bridge(self); }
	}

	class Bridge : IRender, ICustomTerrain
	{
		Dictionary<int2, int> Tiles;
		TileTemplate Template;
		Dictionary<int2, Sprite> TileSprites;
		Actor self;

		public Bridge(Actor self) { this.self = self; self.RemoveOnDeath = false; }

		static string cachedTheater;
		static Cache<TileReference, Sprite> sprites;

		public IEnumerable<Renderable> Render(Actor self)
		{
			if (Template == null) yield break;
			foreach (var t in TileSprites)
				yield return new Renderable(t.Value, Game.CellSize * t.Key, PaletteType.Gold);
		}

		public void SetTiles(World world, TileTemplate template, Dictionary<int2, int> replacedTiles)
		{
			Template = template;
			Tiles = replacedTiles;

			foreach (var t in replacedTiles.Keys)
				world.customTerrain[t.X, t.Y] = this;

			if (cachedTheater != world.Map.Theater)
			{
				cachedTheater = world.Map.Theater;
				sprites = new Cache<TileReference, Sprite>(
				x => SheetBuilder.Add(world.TileSet.GetBytes(x),
					new Size(Game.CellSize, Game.CellSize)));
			}

			TileSprites = replacedTiles.ToDictionary(
				a => a.Key,
				a => sprites[new TileReference { tile = (ushort)template.Index, image = (byte)a.Value }]);
		}

		public float GetCost(int2 p, UnitMovementType umt)
		{
			return self.Health > 0
				? TerrainCosts.Cost(umt, Template.TerrainType[Tiles[p]])
				: TerrainCosts.Cost(umt, 1);
		}
	}
}
