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
		public readonly bool Long = false;
		public object Create(Actor self) { return new Bridge(self); }
	}

	class Bridge : IRender, ICustomTerrain
	{
		Dictionary<int2, int> Tiles;
		List<Dictionary<int2, Sprite>> TileSprites = new List<Dictionary<int2,Sprite>>();
		List<TileTemplate> Templates = new List<TileTemplate>();
		Actor self;
		int state;

		public Bridge(Actor self) { this.self = self; self.RemoveOnDeath = false; }

		static string cachedTheater;
		static Cache<TileReference, Sprite> sprites;

		public IEnumerable<Renderable> Render(Actor self)
		{
			foreach (var t in TileSprites[state])
				yield return new Renderable(t.Value, Game.CellSize * t.Key, PaletteType.Gold);
		}

		public void SetTiles(World world, TileTemplate template, Dictionary<int2, int> replacedTiles)
		{
			Tiles = replacedTiles;
			state = template.Name[template.Name.Length - 1] - 'a';

			foreach (var t in replacedTiles.Keys)
				world.customTerrain[t.X, t.Y] = this;

			if (cachedTheater != world.Map.Theater)
			{
				cachedTheater = world.Map.Theater;
				sprites = new Cache<TileReference, Sprite>(
				x => SheetBuilder.Add(world.TileSet.GetBytes(x),
					new Size(Game.CellSize, Game.CellSize)));
			}

			var numStates = self.Info.Traits.Get<BridgeInfo>().Long ? 6 : 3;
			for (var n = 0; n < numStates; n++)
			{
				var stateTemplate = world.TileSet.Walkability.GetWalkability(template.Bridge + (char)(n + 'a'));
				Templates.Add( stateTemplate );

				TileSprites.Add(replacedTiles.ToDictionary(
					a => a.Key,
					a => sprites[new TileReference { tile = (ushort)stateTemplate.Index, image = (byte)a.Value }]));
			}

			self.Health = (int)(self.GetMaxHP() * template.HP);
		}

		public void FinalizeBridges(World world)
		{
			// go looking for our neighbors
		}

		public float GetCost(int2 p, UnitMovementType umt)
		{
			// just use the standard walkability from templates.ini. no hackery.

			return TerrainCosts.Cost(umt, 
				Templates[state].TerrainType[Tiles[p]]);
		}
	}
}
