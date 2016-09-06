#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using OpenRA.Graphics;

namespace OpenRA.Mods.Common.UtilityCommands
{
	class ConvertSpriteToPngCommand : IUtilityCommand
	{
		public string Name { get { return "--png"; } }

		public bool ValidateArguments(string[] args)
		{
			return args.Length >= 3;
		}

		[Desc("SPRITEFILE PALETTE [--noshadow] [--nopadding]",
			  "Convert a shp/tmp/R8 to a series of PNGs, optionally removing shadow")]
		public void Run(ModData modData, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			Game.ModData = modData;

			var src = args[1];
			var shadowIndex = new int[] { };
			if (args.Contains("--noshadow"))
			{
				Array.Resize(ref shadowIndex, shadowIndex.Length + 3);
				shadowIndex[shadowIndex.Length - 1] = 1;
				shadowIndex[shadowIndex.Length - 2] = 3;
				shadowIndex[shadowIndex.Length - 3] = 4;
			}

			var palette = new ImmutablePalette(args[2], shadowIndex);

			var frames = SpriteLoader.GetFrames(File.OpenRead(src), modData.SpriteLoaders);

			var usePadding = !args.Contains("--nopadding");
			var count = 0;
			var prefix = Path.GetFileNameWithoutExtension(src);

			foreach (var frame in frames)
			{
				var frameSize = usePadding && !frame.DisableExportPadding ? frame.FrameSize : frame.Size;
				var offset = usePadding && !frame.DisableExportPadding ? (frame.Offset - 0.5f * new float2(frame.Size - frame.FrameSize)).ToInt2() : int2.Zero;

				// shp(ts) may define empty frames
				if (frameSize.Width == 0 && frameSize.Height == 0)
				{
					count++;
					continue;
				}

				using (var bitmap = new Bitmap(frameSize.Width, frameSize.Height, PixelFormat.Format8bppIndexed))
				{
					bitmap.Palette = palette.AsSystemPalette();
					var data = bitmap.LockBits(new Rectangle(0, 0, frameSize.Width, frameSize.Height),
						ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

					// Clear the frame
					if (usePadding && !frame.DisableExportPadding)
					{
						var clearRow = new byte[data.Stride];
						for (var i = 0; i < frameSize.Height; i++)
							Marshal.Copy(clearRow, 0, new IntPtr(data.Scan0.ToInt64() + i * data.Stride), data.Stride);
					}

					for (var i = 0; i < frame.Size.Height; i++)
					{
						var destIndex = new IntPtr(data.Scan0.ToInt64() + (i + offset.Y) * data.Stride + offset.X);
						Marshal.Copy(frame.Data, i * frame.Size.Width, destIndex, frame.Size.Width);
					}

					bitmap.UnlockBits(data);

					var filename = "{0}-{1:D4}.png".F(prefix, count++);
					bitmap.Save(filename);
				}
			}

			Console.WriteLine("Saved {0}-[0..{1}].png", prefix, count - 1);
		}
	}
}
