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

using System.IO;
using OpenRA.Support;

namespace OpenRA
{
	public class ScreenshotManager
	{
		public static void TakeScreenshot()
		{
			using (new PerfTimer("Renderer.SaveScreenshot"))
			{
				var mod = Game.ModData.Manifest.Metadata;
				var directory = Path.Combine(Platform.SupportDir, "Screenshots", Game.ModData.Manifest.Id, mod.Version);
				Directory.CreateDirectory(directory);

				var filename = Game.TimestampedFilename(true);
				var path = Path.Combine(directory, string.Concat(filename, ".png"));
				Log.Write("debug", "Taking screenshot " + path);

				Game.Renderer.SaveScreenshot(path);
				TextManager.Debug("Saved screenshot " + filename);
			}
		}
	}
}
