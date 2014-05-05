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
using System.Diagnostics;
using System.Drawing;
using OpenRA.Primitives;

namespace OpenRA.Support
{
	public static class PerfHistory
	{
		static readonly Color[] colors = { Color.Red, Color.Green,
											 Color.Orange, Color.Yellow,
											 Color.Fuchsia, Color.Lime,
											 Color.LightBlue, Color.Blue,
											 Color.White, Color.Teal };
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
	}

	public class PerfItem
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

		public double Average(int count)
		{
			int i = 0;
			int n = head;
			double sum = 0;
			while (i < count && n != tail)
			{
				if (--n < 0) n = samples.Length - 1;
				sum += samples[n];
				i++;
			}
			return sum / i;
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

	public class PerfSample : IDisposable
	{
		readonly Stopwatch sw = Stopwatch.StartNew();
		readonly string Item;

		public PerfSample(string item)
		{
			Item = item;
		}

		public void Dispose()
		{
			PerfHistory.Increment(Item, sw.Elapsed.TotalMilliseconds);
		}
	}
}
