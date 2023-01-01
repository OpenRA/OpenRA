#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Primitives;

namespace OpenRA.Support
{
	public class PerfItem
	{
		public readonly Color C;
		public readonly string Name;
		readonly double[] samples = new double[100];
		public double Val = 0.0;
		int head = 1, tail = 0;
		public bool HasNormalTick = true;

		public PerfItem(string name, Color c)
		{
			Name = name;
			C = c;
		}

		public void Tick()
		{
			samples[head++] = Val;
			if (head == samples.Length) head = 0;
			if (head == tail && ++tail == samples.Length) tail = 0;
			Val = 0.0;
		}

		public IEnumerable<double> Samples()
		{
			var n = head;
			while (n != tail)
			{
				--n;
				if (n < 0) n = samples.Length - 1;
				yield return samples[n];
			}
		}

		public double Average(int count)
		{
			var i = 0;
			var n = head;
			double sum = 0;
			while (i < count && n != tail)
			{
				if (--n < 0) n = samples.Length - 1;
				sum += samples[n];
				i++;
			}

			return i == 0 ? sum : sum / i;
		}

		public double LastValue
		{
			get
			{
				var n = head;
				if (--n < 0) n = samples.Length - 1;
				return samples[n];
			}
		}
	}
}
