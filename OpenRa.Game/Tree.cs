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
			currentImages = new SheetRectangle<Sheet>[] { renderer.GetImage(r.Image) };
		}

		SheetRectangle<Sheet>[] currentImages;
		public override SheetRectangle<Sheet>[] CurrentImages
		{
			get { return currentImages; }
		}
	}
}
