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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using OpenRA.FileFormats;
using OpenRA.Graphics;

namespace OpenRA.Mods.Common.UtilityCommands
{
	class ConvertSpriteToPngCommand : IUtilityCommand
	{
		public string Name { get { return "--png"; } }

		[Desc("SPRITEFILE PALETTE [--noshadow] [--nopadding]",
			  "Convert a shp/tmp/R8 to a series of PNGs, optionally removing shadow")]
		public void Run(ModData modData, string[] args)
		{
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

			ISpriteSource source;
			using (var stream = File.OpenRead(src))
				source = SpriteSource.LoadSpriteSource(stream, src);

			// The r8 padding requires external information that we can't access here.
			var usePadding = !(args.Contains("--nopadding") || source is R8Reader);
			var count = 0;
			var prefix = Path.GetFileNameWithoutExtension(src);

			foreach (var frame in source.Frames)
			{
				var frameSize = usePadding ? frame.FrameSize : frame.Size;
				var offset = usePadding ? (frame.Offset - 0.5f * new float2(frame.Size - frame.FrameSize)).ToInt2() : int2.Zero;

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
					if (usePadding)
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
