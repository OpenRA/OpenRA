#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Xml;

namespace OpenRA.Graphics
{
	public class CursorSequence
	{
		readonly int start, length;
		readonly string palette;

		public int Start { get { return start; } }
		public int End { get { return start + length; } }
		public int Length { get { return length; } }
		public string Palette { get { return palette; } }
		public readonly int2 Hotspot;

		Sprite[] sprites;

		public CursorSequence(string cursorSrc, string palette, XmlElement e)
		{
			sprites = Game.modData.CursorSheetBuilder.LoadAllSprites(cursorSrc);

			start = int.Parse(e.GetAttribute("start"));
			this.palette = palette;
			
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
