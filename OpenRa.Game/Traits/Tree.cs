using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Graphics;
using IjwFramework.Types;

namespace OpenRa.Game.Traits
{
	class Tree : IRender
	{
		Sprite Image;

		public Tree(Sprite treeImage)
		{
			Image = treeImage;
		}

		public IEnumerable<Pair<Sprite, float2>> Render(Actor self)
		{
			yield return Pair.New(Image, 24 * (float2)self.Location);
		}
	}
}
