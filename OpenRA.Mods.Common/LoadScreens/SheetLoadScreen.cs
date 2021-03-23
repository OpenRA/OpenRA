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
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.LoadScreens
{
	public abstract class SheetLoadScreen : BlankLoadScreen
	{
		Stopwatch lastUpdate;

		protected Dictionary<string, string> Info { get; private set; }
		float dpiScale = 1;

		Sheet sheet;
		int density;

		public override void Init(ModData modData, Dictionary<string, string> info)
		{
			base.Init(modData, info);
			Info = info;
		}

		public abstract void DisplayInner(Renderer r, Sheet s, int density);

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
				sheet?.Dispose();

				sheet = null;
			}

			if (sheet == null && Info.ContainsKey("Image"))
			{
				var key = "Image";
				density = 1;
				if (dpiScale > 2 && Info.ContainsKey("Image3x"))
				{
					key = "Image3x";
					density = 3;
				}
				else if (dpiScale > 1 && Info.ContainsKey("Image2x"))
				{
					key = "Image2x";
					density = 2;
				}

				using (var stream = ModData.DefaultFileSystem.Open(Platform.ResolvePath(Info[key])))
				{
					sheet = new Sheet(SheetType.BGRA, stream);
					sheet.GetTexture().ScaleFilter = TextureScaleFilter.Linear;
				}
			}

			Game.Renderer.BeginUI();
			DisplayInner(Game.Renderer, sheet, density);
			Game.Renderer.EndFrame(new NullInputHandler());

			lastUpdate.Restart();
		}

		protected static Sprite CreateSprite(Sheet s, int density, Rectangle rect)
		{
			return new Sprite(s, density * rect, TextureChannel.RGBA, 1f / density);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				sheet?.Dispose();

			base.Dispose(disposing);
		}
	}
}
