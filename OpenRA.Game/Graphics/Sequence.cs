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
		readonly int start, length, stride, facings, tick;

		public readonly string Name;
		public int Start { get { return start; } }
		public int End { get { return start + length; } }
		public int Length { get { return length; } }
		public int Stride { get { return stride; } }
		public int Facings { get { return facings; } }
		public int Tick { get { return tick; } }

		public Sequence(string unit, string name, MiniYaml info)
		{
			var srcOverride = info.Value;
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

			if (d.ContainsKey("Stride"))
				stride = int.Parse(d["Stride"].Value);
			else
				stride = length;

			if(d.ContainsKey("Facings"))
				facings = int.Parse(d["Facings"].Value);
			else
				facings = 1;

			if(d.ContainsKey("Tick"))
				tick = int.Parse(d["Tick"].Value);
			else
				tick = 40;

			if (length > stride)
				throw new InvalidOperationException(
					"{0}: Sequence {1}.{2}: Length must be <= stride"
						.F(info.Nodes[0].Location, unit, name));

			if (start < 0 || start + facings * stride > sprites.Length)
				throw new InvalidOperationException(
					"{6}: Sequence {0}.{1} uses frames [{2}..{3}] of SHP `{4}`, but only 0..{5} actually exist"
					.F(unit, name, start, start + facings * stride - 1, srcOverride ?? unit, sprites.Length - 1,
					info.Nodes[0].Location));
		}

		public Sprite GetSprite( int frame )
		{
			return GetSprite( frame, 0 );
		}

		public Sprite GetSprite(int frame, int facing)
		{
			var f = Traits.Util.QuantizeFacing( facing, facings );
			return sprites[ (f * stride) + ( frame % length ) + start ];
		}
	}
}
