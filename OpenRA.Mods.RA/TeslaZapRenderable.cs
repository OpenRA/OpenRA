#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;

namespace OpenRA.Mods.RA
{
	struct TeslaZapRenderable : IRenderable
	{
		static int[][] steps = new[]
		{
			new int[] { 8, 8, 4, 4, 0 },
			new int[] { -8, -8, -4, -4, 0 },
			new int[] { 8, 0, 4, 4, 1 },
			new int[] { -8, 0, -4, 4, 1 },
			new int[] { 0, 8, 4, 4, 2 },
			new int[] { 0, -8, 4, -4, 2 },
			new int[] { -8, 8, -4, 4, 3 },
			new int[] { 8, -8, 4, -4, 3 }
		};

		readonly WPos pos;
		readonly int zOffset;
		readonly WVec length;
		readonly string image;
		readonly string palette;
		readonly int brightZaps, dimZaps;

		WPos cachedPos;
		WVec cachedLength;
		IEnumerable<IRenderable> cache;

		public TeslaZapRenderable(WPos pos, int zOffset, WVec length, string image, int brightZaps, int dimZaps, string palette)
		{
			this.pos = pos;
			this.zOffset = zOffset;
			this.length = length;
			this.image = image;
			this.palette = palette;
			this.brightZaps = brightZaps;
			this.dimZaps = dimZaps;

			cachedPos = WPos.Zero;
			cachedLength = WVec.Zero;
			cache = new IRenderable[] { };
		}

		public WPos Pos { get { return pos; } }
		public float Scale { get { return 1f; } }
		public PaletteReference Palette { get { return null; } }
		public int ZOffset { get { return zOffset; } }
		public bool IsDecoration { get { return true; } }
		
		public IRenderable WithScale(float newScale) { return new TeslaZapRenderable(pos, zOffset, length, image, brightZaps, dimZaps, palette); }
		public IRenderable WithPalette(PaletteReference newPalette) { return new TeslaZapRenderable(pos, zOffset, length, image, brightZaps, dimZaps, palette); }
		public IRenderable WithZOffset(int newOffset) { return new TeslaZapRenderable(pos, zOffset, length, image, brightZaps, dimZaps, palette); }
		public IRenderable OffsetBy(WVec vec) { return new TeslaZapRenderable(pos + vec, zOffset, length, image, brightZaps, dimZaps, palette); }
		public IRenderable AsDecoration() { return this; }

		public void BeforeRender(WorldRenderer wr) { }
		public void RenderDebugGeometry(WorldRenderer wr) { }
		public void Render(WorldRenderer wr)
		{
			if (!cache.Any() || length != cachedLength || pos != cachedPos)
				cache = GenerateRenderables(wr);

			cache.Do(c => c.Render(wr));
		}

		public IEnumerable<IRenderable> GenerateRenderables(WorldRenderer wr)
		{
			var bright = wr.world.Map.SequenceProvider.GetSequence(image, "bright");
			var dim = wr.world.Map.SequenceProvider.GetSequence(image, "dim");
			
			var source = wr.ScreenPosition(pos);
			var target = wr.ScreenPosition(pos + length);
			
			for (var n = 0; n < dimZaps; n++)
				foreach (var z in DrawZapWandering(wr, source, target, dim, palette))
					yield return z;
			for (var n = 0; n < brightZaps; n++)
				foreach (var z in DrawZapWandering(wr, source, target, bright, palette))
					yield return z;
		}

		static IEnumerable<IRenderable> DrawZapWandering(WorldRenderer wr, float2 from, float2 to, Sequence s, string pal)
		{
			var z = float2.Zero;	/* hack */
			var dist = to - from;
			var norm = (1f / dist.Length) * new float2(-dist.Y, dist.X);

			var renderables = new List<IRenderable>();
			if (Game.CosmeticRandom.Next(2) != 0)
			{
				var p1 = from + (1 / 3f) * dist + WRange.FromPDF(Game.CosmeticRandom, 2).Range * dist.Length / 4096 * norm;
				var p2 = from + (2 / 3f) * dist + WRange.FromPDF(Game.CosmeticRandom, 2).Range * dist.Length / 4096 * norm;

				renderables.AddRange(DrawZap(wr, from, p1, s, out p1, pal));
				renderables.AddRange(DrawZap(wr, p1, p2, s, out p2, pal));
				renderables.AddRange(DrawZap(wr, p2, to, s, out z, pal));
			}
			else
			{
				var p1 = from + (1 / 2f) * dist + WRange.FromPDF(Game.CosmeticRandom, 2).Range * dist.Length / 4096 * norm;

				renderables.AddRange(DrawZap(wr, from, p1, s, out p1, pal));
				renderables.AddRange(DrawZap(wr, p1, to, s, out z, pal));
			}

			return renderables;
		}

		static IEnumerable<IRenderable> DrawZap(WorldRenderer wr, float2 from, float2 to, Sequence s, out float2 p, string palette)
		{
			var dist = to - from;
			var q = new float2(-dist.Y, dist.X);
			var c = -float2.Dot(from, q);
			var rs = new List<IRenderable>();
			var z = from;
			var pal = wr.Palette(palette);

			while ((to - z).X > 5 || (to - z).X < -5 || (to - z).Y > 5 || (to - z).Y < -5)
			{
				var step = steps.Where(t => (to - (z + new float2(t[0], t[1]))).LengthSquared < (to - z).LengthSquared)
					.OrderBy(t => Math.Abs(float2.Dot(z + new float2(t[0], t[1]), q) + c)).First();

				var pos = wr.Position((z + new float2(step[2], step[3])).ToInt2());
				rs.Add(new SpriteRenderable(s.GetSprite(step[4]), pos, WVec.Zero, 0, pal, 1f, true));

				z += new float2(step[0], step[1]);
				if (rs.Count >= 1000)
					break;
			}

			p = z;

			return rs;
		}
	}
}
