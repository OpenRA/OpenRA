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
using System.Linq;
using OpenRA.FileFormats;

namespace OpenRA.Graphics
{
	public class Sequence
	{
		readonly Sprite[] sprites;
		readonly int start, length, stride, facings, tick;
		readonly bool reverseFacings, transpose;

		public readonly string Name;
		public int Start { get { return start; } }
		public int End { get { return start + length; } }
		public int Length { get { return length; } }
		public int Stride { get { return stride; } }
		public int Facings { get { return facings; } }
		public int Tick { get { return tick; } }
		public readonly int ZOffset;

		public Sequence(string unit, string name, MiniYaml info)
		{
			var srcOverride = info.Value;
			Name = name;
			var d = info.NodesDict;
			var offset = float2.Zero;

			start = int.Parse(d["Start"].Value);

			if (d.ContainsKey("Offset"))
				offset = FieldLoader.GetValue<float2>("Offset", d["Offset"].Value);

			// Apply offset to each sprite in the sequence
			// Different sequences may apply different offsets to the same frame
			sprites = Game.modData.SpriteLoader.LoadAllSprites(srcOverride ?? unit).Select(
				s => new Sprite(s.sheet, s.bounds, s.offset + offset, s.channel)).ToArray();

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

			if (d.ContainsKey("Facings"))
			{
				var f = int.Parse(d["Facings"].Value);
				facings = Math.Abs(f);
				reverseFacings = f < 0;
			}
			else
				facings = 1;

			if (d.ContainsKey("Tick"))
				tick = int.Parse(d["Tick"].Value);
			else
				tick = 40;

			if (d.ContainsKey("Transpose"))
			    transpose = bool.Parse(d["Transpose"].Value);

			if (d.ContainsKey("ZOffset"))
				ZOffset = int.Parse(d["ZOffset"].Value);

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

		public Sprite GetSprite(int frame)
		{
			return GetSprite(frame, 0);
		}

		public Sprite GetSprite(int frame, int facing)
		{
			var f = Traits.Util.QuantizeFacing(facing, facings);

			if (reverseFacings)
				f = (facings - f) % facings;

			int i = transpose ? (frame % length) * facings + f :
				(f * stride) + (frame % length);

			return sprites[start + i];
		}
	}
}
