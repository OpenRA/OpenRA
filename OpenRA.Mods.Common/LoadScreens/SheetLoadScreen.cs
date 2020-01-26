#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Diagnostics;
using OpenRA.Graphics;

namespace OpenRA.Mods.Common.LoadScreens
{
	public abstract class SheetLoadScreen : BlankLoadScreen
	{
		Stopwatch lastUpdate;

		protected Dictionary<string, string> Info { get; private set; }
		Sheet sheet;

		public override void Init(ModData modData, Dictionary<string, string> info)
		{
			base.Init(modData, info);
			Info = info;
		}

		public abstract void DisplayInner(Renderer r, Sheet s);

		public override void Display()
		{
			// Limit load screens to at most 5 FPS
			if (Game.Renderer == null || (lastUpdate != null && lastUpdate.Elapsed.TotalSeconds < 0.2))
				return;

			// Start the timer on the first render
			if (lastUpdate == null)
				lastUpdate = Stopwatch.StartNew();

			if (sheet == null && Info.ContainsKey("Image"))
				using (var stream = ModData.DefaultFileSystem.Open(Info["Image"]))
					sheet = new Sheet(SheetType.BGRA, stream);

			Game.Renderer.BeginUI();
			DisplayInner(Game.Renderer, sheet);
			Game.Renderer.EndFrame(new NullInputHandler());

			lastUpdate.Restart();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && sheet != null)
				sheet.Dispose();

			base.Dispose(disposing);
		}
	}
}
