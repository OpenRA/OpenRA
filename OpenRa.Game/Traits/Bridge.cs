using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Graphics;
using OpenRa.FileFormats;

namespace OpenRa.Traits
{
	class BridgeInfo : ITraitInfo
	{
		public object Create(Actor self) { return new Bridge(); }
	}

	class Bridge : IRender, ITick, ICustomTerrain
	{
		Animation anim;
		Dictionary<int2, int> Tiles;
		TileTemplate Template;

		public IEnumerable<Renderable> Render(Actor self)
		{
			if (anim != null)
				return new[] { Util.Centered(self, anim.Image, self.CenterLocation) };
			else
				return new Renderable[] { };
		}

		public void Tick(Actor self)
		{
			if (anim == null)
			{
				anim = new Animation("3tnk");
				anim.PlayRepeating("idle");
			}
		}

		public void SetTiles(TileTemplate template, Dictionary<int2, int> replacedTiles)
		{
			Template = template;
			Tiles = replacedTiles;

			foreach (var t in replacedTiles.Keys)
				Game.world.customTerrain[t.X, t.Y] = this;
		}

		public double GetCost(int2 p, UnitMovementType umt)
		{
			var origTile = Tiles[p];	// if this explodes, then SetTiles did something horribly wrong.

			return 1.0;
		}
	}
}
