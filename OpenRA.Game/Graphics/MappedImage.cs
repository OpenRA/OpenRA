#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;

namespace OpenRA.Graphics
{
	class MappedImage
	{
		readonly Rectangle rect = Rectangle.Empty;
		public readonly string Src;

		public MappedImage(string defaultSrc, MiniYaml info)
		{
			FieldLoader.LoadField(this, "rect", info.Value);
			FieldLoader.Load(this, info);
			if (Src == null)
				Src = defaultSrc;
		}

		public Sprite GetImage(Sheet s)
		{
			return new Sprite(s, rect, TextureChannel.Alpha);
		}

		public MiniYaml Save(string defaultSrc)
		{
			var root = new List<MiniYamlNode>();
			if (defaultSrc != Src)
				root.Add(new MiniYamlNode("Src", Src));

			return new MiniYaml(FieldSaver.FormatValue(this, GetType().GetField("rect")), root);
		}
	}
}
