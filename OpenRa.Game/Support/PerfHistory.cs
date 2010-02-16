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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRa.FileFormats;
using OpenRa.Graphics;

namespace OpenRa.Support
{
	static class PerfHistory
	{
		static readonly Color[] colors = { Color.Red, Color.Green, Color.Blue, Color.Yellow, Color.Orange, Color.Fuchsia, Color.Lime, Color.LightBlue, Color.White, Color.Black };
		static int nextColor;

		public static Cache<string, PerfItem> items = new Cache<string, PerfItem>(
			s => 
			{
				var x = new PerfItem(s, colors[nextColor++]);
				if (nextColor >= colors.Length) nextColor = 0;
				return x;
			});

		public static void Increment( string item, double x )
		{
			items[item].val += x;
		}

		public static void Tick()
		{
			foreach (var item in items.Values)
				if (item.hasNormalTick)
					item.Tick();
		}

		public static void Render(Renderer r, LineRenderer lr)
		{
			float2 origin = Game.viewport.Location + new float2(330, Game.viewport.Height - 30);
			float2 basis = new float2(-3, -3);

			lr.DrawLine(origin, origin + new float2(100, 0) * basis, Color.White, Color.White);
			lr.DrawLine(origin + new float2(100,0) * basis, origin + new float2(100,70) * basis, Color.White, Color.White);

			foreach (var item in items.Values)
			{
				int n = 0;
				item.Samples().Aggregate((a, b) =>
				{
					lr.DrawLine(
						origin + new float2(n, (float)a) * basis,
						origin + new float2(n+1, (float)b) * basis,
						item.c, item.c);
					++n;
					return b;
				});
			}

			lr.Flush();
		}
	}

	class PerfItem
	{
		public readonly Color c;
		public readonly string Name;
		public double[] samples = new double[100];
		public double val = 0.0;
		int head = 1, tail = 0;
		public bool hasNormalTick = true;

		public PerfItem(string name, Color c)
		{
			Name = name;
			this.c = c;
		}

		public void Tick()
		{
			samples[head++] = val;
			if (head == samples.Length) head = 0;
			if (head == tail && ++tail == samples.Length) tail = 0;
			val = 0.0;
		}

		public IEnumerable<double> Samples()
		{
			int n = head;
			while (n != tail)
			{
				--n;
				if (n < 0) n = samples.Length - 1;
				yield return samples[n];
			}
		}

		public double LastValue
		{
			get
			{
				int n = head;
				if (--n < 0) n = samples.Length - 1;
				return samples[n];
			}
		}
	}

	class PerfSample : IDisposable
	{
		readonly Stopwatch sw = new Stopwatch();
		readonly string Item;

		public PerfSample(string item)
		{
			Item = item;
		}

		public void Dispose()
		{
			PerfHistory.Increment(Item, sw.ElapsedTime() * 1000);
		}
	}
}
