using System;
using System.Collections.Generic;
using System.Text;
using OpenRa.FileFormats;
using System.Drawing;

namespace OpenRa.Game
{
	class Tree : Actor
	{
		public Tree(TreeReference r, TreeCache renderer, Map map)
		{
			location = new PointF(24 * (r.X - map.XOffset), 24 * (r.Y - map.YOffset));
			currentImages = new Sprite[] { renderer.GetImage(r.Image) };
		}

		Sprite[] currentImages;
		public override Sprite[] CurrentImages
		{
			get { return currentImages; }
		}
	}
}
