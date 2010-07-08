#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Xml;

namespace OpenRA.Graphics
{
	public class CursorSequence
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
