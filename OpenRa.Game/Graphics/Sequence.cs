using System.Xml;
using Ijw.DirectX;

namespace OpenRa.Game.Graphics
{
	class Sequence
	{
		readonly Sprite[] sprites;
		readonly int start, length;

		public readonly string Name;
		public int Start { get { return start; } }
		public int End { get { return start + length; } }
		public int Length { get { return length; } }

		public Sequence(string unit, XmlElement e)
		{
			string srcOverride = e.GetAttribute("src");
			Name = e.GetAttribute("name");

			sprites = SpriteSheetBuilder.LoadAllSprites(string.IsNullOrEmpty(srcOverride) ? unit : srcOverride,
				".tem", ".sno", ".int", ".shp" );
			start = int.Parse(e.GetAttribute("start"));

			if (e.GetAttribute("length") == "*" || e.GetAttribute("end") == "*")
				length = sprites.Length - Start;
			else if (e.HasAttribute("length"))
				length = int.Parse(e.GetAttribute("length"));
			else if (e.HasAttribute("end"))
				length = int.Parse(e.GetAttribute("end")) - int.Parse(e.GetAttribute("start"));
			else
				length = 1;
		}

		public Sprite GetSprite(int frame)
		{
			return sprites[ ( frame % length ) + start ];
		}
	}
}
