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
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;

namespace OpenRA.Graphics
{
	public class Sequence
	{
		readonly Sprite[] sprites;
		readonly bool reverseFacings, transpose;

		public readonly string Name;
		public readonly int Start;
		public readonly int Length;
		public readonly int Stride;
		public readonly int Facings;
		public readonly int Tick;
		public readonly int ZOffset;
		public readonly int ShadowStart;
		public readonly int ShadowZOffset;

		public Sequence(string unit, string name, MiniYaml info)
		{
			var srcOverride = info.Value;
			Name = name;
			var d = info.NodesDict;
			var offset = float2.Zero;

			Start = int.Parse(d["Start"].Value);

			if (d.ContainsKey("Offset"))
				offset = FieldLoader.GetValue<float2>("Offset", d["Offset"].Value);

			// Apply offset to each sprite in the sequence
			// Different sequences may apply different offsets to the same frame
			sprites = Game.modData.SpriteLoader.LoadAllSprites(srcOverride ?? unit).Select(
				s => new Sprite(s.sheet, s.bounds, s.offset + offset, s.channel)).ToArray();

			if (!d.ContainsKey("Length"))
				Length = 1;
			else if (d["Length"].Value == "*")
				Length = sprites.Length - Start;
			else
				Length = int.Parse(d["Length"].Value);

			if (d.ContainsKey("Stride"))
				Stride = int.Parse(d["Stride"].Value);
			else
				Stride = Length;

			if (d.ContainsKey("Facings"))
			{
				var f = int.Parse(d["Facings"].Value);
				Facings = Math.Abs(f);
				reverseFacings = f < 0;
			}
			else
				Facings = 1;

			if (d.ContainsKey("Tick"))
				Tick = int.Parse(d["Tick"].Value);
			else
				Tick = 40;

			if (d.ContainsKey("Transpose"))
			    transpose = bool.Parse(d["Transpose"].Value);

			if (d.ContainsKey("ShadowStart"))
				ShadowStart = int.Parse(d["ShadowStart"].Value);
			else
				ShadowStart = -1;

			if (d.ContainsKey("ShadowZOffset"))
				ShadowZOffset = int.Parse(d["ShadowZOffset"].Value);
			else
				ShadowZOffset = -5;

			if (d.ContainsKey("ZOffset"))
				ZOffset = int.Parse(d["ZOffset"].Value);

			if (Length > Stride)
				throw new InvalidOperationException(
					"{0}: Sequence {1}.{2}: Length must be <= stride"
						.F(info.Nodes[0].Location, unit, name));

			if (Start < 0 || Start + Facings * Stride > sprites.Length || ShadowStart + Facings * Stride > sprites.Length)
				throw new InvalidOperationException(
					"{6}: Sequence {0}.{1} uses frames [{2}..{3}] of SHP `{4}`, but only 0..{5} actually exist"
					.F(unit, name, Start, Start + Facings * Stride - 1, srcOverride ?? unit, sprites.Length - 1,
					info.Nodes[0].Location));
		}

		public Sprite GetSprite(int frame)
		{
			return GetSprite(Start, frame, 0);
		}

		public Sprite GetSprite(int frame, int facing)
		{
			return GetSprite(Start, frame, facing);
		}

		public Sprite GetShadow(int frame, int facing)
		{
			return ShadowStart >= 0 ? GetSprite(ShadowStart, frame, facing) : null;
		}

		Sprite GetSprite(int start, int frame, int facing)
		{
			var f = Traits.Util.QuantizeFacing(facing, Facings);

			if (reverseFacings)
				f = (Facings - f) % Facings;

			int i = transpose ? (frame % Length) * Facings + f :
				(f * Stride) + (frame % Length);

			return sprites[start + i];
		}
	}
}
