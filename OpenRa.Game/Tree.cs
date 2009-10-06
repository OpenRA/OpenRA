using System;
using System.Collections.Generic;
using System.Text;
using OpenRa.FileFormats;
using System.Drawing;
using OpenRa.Game.Graphics;
using IjwFramework.Types;

namespace OpenRa.Game
{
	class Tree : Actor
	{
		int2 location;

		public Tree(TreeReference r, TreeCache renderer, Map map, Game game)
			: base( game )
		{
			location = new int2( r.Location ) - map.Offset;
			currentImages = new Sprite[] { renderer.GetImage(r.Image) };
		}

		Sprite[] currentImages;
		public override IEnumerable<Pair<Sprite, float2>> CurrentImages
		{
			get
			{
				foreach( var x in currentImages )
					yield return Pair.New( x, 24 * (float2)location );
			}
		}
	}
}
