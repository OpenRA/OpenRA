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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.SpriteLoaders
{
	public class PngSheetMetadata
	{
		public readonly ReadOnlyDictionary<string, string> Metadata;

		public PngSheetMetadata(Dictionary<string, string> metadata)
		{
			Metadata = new ReadOnlyDictionary<string, string>(metadata);
		}
	}

	public class PngSheetLoader : ISpriteLoader
	{
		class PngSheetFrame : ISpriteFrame
		{
			public SpriteFrameType Type { get; set; }
			public Size Size { get; set; }
			public Size FrameSize { get; set; }
			public float2 Offset { get; set; }
			public byte[] Data { get; set; }
			public bool DisableExportPadding { get { return false; } }
		}

		public bool TryParseSprite(Stream s, out ISpriteFrame[] frames, out TypeDictionary metadata)
		{
			metadata = null;
			frames = null;
			if (!Png.Verify(s))
				return false;

			var png = new Png(s);

			List<Rectangle> frameRegions;
			List<float2> frameOffsets;

			// Prefer manual defined regions over auto sliced regions.
			if (png.EmbeddedData.Any(meta => meta.Key.StartsWith("Frame[")))
				RegionsFromFrames(png, out frameRegions, out frameOffsets);
			else
				RegionsFromSlices(png, out frameRegions, out frameOffsets);

			frames = new ISpriteFrame[frameRegions.Count];

			for (var i = 0; i < frames.Length; i++)
			{
				var frameStart = frameRegions[i].X + frameRegions[i].Y * png.Width;
				var frameSize = new Size(frameRegions[i].Width, frameRegions[i].Height);
				var pixelLength = png.Palette == null ? 4 : 1;

				frames[i] = new PngSheetFrame()
				{
					Size = frameSize,
					FrameSize = frameSize,
					Offset = frameOffsets[i],
					Data = new byte[frameRegions[i].Width * frameRegions[i].Height * pixelLength],
					Type = png.Palette == null ? SpriteFrameType.BGRA : SpriteFrameType.Indexed
				};

				for (var y = 0; y < frames[i].Size.Height; y++)
					Array.Copy(png.Data, (frameStart + y * png.Width) * pixelLength, frames[i].Data, y * frames[i].Size.Width * pixelLength, frames[i].Size.Width * pixelLength);
			}

			metadata = new TypeDictionary
			{
				new PngSheetMetadata(png.EmbeddedData),
			};

			if (png.Palette != null)
				metadata.Add(new EmbeddedSpritePalette(png.Palette.Select(x => (uint)x.ToArgb()).ToArray()));

			return true;
		}

		void RegionsFromFrames(Png png, out List<Rectangle> regions, out List<float2> offsets)
		{
			regions = new List<Rectangle>();
			offsets = new List<float2>();
			var pngRectangle = new Rectangle(0, 0, png.Width, png.Height);

			string frame;
			for (var i = 0; png.EmbeddedData.TryGetValue("Frame[" + i + "]", out frame); i++)
			{
				// Format: x,y,width,height;offsetX,offsetY
				var coords = frame.Split(';');
				var region = FieldLoader.GetValue<Rectangle>("Region", coords[0]);
				if (!pngRectangle.Contains(region))
					throw new InvalidDataException("Invalid frame regions {0} defined.".F(region));

				regions.Add(region);
				offsets.Add(FieldLoader.GetValue<float2>("Offset", coords[1]));
			}
		}

		void RegionsFromSlices(Png png, out List<Rectangle> regions, out List<float2> offsets)
		{
			// Default: whole image is 1 frame.
			var frameSize = new Size(png.Width, png.Height);
			var frameAmount = 1;

			if (png.EmbeddedData.ContainsKey("FrameSize"))
			{
				// If FrameSize exist, use it and...
				frameSize = FieldLoader.GetValue<Size>("FrameSize", png.EmbeddedData["FrameSize"]);

				// ... either use FrameAmount or calculate how many times FrameSize fits into the image.
				if (png.EmbeddedData.ContainsKey("FrameAmount"))
					frameAmount = FieldLoader.GetValue<int>("FrameAmount", png.EmbeddedData["FrameAmount"]);
				else
					frameAmount = (png.Width / frameSize.Width) * (png.Height / frameSize.Height);
			}
			else if (png.EmbeddedData.ContainsKey("FrameAmount"))
			{
				// Otherwise, calculate the number of frames by splitting the image horizontaly by FrameAmount.
				frameAmount = FieldLoader.GetValue<int>("FrameAmount", png.EmbeddedData["FrameAmount"]);
				frameSize = new Size(png.Width / frameAmount, png.Height);
			}

			float2 offset;

			// If Offset property exists, use its value. Otherwise assume the frame is centered.
			if (png.EmbeddedData.ContainsKey("Offset"))
				offset = FieldLoader.GetValue<float2>("Offset", png.EmbeddedData["Offset"]);
			else
				offset = float2.Zero;

			var framesPerRow = png.Width / frameSize.Width;

			var rows = (frameAmount + framesPerRow - 1) / framesPerRow;
			if (png.Width < frameSize.Width * frameAmount / rows || png.Height < frameSize.Height * rows)
				throw new InvalidDataException("Invalid frame size {0} and frame amount {1} defined.".F(frameSize, frameAmount));

			regions = new List<Rectangle>();
			offsets = new List<float2>();

			for (var i = 0; i < frameAmount; i++)
			{
				var x = i % framesPerRow * frameSize.Width;
				var y = i / framesPerRow * frameSize.Height;

				regions.Add(new Rectangle(x, y, frameSize.Width, frameSize.Height));
				offsets.Add(offset);
			}
		}
	}
}
