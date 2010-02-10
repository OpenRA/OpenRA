using System.Xml;

namespace OpenRa.Graphics
{
	public class Sequence
	{
		readonly Sprite[] sprites;
		readonly int start, length, facings;

		public readonly string Name;
		public int Start { get { return start; } }
		public int End { get { return start + length; } }
		public int Length { get { return length; } }
		public int Facings { get { return facings; } }

		public Sequence(string unit, XmlElement e)
		{
			string srcOverride = e.GetAttribute("src");
			Name = e.GetAttribute("name");

			sprites = SpriteSheetBuilder.LoadAllSprites(string.IsNullOrEmpty(srcOverride) ? unit : srcOverride );
			start = int.Parse(e.GetAttribute("start"));

			if (e.GetAttribute("length") == "*" || e.GetAttribute("end") == "*")
				length = sprites.Length - Start;
			else if (e.HasAttribute("length"))
				length = int.Parse(e.GetAttribute("length"));
			else if (e.HasAttribute("end"))
				length = int.Parse(e.GetAttribute("end")) - int.Parse(e.GetAttribute("start"));
			else
				length = 1;

			if( e.HasAttribute( "facings" ) )
				facings = int.Parse( e.GetAttribute( "facings" ) );
			else
				facings = 1;
		}

		public Sprite GetSprite( int frame )
		{
			return GetSprite( frame, 0 );
		}

		public Sprite GetSprite(int frame, int facing)
		{
			var f = Traits.Util.QuantizeFacing( facing, facings );
			return sprites[ (f * length) + ( frame % length ) + start ];
		}
	}
}
