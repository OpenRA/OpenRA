#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA.Primitives;

namespace OpenRA.Support
{
	public static class PerfHistory
	{
		static readonly Color[] Colors = { Color.Red, Color.Green,
			Color.Orange, Color.Yellow,
			Color.Fuchsia, Color.Lime,
			Color.LightBlue, Color.Blue,
			Color.White, Color.Teal };

		static int nextColor;

		public static Cache<string, PerfItem> Items = new Cache<string, PerfItem>(
			s =>
			{
				var x = new PerfItem(s, Colors[nextColor++]);
				if (nextColor >= Colors.Length) nextColor = 0;
				return x;
			});

		public static void Increment(string item, double x)
		{
			Items[item].Val += x;
		}

		public static void Tick()
		{
			foreach (var item in Items.Values)
				if (item.HasNormalTick)
					item.Tick();
		}
	}
}