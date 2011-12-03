#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Xml;
using System.Collections.Generic;
using OpenRA.FileFormats;

namespace OpenRA.Graphics
{
	public class Sequence
	{
		readonly Sprite[] sprites;
		readonly int start, length, facings, tick;

		public readonly string Name;
		public int Start { get { return start; } }
		public int End { get { return start + length; } }
		public int Length { get { return length; } }
		public int Facings { get { return facings; } }
		public int Tick { get { return tick; } }

		string srcOverride;
		public Sequence(string unit, string name, MiniYaml info)
		{
			srcOverride = info.Value;
			Name = name;
			var d = info.NodesDict;

			sprites = Game.modData.SpriteLoader.LoadAllSprites(srcOverride ?? unit);
			start = int.Parse(d["Start"].Value);

			if (!d.ContainsKey("Length"))
				length = 1;
			else if (d["Length"].Value == "*")
				length = sprites.Length - Start;
			else
				length = int.Parse(d["Length"].Value);


			if(d.ContainsKey("Facings"))
				facings = int.Parse(d["Facings"].Value);
			else
				facings = 1;

			if(d.ContainsKey("Tick"))
				tick = int.Parse(d["Tick"].Value);
			else
				tick = 40;

			if (start < 0 || start + facings * length > sprites.Length)
				throw new InvalidOperationException(
					"{6}: Sequence {0}.{1} uses frames [{2}..{3}] of SHP `{4}`, but only 0..{5} actually exist"
					.F(unit, name, start, start + facings * length - 1, srcOverride ?? unit, sprites.Length - 1,
					info.Nodes[0].Location));
		}

		public MiniYaml Save()
		{
			var root = new List<MiniYamlNode>();

			root.Add(new MiniYamlNode("Start", start.ToString()));

			if (length > 1 && (start != 0 || length != sprites.Length - start))
				root.Add(new MiniYamlNode("Length", length.ToString()));
			else if (length > 1 && length == sprites.Length - start)
				root.Add(new MiniYamlNode("Length", "*"));

			if (facings > 1)
				root.Add(new MiniYamlNode("Facings", facings.ToString()));

			if (tick != 40)
				root.Add(new MiniYamlNode("Tick", tick.ToString()));

			return new MiniYaml(srcOverride, root);
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
