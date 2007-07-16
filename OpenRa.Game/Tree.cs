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
			renderLocation = 24 * (new float2(r.Location) - new float2(map.XOffset, map.YOffset));
			currentImages = new Sprite[] { renderer.GetImage(r.Image) };
		}

		Sprite[] currentImages;
		public override Sprite[] CurrentImages
		{
			get { return currentImages; }
		}

		public override void Tick(double t){}
	}
}
