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

	class Bridge : IRender, ITick
	{
		Animation anim;

		public Bridge() {}

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
			/* todo: stash these, etc */
		}
	}
}
