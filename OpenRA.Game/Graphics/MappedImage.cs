#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
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

		public Sprite GetImage(Sheet s)
		{
			return new Sprite(s, rect, TextureChannel.Alpha);
		}
	}
}
