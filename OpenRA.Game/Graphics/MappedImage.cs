#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;

namespace OpenRA.Graphics
{
	class MappedImage
	{
		public readonly Rectangle rect = Rectangle.Empty;
		public readonly string src;

		public MappedImage(string defaultSrc, MiniYaml info)
		{
			FieldLoader.LoadField(this, "rect", info.Value);
			FieldLoader.Load(this, info);
			if (src == null)
				src = defaultSrc;
		}

		public Sprite GetImage(Sheet s)
		{
			return new Sprite(s, rect, TextureChannel.Alpha);
		}

		public MiniYaml Save(string defaultSrc)
		{
			var root = new List<MiniYamlNode>();
			if (defaultSrc != src)
				root.Add(new MiniYamlNode("src", src));

			return new MiniYaml(FieldSaver.FormatValue( this, this.GetType().GetField("rect") ), root);
		}
	}
}
