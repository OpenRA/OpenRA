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

using System.Drawing;
using System.IO;
using System.Xml;

namespace OpenRA.Graphics
{
	class MappedImage
	{
		readonly Rectangle rect;
		public readonly string Src;
		public readonly string Name;

		public MappedImage(string defaultSrc, XmlElement e)
		{
			Src = (e.HasAttribute("src")) ? e.GetAttribute("src") : defaultSrc;
			Name = e.GetAttribute("name");
			if (Src == null)
				throw new InvalidDataException("Image src missing");

			rect = new Rectangle(int.Parse(e.GetAttribute("x")),
								 int.Parse(e.GetAttribute("y")),
								 int.Parse(e.GetAttribute("width")),
								 int.Parse(e.GetAttribute("height")));
		}

		public Sprite GetImage(Renderer r, Sheet s)
		{
			return new Sprite(s, rect, TextureChannel.Alpha);
		}
	}
}
