using System.Collections.Generic;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.Traits
{
	class Tree : IRender
	{
		Sprite Image;

		public Tree(Sprite treeImage)
		{
			Image = treeImage;
		}

		public IEnumerable<Renderable> Render(Actor self)
		{
			yield return new Renderable(Image, Game.CellSize * (float2)self.Location, 0);
		}
	}
}
