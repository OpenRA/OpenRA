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
		float dpiScale = 1;
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

			// Check for window DPI changes
			// We can't trust notifications to be working during initialization, so must do this manually
			var scale = Game.Renderer.WindowScale;
			if (dpiScale != scale)
			{
				dpiScale = scale;

				// Force images to be reloaded on the next display
				if (sheet != null)
					sheet.Dispose();

				sheet = null;
			}

			if (sheet == null && Info.ContainsKey("Image"))
			{
				var key = "Image";
				float sheetScale = 1;
				if (dpiScale > 2 && Info.ContainsKey("Image3x"))
				{
					key = "Image3x";
					sheetScale = 3;
				}
				else if (dpiScale > 1 && Info.ContainsKey("Image2x"))
				{
					key = "Image2x";
					sheetScale = 2;
				}

				using (var stream = ModData.DefaultFileSystem.Open(Info[key]))
				{
					sheet = new Sheet(SheetType.BGRA, stream);
					sheet.GetTexture().ScaleFilter = TextureScaleFilter.Linear;
					sheet.DPIScale = sheetScale;
				}
			}

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
