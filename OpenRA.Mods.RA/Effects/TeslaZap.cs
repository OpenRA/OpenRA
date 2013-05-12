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
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Effects
{
	class TeslaZapInfo : IProjectileInfo
	{
		public readonly string Image = "litning";
		public readonly int BrightZaps = 1;
		public readonly int DimZaps = 2;
		public IEffect Create(ProjectileArgs args) { return new TeslaZap( this, args ); }
	}

	class TeslaZap : IEffect
	{
		readonly ProjectileArgs Args;
		readonly TeslaZapInfo Info;
		IEnumerable<Renderable> renderables;
		int timeUntilRemove = 2; // # of frames
		bool doneDamage = false;
		bool initialized = false;

		public TeslaZap(TeslaZapInfo info, ProjectileArgs args)
		{
			Args = args;
			Info = info;
		}

		public IEnumerable<Renderable> GenerateRenderables(WorldRenderer wr)
		{
			var bright = SequenceProvider.GetSequence(Info.Image, "bright");
			var dim = SequenceProvider.GetSequence(Info.Image, "dim");

			var src = new PPos(Args.src.X, Args.src.Y - Args.srcAltitude);
			var dest = new PPos(Args.dest.X, Args.dest.Y - Args.destAltitude);
			for (var n = 0; n < Info.DimZaps; n++)
				foreach (var z in DrawZapWandering(wr, src, dest, dim))
					yield return z;
			for (var n = 0; n < Info.BrightZaps; n++)
				foreach (var z in DrawZapWandering(wr, src, dest, bright))
					yield return z;
		}

		public void Tick(World world)
		{
			if (timeUntilRemove <= 0)
				world.AddFrameEndTask(w => w.Remove(this));
			--timeUntilRemove;

			if (!doneDamage)
			{
				if (Args.target.IsValid)
					Args.dest = Args.target.CenterLocation;

				Combat.DoImpacts(Args);
				doneDamage = true;
			}
		}

		public IEnumerable<Renderable> Render(WorldRenderer wr)
		{
			if (!initialized)
			{
				renderables = GenerateRenderables(wr);
				initialized = true;
			}

			return renderables;
		}

		static IEnumerable<Renderable> DrawZapWandering(WorldRenderer wr, PPos from, PPos to, Sequence s)
		{
			var z = float2.Zero;	/* hack */
			var dist = to - from;
			var norm = (1f / dist.Length) * new float2(-dist.Y, dist.X);

			var renderables = new List<Renderable>();
			if (Game.CosmeticRandom.Next(2) != 0)
			{
				var p1 = from.ToFloat2() + (1 / 3f) * dist.ToFloat2() + Game.CosmeticRandom.Gauss1D(1) * .2f * dist.Length * norm;
				var p2 = from.ToFloat2() + (2 / 3f) * dist.ToFloat2() + Game.CosmeticRandom.Gauss1D(1) * .2f * dist.Length * norm;

				renderables.AddRange(DrawZap(wr, from.ToFloat2(), p1, s, out p1));
				renderables.AddRange(DrawZap(wr, p1, p2, s, out p2));
				renderables.AddRange(DrawZap(wr, p2, to.ToFloat2(), s, out z));
			}
			else
			{
				var p1 = from.ToFloat2() + (1 / 2f) * dist.ToFloat2() + Game.CosmeticRandom.Gauss1D(1) * .2f * dist.Length * norm;

				renderables.AddRange(DrawZap(wr, from.ToFloat2(), p1, s, out p1));
				renderables.AddRange(DrawZap(wr, p1, to.ToFloat2(), s, out z));
			}

			return renderables;
		}

		static IEnumerable<Renderable> DrawZap(WorldRenderer wr, float2 from, float2 to, Sequence s, out float2 p)
		{
			var dist = to - from;
			var q = new float2(-dist.Y, dist.X);
			var c = -float2.Dot(from, q);
			var rs = new List<Renderable>();
			var z = from;

			while ((to - z).X > 5 || (to - z).X < -5 || (to - z).Y > 5 || (to - z).Y < -5)
			{
				var step = steps.Where(t => (to - (z + new float2(t[0],t[1]))).LengthSquared < (to - z).LengthSquared )
					.OrderBy(t => Math.Abs(float2.Dot(z + new float2(t[0], t[1]), q) + c)).First();

				rs.Add(new Renderable(s.GetSprite(step[4]), z + new float2(step[2], step[3]),
					wr.Palette("effect"), (int)from.Y));
				z += new float2(step[0], step[1]);
				if( rs.Count >= 1000 )
					break;
			}

			p = z;

			return rs;
		}

		static int[][] steps = new []
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
	}
}
