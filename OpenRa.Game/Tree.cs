using System;
using System.Collections.Generic;
using System.Text;
using OpenRa.FileFormats;
using System.Drawing;

namespace OpenRa.Game
{
	class Tree : Actor
	{
		int2 location;

		public Tree(TreeReference r, TreeCache renderer, Map map)
		{
			location = new int2( r.Location ) - map.Offset;
			currentImages = new Sprite[] { renderer.GetImage(r.Image) };
		}

		Sprite[] currentImages;
		public override Sprite[] CurrentImages { get { return currentImages; } }

		public override float2 RenderLocation { get { return 24 * location; } }
	}
}
