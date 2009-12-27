using System.Xml;

namespace OpenRa.Game.Graphics
{
	class CursorSequence
	{
		readonly int start, length;

		public int Start { get { return start; } }
		public int End { get { return start + length; } }
		public int Length { get { return length; } }

		public readonly int2 Hotspot;

		Sprite[] sprites;

		public CursorSequence(string cursorSrc, XmlElement e)
		{
			sprites = CursorSheetBuilder.LoadAllSprites(cursorSrc);

			start = int.Parse(e.GetAttribute("start"));

			if (e.GetAttribute("length") == "*" || e.GetAttribute("end") == "*")
				length = sprites.Length - start;
			else if (e.HasAttribute("length"))
				length = int.Parse(e.GetAttribute("length"));
			else if (e.HasAttribute("end"))
				length = int.Parse(e.GetAttribute("end")) - start;
			else
				length = 1;

			int.TryParse( e.GetAttribute("x"), out Hotspot.X );
			int.TryParse( e.GetAttribute("y"), out Hotspot.Y );
		}

		public Sprite GetSprite(int frame)
		{
			return sprites[(frame % length) + start];
		}
	}
}
