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

	class Bridge : IRender, ITick, ICustomTerrain
	{
		Dictionary<int2, int> Tiles;
		TileTemplate Template;
		Dictionary<int2, Sprite> TileSprites;

		public Bridge(Actor self) { self.RemoveOnDeath = false; }

		static Cache<TileReference, Sprite> Sprites =
			new Cache<TileReference, Sprite>(
				x => SheetBuilder.Add(Game.world.TileSet.GetBytes(x), 
					new Size(Game.CellSize, Game.CellSize)));

		public IEnumerable<Renderable> Render(Actor self)
		{
			if (Template == null) yield break;
			foreach (var t in TileSprites)
				yield return new Renderable(t.Value, Game.CellSize * t.Key, PaletteType.Gold);
		}

		public void Tick(Actor self) {}

		public void SetTiles(TileTemplate template, Dictionary<int2, int> replacedTiles)
		{
			Template = template;
			Tiles = replacedTiles;

			foreach (var t in replacedTiles.Keys)
				Game.world.customTerrain[t.X, t.Y] = this;

			TileSprites = replacedTiles.ToDictionary(
				a => a.Key,
				a => Sprites[new TileReference { tile = (ushort)template.Index, image = (byte)a.Value }]);
		}

		public double GetCost(int2 p, UnitMovementType umt)
		{
			var origTile = Tiles[p];	// if this explodes, then SetTiles did something horribly wrong.
			return 1.0;
		}
	}
}
