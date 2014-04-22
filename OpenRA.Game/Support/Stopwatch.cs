#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.Support
{
	public class Stopwatch
	{
		System.Diagnostics.Stopwatch sw;

		public Stopwatch()
		{
			Reset();
		}

		public System.TimeSpan Elapsed
		{
			get { return this.sw.Elapsed; }
		}

		public void Reset()
		{
			sw = System.Diagnostics.Stopwatch.StartNew();
		}
	}
}
